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

			for (var count = 10; count < _dataEachDay.Count - 1; count++)
			{
				stopwatch.Restart();

				if (_log)
				{
					Console.Write($"Day {count} of {_dataEachDay.Count - 1}...");
				}

				var tenDays = _dataEachDay[count - 10];
				var fiveDays = _dataEachDay[count - 5];
				var previousDay = _dataEachDay[count - 1];
				var currentDay = _dataEachDay[count];
				var tomorrow = _dataEachDay[count + 1];

				var tickerSubset = tenDays.Select(x => x.Ticker)
					.Intersect(fiveDays.Select(x => x.Ticker))
					.Intersect(previousDay.Select(x => x.Ticker))
					.Intersect(currentDay.Select(x => x.Ticker))
					.Intersect(tomorrow.Select(x => x.Ticker));

				var tickerToData = new Dictionary<string, DayData>();

				Parallel.ForEach(tickerSubset, ticker =>
				{
					var dayData = new DayData();
					var tickerCurrentDay = currentDay.First(x => x.Ticker.Equals(ticker));
					decimal temp;

					dayData.TickerChangePastDay = (double)
					((tickerCurrentDay.Close - (temp = previousDay.First(x => x.Ticker.Equals(ticker)).Close))
					 / temp);

					dayData.TickerChangePast5Days = (double)
					((tickerCurrentDay.Close - (temp = fiveDays.First(x => x.Ticker.Equals(ticker)).Close))
					 / temp);

					dayData.TickerChangePast10Days = (double)
					((tickerCurrentDay.Close - (temp = tenDays.First(x => x.Ticker.Equals(ticker)).Close))
					 / temp);

					dayData.TickerChangeNext = (double)
					((tomorrow.First(x => x.Ticker.Equals(ticker)).Close - tickerCurrentDay.Close)
					 / tickerCurrentDay.Close);
					
					lock (this)
					{
						tickerToData[ticker] = dayData;
					}
				});

				var allDataForDay = tickerToData.Values.ToList();

				var sdChangePastDay = StandardDeviation(allDataForDay.Select(x => x.TickerChangePastDay).ToList());
				var sdChangePast5Days = StandardDeviation(allDataForDay.Select(x => x.TickerChangePast5Days).ToList());
				var sdChangePast10Days = StandardDeviation(allDataForDay.Select(x => x.TickerChangePast10Days).ToList());
				var sdChangeNext = StandardDeviation(allDataForDay.Select(x => x.TickerChangeNext).ToList());

				tickerToData.Values.ToList().ForEach(dayData =>
				{
					dayData.TickerChangePastDay /= sdChangePastDay;
					dayData.TickerChangePast5Days /= sdChangePast5Days;
					dayData.TickerChangePast10Days /= sdChangePast10Days;
					dayData.TickerChangeNext /= sdChangeNext;
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
					
					dayData.AverageRelationChangePastDay =
						relations.Select(relation => tickerToData[relation].TickerChangePastDay).Average();

					dayData.AverageRelationChangePast5Days =
						relations.Select(relation => tickerToData[relation].TickerChangePast5Days).Average();

					dayData.AverageRelationChangePast10Days =
						relations.Select(relation => tickerToData[relation].TickerChangePast10Days).Average();

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

		private static double StandardDeviation(List<double> someDoubles)
		{
			var average = someDoubles.Average();
			var sumOfSquaresOfDifferences = someDoubles.Select(val => (val - average) * (val - average)).Sum();
			return Math.Sqrt(sumOfSquaresOfDifferences / someDoubles.Count());
		}
	}
}