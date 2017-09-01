using System;

namespace A123Lib.A123Core.Agent
{
	public static class UserAgentUtil
	{
		#region Constants and Fields

		private static readonly Random RandomInstance = new Random();

		private static readonly string[] UserAgents =
			{
				"Mozilla/5.0 (Windows NT 6.3; WOW64; rv:29.0) Gecko/20100101 Firefox/29.0",
				"Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.1916.114 Safari/537.36",
				"Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko"
			};

		#endregion

		#region Public Methods

		public static string RandomUserAgent()
		{
			return UserAgents[RandomInstance.Next(0, UserAgents.Length)];
		}

		#endregion
	}
}
