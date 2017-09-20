using System;
using System.Collections.Generic;
using System.Linq;

namespace Engulfer
{
	public class DayDataMaker
	{
		private readonly Dictionary<int, List<EodEntry>> _dataEachDay;
		private readonly Dictionary<string, HashSet<string>> _parentToRelations;
		private List<string> _allTickers;
		
		public DayDataMaker()
		{
			_allTickers = new List<string>();
			_dataEachDay = new Dictionary<int, List<EodEntry>>();
			_parentToRelations = new Dictionary<string, HashSet<string>>();
		}

		public void Init()
		{
			_dataEachDay.Clear();
			
			using (var db = new MarketContext())
			{
				using (var connection = db.GetConnection())
				{
					connection.Open();

					var allDays = db.EodEntries.ToList();

					var byDay = allDays.GroupBy(x => x.Date).ToDictionary(x => x.Key);

					var days = allDays.Select(x => x.Date).Distinct().OrderBy(x => x).ToList();

					var count = 0;
					
					foreach (var day in days)
					{
						// exclude holidays
						if (byDay[day].Any(x => x.Vol > 0))
						{
							_dataEachDay[count++] = byDay[day].ToList();
						}
					}

					_allTickers = allDays.Select(x => x.Ticker).Distinct().ToList();

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
				
			for (var count = 10; count < _dataEachDay.Count - 1; count++)
			{
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
				
				foreach (var ticker in tickerSubset)
				{
					var dayData = new DayData();
					var tickerCurrentDay = currentDay.First(ticker.Equals);
					
					dayData.TickerChangePastDay = (double) ((tickerCurrentDay.Close - previousDay.First(ticker.Equals).Close) /
					                                        previousDay.First(ticker.Equals).Close);
					
					dayData.TickerChangePast5Days = (double) ((tickerCurrentDay.Close - fiveDays.First(ticker.Equals).Close) /
					                                          fiveDays.First(ticker.Equals).Close);

					dayData.TickerChangePast10Days = (double) ((tickerCurrentDay.Close - tenDays.First(ticker.Equals).Close) /
					                                           tenDays.First(ticker.Equals).Close);
					
					dayData.TickerChangeNext =  (double) ((tomorrow.First(ticker.Equals).Close - tickerCurrentDay.Close) /
					                                      tickerCurrentDay.Close);
					
					tickerToData[ticker] = dayData;
				}

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
					
				tickerToData.Keys.ToList().ForEach(ticker =>
				{
					var dayData = tickerToData[ticker];
					var relations = _parentToRelations[ticker];

					dayData.AverageRelationChangePastDay =
						relations.Select(relation => tickerToData[relation].TickerChangePastDay).Average();
					
					dayData.AverageRelationChangePast5Days =
						relations.Select(relation => tickerToData[relation].TickerChangePast5Days).Average();
					
					dayData.AverageRelationChangePast10Days =
						relations.Select(relation => tickerToData[relation].TickerChangePast10Days).Average();
					
					dayDatas.Add(dayData);
				});
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