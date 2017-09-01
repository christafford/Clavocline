using System;
using System.Linq;
using System.Text.RegularExpressions;
using A123Lib.A123Core.Agent;
using Engulfer.Agent;

namespace Engulfer
{
	internal class MainClass
	{
		public static void Main(string[] args)
		{
			var startDate = new DateTime(2017, 01, 01);
			var endDate = DateTime.Now.Add(TimeSpan.FromDays(-1)).Date;
			var exchange = "NYSE";
			var regexKey = new Regex("&k=([a-z0-9]*)&");

			const string templateUrl = "http://eoddata.com/data/filedownload.aspx?e={0}&sd={1}&ed={2}&d=1&k={3}&o=d&ea=1&p=0";
			
			var session = new AgentSession();
			var action = new AgentAction("http://eoddata.com/", false);
			var document = AgentHandler.Instance.PerformAction(session, action);
			var forms = FormContainer.ParseAll(document.ResponseString, document.Uri);
			var loginForm = forms.FirstOrDefault(x => x.Elements.Any(y => y.Name.EndsWith("txtEmail")) &&
			                                          x.Elements.Any(y => y.Name.EndsWith("txtPassword")) &&
			                                          x.Elements.Any(y => y.Name.EndsWith("btnLogin")));
			
			if (loginForm == null)
			{
				Console.WriteLine("Can't log in");
				return;
			}

			loginForm.Elements.First(x => x.Name.EndsWith("txtEmail")).Value = "christafford@gmail.com";
			loginForm.Elements.First(x => x.Name.EndsWith("txtPassword")).Value = "3b4f5w8i";

			action = loginForm.CreateLocalAction();
			action.WebURL = "http://eoddata.com/";
			
			var loggedIn = AgentHandler.Instance.PerformAction(session, action);

			if (!loggedIn.ResponseString.Contains("chris stafford"))
			{
				throw new Exception("Can't log in");
			}

			var downloadAction = new AgentAction("http://eoddata.com/download.aspx", false);
			var downloadResult = AgentHandler.Instance.PerformAction(session, downloadAction);
			var key = regexKey.Match(downloadResult.ResponseString).Result("$1");
			
			var downloadUrl = string.Format(
				templateUrl,
				exchange,
				startDate.ToString("yyyyMMdd"),
				endDate.ToString("yyyyMMdd"),
				key);

			var downloadDataAction = new AgentAction(downloadUrl, false);
			var data = AgentHandler.Instance.PerformAction(session, downloadDataAction);

			var parseDate = new Func<string, DateTime>(datetimestr => new DateTime(
				int.Parse(datetimestr.Substring(0, 4)), 
				int.Parse(datetimestr.Substring(4, 2)),
				int.Parse(datetimestr.Substring(6, 2))));
			
			using (var db = new MarketContext())
			{
				data.ResponseString.Split('\n').ToList().ForEach(line =>
				{
					var items = line.Split(',');
					var eoddata = new EodEntry
					{
						Ticker = items[0],
						Per = items[1],
						Date = parseDate(items[2]),
						Open = decimal.Parse(items[3]),
						High = decimal.Parse(items[4]),
						Low = decimal.Parse(items[5]),
						Close = decimal.Parse(items[6]),
						Volume = double.Parse(items[7])
					};

					db.EodEntries.Add(eoddata);
					
					db.SaveChanges();
				});
			}

			Console.WriteLine("Done");
		}
	}
}