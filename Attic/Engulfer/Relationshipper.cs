using System;
using System.Linq;
using System.Text.RegularExpressions;
using A123Lib.A123Core.Agent;
using Engulfer.Agent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Engulfer
{
	public class Relationshipper
	{
		public static void Run()
		{
			var regex = new Regex("streaming:\\[.*?\\]", RegexOptions.Singleline);
			
			using (var db = new MarketContext())
			{
				using (var connection = db.GetConnection())
				{
					connection.Open();
					
					var tickers = db.GetMarketplaceTickers(connection);
					const string financeUrl = "https://www.google.com/finance?q={0}";
					var session = new AgentSession();

					var count = 0;
					foreach (var ticker in tickers)
					{
						var transaction = connection.BeginTransaction();
						
						try
						{
							Console.Write($"{++count} of {tickers.Count}: {ticker} - ");
							var url = string.Format(financeUrl, ticker.Replace(":", "%3A"));

							var action = new AgentAction(url, false);

							var document = AgentHandler.Instance.PerformAction(session, action);

							var match = regex.Match(document.ResponseString);
							if (!match.Success)
							{
								Console.WriteLine("No relations!");
								continue;
							}

							var json = "{" + match + "}";
							var relatedItems = ((JObject) JsonConvert.DeserializeObject(json)).First?.First?.Children().AsJEnumerable()
								.ToList();
							
							if (relatedItems == null)
							{
								Console.WriteLine("\nERROR!");
								continue;
							}

							foreach (var relatedItem in relatedItems)
							{
								var values = relatedItem.Children().Select(x => (JProperty) x).ToDictionary(x => x.Name);
								var relatedTicker = values["s"].Value;
								var relatedExchange = values["e"].Value;

								if (ticker.EndsWith($":{relatedTicker}"))
								{
									continue;
								}

								Console.Write($"{relatedTicker} ");

								if (tickers.Contains($"{relatedExchange}:{relatedTicker}"))
								{
									db.AddRelationship(ticker, $"{relatedExchange}:{relatedTicker}", connection);
									continue;
								}

								var probableTicker = tickers.FirstOrDefault(x => x.EndsWith($":{relatedTicker}"));
								if (probableTicker != null)
								{
									db.AddRelationship(ticker, probableTicker, connection);
									Console.Write($"({probableTicker}) ");
								}
								else
								{
									Console.Write("! ");
								}
							}

							Console.WriteLine();
						}
						finally
						{
							transaction.Commit();
						}
					}
				}
			}

			Console.WriteLine("Done.");
			Console.ReadLine();
		}
	}
}