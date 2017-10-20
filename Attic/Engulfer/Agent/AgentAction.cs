using System;
using System.Collections.Generic;
using System.Linq;

namespace Engulfer.Agent
{
	public class AgentAction
	{
		#region Constants and Fields

		public bool AllowRedirect = true;

		public string Version = "1.1";

		#endregion

		#region Constructors and Destructors

		public AgentAction(string url) : this(url, true)
		{
		}

		public AgentAction(string url, bool isPost)
		{
			WebURL = url;
			IsPost = isPost;
			Elements = new List<Tuple<string, string>>();
		}

		#endregion

		#region Public Properties

		public AgentAttachment Attachment { get; set; }

		public List<Tuple<string, string>> Elements { get; set; }

		public bool IsPost { get; set; }

		public string ProxyAddress { get; set; }

		public string Referer { get; set; }

		public string UserAgent { get; set; }

		public string WebURL { get; set; }

		#endregion

		#region Public Methods

		public void AddParam(string key, string requestValue)
		{
			Elements.RemoveAll(x => x.Item1 == key);
			Elements.Add(new Tuple<string, string>(key, requestValue));
		}

		public override string ToString()
		{
			var output = "URL: " + WebURL;

			return Elements.Aggregate(
				output, (current, element) => current + ("\n\t\tParam: " + element.Item1 + " = " + element.Item2));
		}

		#endregion
	}
}
