using System;
using System.Net;

namespace A123Lib.A123Core.Agent
{
	[Serializable]
	public class AgentSession
	{
		#region Constructors and Destructors

		public AgentSession()
		{
			Cookies = new CookieContainer();
		}

		#endregion

		#region Public Properties

		public CookieContainer Cookies { get; set; }

		#endregion
	}
}
