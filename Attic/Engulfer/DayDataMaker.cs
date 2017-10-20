using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Engulfer
{
	public class DayDataMaker
	{
		private readonly Dictionary<int, List<EodEntry>> _dataEachDay;
		private readonly Dictionary<string, HashSet<string>> _parentToRelations;
		private List<string> _allTickers;
		private readonly bool _log;
		
		public DayDataMaker(bool log)
		{
			_allTickers = new List<string>();
			_dataEachDay = new Dictionary<int, List<EodEntry>>();
			_parentToRelations = new Dictionary<string, HashSet<string>>();
			_log = log;
		}

		public void Init()
		{
			_dataEachDay.Clear();
			
			using (var db = new MarketContext())
			{
				using (var connection = db.GetConnection())
				{
					connection.Open();

					if (_log)
					{
						Console.WriteLine("Reading all eod entries");
					}

					var allDays = db.GetAllEodEntries(connection);

					if (_log)
					{
						Console.WriteLine("organizing eod entries into days");
					}
					
					var byDay = allDays.GroupBy(x => x.Date).ToDictionary(x => x.Key);

					var days = allDays.Select(x => x.Date).Distinct().OrderBy(x => x).ToList();

					var count = 0;
					
					foreach (var day in days)
					{
						// exclude holidays
						if (byDay[day].Any(x => x.Vol > 0))
						{
							_dataEachDay[count++] = byDay[day].Where(x => x.Close > 0 && x.Vol > 0).ToList();
						}
					}

					_allTickers = allDays.Select(x => x.Ticker).Distinct().ToList();

					if (_log)
					{
						Console.WriteLine("Gathering related tickers per ticker");
					}

					foreach (var ticker in _allTickers)
					{
						var relationships = db.GetRelations(ticker, connection);	
						_parentToRelations[ticker] = new HashSet<string>(relationships.Select(x => x.RelatedTicker));
					}
				}
			}
		}

		public List<DayData> GetDatas()
		{
			if (!_dataEachDay.Any())
			{
				Init();
			}

			var dayDatas = new List<DayData>();
			
			var stopwatch = new Stopwatch();

			for (var count = 5; count < _dataEachDay.Count - 1; count++)
			{
				stopwatch.Restart();

				if (_log)
				{
					Console.Write($"Day {count} of {_dataEachDay.Count - 2}...");
				}

				var days5 = _dataEachDay[count - 5];
				var days4 = _dataEachDay[count - 4];
				var days3 = _dataEachDay[count - 3];
				var days2 = _dataEachDay[count - 2];
				var days1 = _dataEachDay[count - 1];
				var days0 = _dataEachDay[count];
				var daysT = _dataEachDay[count + 1];

				var tickerSubset = days5.Select(x => x.Ticker)
					.Intersect(days4.Select(x => x.Ticker))
					.Intersect(days3.Select(x => x.Ticker))
					.Intersect(days2.Select(x => x.Ticker))
					.Intersect(days1.Select(x => x.Ticker))
					.Intersect(days0.Select(x => x.Ticker))
					.Intersect(daysT.Select(x => x.Ticker));

				var tickerToData = new Dictionary<string, DayData>();

				Parallel.ForEach(tickerSubset, ticker =>
				{
					var dayData = new DayData();
					
					var day5 = days5.First(x => x.Ticker.Equals(ticker));
					var day4 = days4.First(x => x.Ticker.Equals(ticker));
					var day3 = days3.First(x => x.Ticker.Equals(ticker));
					var day2 = days2.First(x => x.Ticker.Equals(ticker));
					var day1 = days1.First(x => x.Ticker.Equals(ticker));
					var day0 = days0.First(x => x.Ticker.Equals(ticker));
					var dayT = daysT.First(x => x.Ticker.Equals(ticker));

					dayData.TickerCloseChangePastDay = (double) ((day0.Close - day1.Close) / day1.Close);
					dayData.TickerCloseChangePast2Days = (double) ((day0.Close - day2.Close) / day2.Close);
					dayData.TickerCloseChangePast4Days = (double) ((day0.Close - day4.Close) / day4.Close);
					dayData.TickerCloseChangeNext = (double) ((dayT.Close - day0.Close) / day0.Close);

					var tickerVolLately = new[] {day2.Vol + day3.Vol + day4.Vol + day5.Vol}.Average();

					dayData.TickerVolTodayVsLately = (day0.Vol - tickerVolLately) / tickerVolLately;
					dayData.TickerVolYesterdayVsLately = (day1.Vol - tickerVolLately) / tickerVolLately;
					
					lock (this)
					{
						tickerToData[ticker] = dayData;
					}
				});

				var allDataForDay = tickerToData.Values.ToList();

				var sdCloseChangePastDay = StandardDeviation(allDataForDay.Select(x => x.TickerCloseChangePastDay).ToList());
				var sdCloseChangePast2Days = StandardDeviation(allDataForDay.Select(x => x.TickerCloseChangePast2Days).ToList());
				var sdCloseChangePast4Days = StandardDeviation(allDataForDay.Select(x => x.TickerCloseChangePast4Days).ToList());
				var sdVolChangeToday = StandardDeviation(allDataForDay.Select(x => x.TickerVolTodayVsLately).ToList());
				var sdVolChangeYesterday = StandardDeviation(allDataForDay.Select(x => x.TickerVolYesterdayVsLately).ToList());
				var sdChangeNext = StandardDeviation(allDataForDay.Select(x => x.TickerCloseChangeNext).ToList());

				tickerToData.Values.ToList().ForEach(dayData =>
				{
					dayData.TickerCloseChangePastDay = (dayData.TickerCloseChangePastDay - sdCloseChangePastDay.Average) / sdCloseChangePastDay.StandardDeviation;
					dayData.TickerCloseChangePast2Days = (dayData.TickerCloseChangePast2Days - sdCloseChangePast2Days.Average) / sdCloseChangePast2Days.StandardDeviation;
					dayData.TickerCloseChangePast4Days = (dayData.TickerCloseChangePast4Days - sdCloseChangePast4Days.Average) / sdCloseChangePast4Days.StandardDeviation;
					dayData.TickerVolTodayVsLately = (dayData.TickerVolTodayVsLately - sdVolChangeToday.Average) / sdVolChangeToday.StandardDeviation;
					dayData.TickerVolYesterdayVsLately = (dayData.TickerVolYesterdayVsLately - sdVolChangeYesterday.Average) / sdVolChangeYesterday.StandardDeviation;
					dayData.TickerCloseChangeNext = (dayData.TickerCloseChangeNext - sdChangeNext.Average) / sdChangeNext.StandardDeviation;
				});

				Parallel.ForEach(tickerToData.Keys, ticker =>
				{
					var dayData = tickerToData[ticker];
					var relations = _parentToRelations[ticker].Where(relation => tickerToData.ContainsKey(relation)).ToList();

					// ignore anything without related stocks
					if (!relations.Any())
					{
						return;
					}
					
					dayData.AverageRelationCloseChangePastDay =
						relations.Select(relation => tickerToData[relation].TickerCloseChangePastDay).Average();

					dayData.AverageRelationCloseChangePast2Days =
						relations.Select(relation => tickerToData[relation].TickerCloseChangePast2Days).Average();

					dayData.AverageRelationCloseChangePast4Days =
						relations.Select(relation => tickerToData[relation].TickerCloseChangePast4Days).Average();

					dayData.AverageRelationVolTodayVsLately =
						relations.Select(relation => tickerToData[relation].TickerVolTodayVsLately).Average();

					dayData.AverageRelationVolYesterdayVsLately =
						relations.Select(relation => tickerToData[relation].TickerVolYesterdayVsLately).Average();

					lock (this)
					{
						dayDatas.Add(dayData);
					}
				});

				if (_log)
				{
					Console.WriteLine($" {stopwatch.Elapsed.TotalSeconds} seconds");
				}
			}

			if (_log)
			{
				Console.WriteLine("Done");
			}
			
			return dayDatas;
		}

		private static Sd StandardDeviation(List<double> someDoubles)
		{
			var average = someDoubles.Average();
			var sumOfSquaresOfDifferences = someDoubles.Select(val => (val - average) * (val - average)).Sum();
			
			return new Sd
			{
				Average = average,
				StandardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / someDoubles.Count())
			};
		}
	}

	struct Sd
	{
		public double Average;
		public double StandardDeviation;

	}
}