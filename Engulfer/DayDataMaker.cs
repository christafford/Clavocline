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
							_dataEachDay[count++] = byDay[day].Where(x => x.Close > 0).ToList();
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

			for (var count = 4; count < _dataEachDay.Count - 1; count++)
			{
				stopwatch.Restart();

				if (_log)
				{
					Console.Write($"Day {count} of {_dataEachDay.Count - 1}...");
				}

				var fourDays = _dataEachDay[count - 4];
				var twoDays = _dataEachDay[count - 2];
				var previousDay = _dataEachDay[count - 1];
				var currentDay = _dataEachDay[count];
				var tomorrow = _dataEachDay[count + 1];

				var tickerSubset = fourDays.Select(x => x.Ticker)
					.Intersect(twoDays.Select(x => x.Ticker))
					.Intersect(previousDay.Select(x => x.Ticker))
					.Intersect(currentDay.Select(x => x.Ticker))
					.Intersect(tomorrow.Select(x => x.Ticker));

				var tickerToData = new Dictionary<string, DayData>();

				Parallel.ForEach(tickerSubset, ticker =>
				{
					var dayData = new DayData();
					var tickerCurrentDay = currentDay.First(x => x.Ticker.Equals(ticker));
					decimal temp;

					dayData.TickerCloseChangePastDay = (double)
						((tickerCurrentDay.Close - (temp = previousDay.First(x => x.Ticker.Equals(ticker)).Close))
						 / temp);

					dayData.TickerCloseChangePast2Days = (double)
						((tickerCurrentDay.Close - (temp = twoDays.First(x => x.Ticker.Equals(ticker)).Close))
						 / temp);

					dayData.TickerCloseChangePast4Days = (double)
						((tickerCurrentDay.Close - (temp = fourDays.First(x => x.Ticker.Equals(ticker)).Close))
						 / temp);

					dayData.TickerVolToday = tickerCurrentDay.Vol;

					dayData.TickerVolYesterday = previousDay.First(x => x.Ticker.Equals(ticker)).Vol;

					dayData.TickerVolPast2Days = twoDays.First(x => x.Ticker.Equals(ticker)).Vol;
					
					dayData.TickerCloseChangeNext = (double)
						((tomorrow.First(x => x.Ticker.Equals(ticker)).Close - tickerCurrentDay.Close)
						 / tickerCurrentDay.Close);
					
					lock (this)
					{
						tickerToData[ticker] = dayData;
					}
				});

				var allDataForDay = tickerToData.Values.ToList();

				var sdCloseChangePastDay = StandardDeviation(allDataForDay.Select(x => x.TickerCloseChangePastDay).ToList());
				var sdCloseChangePast2Days = StandardDeviation(allDataForDay.Select(x => x.TickerCloseChangePast2Days).ToList());
				var sdCloseChangePast4Days = StandardDeviation(allDataForDay.Select(x => x.TickerCloseChangePast4Days).ToList());
				var sdVolToday = StandardDeviation(allDataForDay.Select(x => x.TickerVolToday).ToList());
				var sdVolYesterday = StandardDeviation(allDataForDay.Select(x => x.TickerVolYesterday).ToList());
				var sdVolPast2Days = StandardDeviation(allDataForDay.Select(x => x.TickerVolPast2Days).ToList());
				var sdChangeNext = StandardDeviation(allDataForDay.Select(x => x.TickerCloseChangeNext).ToList());

				tickerToData.Values.ToList().ForEach(dayData =>
				{
					dayData.TickerCloseChangePastDay = (dayData.TickerCloseChangePastDay - sdCloseChangePastDay.Average) / sdCloseChangePastDay.StandardDeviation;
					dayData.TickerCloseChangePast2Days = (dayData.TickerCloseChangePast2Days - sdCloseChangePast2Days.Average) / sdCloseChangePast2Days.StandardDeviation;
					dayData.TickerCloseChangePast4Days = (dayData.TickerCloseChangePast4Days - sdCloseChangePast4Days.Average) / sdCloseChangePast4Days.StandardDeviation;
					dayData.TickerVolToday = (dayData.TickerVolToday - sdVolToday.Average) / sdVolToday.StandardDeviation;
					dayData.TickerVolYesterday = (dayData.TickerVolYesterday - sdVolYesterday.Average) / sdVolYesterday.StandardDeviation;
					dayData.TickerVolPast2Days = (dayData.TickerVolPast2Days - sdVolPast2Days.Average) / sdVolPast2Days.StandardDeviation;
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

					dayData.AverageRelationVolToday =
						relations.Select(relation => tickerToData[relation].TickerVolToday).Average();

					dayData.AverageRelationVolYesterday =
						relations.Select(relation => tickerToData[relation].TickerVolYesterday).Average();

					dayData.AverageRelationVolPast2Days =
						relations.Select(relation => tickerToData[relation].TickerVolPast2Days).Average();

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