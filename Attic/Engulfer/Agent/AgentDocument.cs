namespace Engulfer.Agent
{
	public class AgentDocument
	{
		#region Public Properties

		public string RedirectUri { get; set; }

		public string ResponseString { get; set; }

		public string Uri { get; set; }

		#endregion

		#region Public Methods

		public override string ToString()
		{
			return ResponseString + "\n\tRedirectUri = " + RedirectUri;
		}

		#endregion
	}
}
