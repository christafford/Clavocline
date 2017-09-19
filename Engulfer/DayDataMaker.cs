using System;
using System.Collections.Generic;
using System.Linq;

namespace Engulfer
{
	public class DayDataMaker
	{
		private readonly Dictionary<int, List<EodEntry>> dataEachDay;
		private Dictionary<string, HashSet<string>> parentToRelations;
		private List<string> allTickers;
		
		public DayDataMaker()
		{
			allTickers = new List<string>();
			dataEachDay = new Dictionary<int, List<EodEntry>>();
			parentToRelations = new Dictionary<string, HashSet<string>>();
		}

		public void Init()
		{
			dataEachDay.Clear();
			
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
							dataEachDay[count++] = byDay[day].ToList();
						}
					}

					allTickers = allDays.Select(x => x.Ticker).Distinct().ToList();

					foreach (var ticker in allTickers)
					{
						var relationships = db.GetRelations(ticker, connection);
						parentToRelations[ticker] = new HashSet<string>(relationships.Select(x => x.RelatedTicker));
					}
				}
			}
		}

		public List<DayData> GetDatas()
		{
			if (!dataEachDay.Any())
			{
				Init();
			}

			for (var count = 10; count < dataEachDay.Count - 1; count++)
			{
				var tenDays = dataEachDay[count - 10];
				var fiveDays = dataEachDay[count - 5];
				var previousDay = dataEachDay[count - 1];
				var currentDay = dataEachDay[count];
				var tomorrow = dataEachDay[count + 1];
			}

			throw new NotImplementedException();
		}
	}
}