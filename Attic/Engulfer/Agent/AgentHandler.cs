using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using A123Lib.A123Core.Agent;
using Mono.Web;

namespace Engulfer.Agent
{
	public class AgentHandler
	{
		#region Constants and Fields

		private static AgentHandler _instance;

		#endregion

		#region Constructors and Destructors

		private AgentHandler()
		{
		}

		#endregion

		#region Public Properties

		public static AgentHandler Instance => _instance ?? (_instance = new AgentHandler());

		#endregion

		#region Public Methods

		public AgentDocument PerformAction(AgentSession session, AgentAction action, int timeout)
		{
			AgentDocument document;

			// these resources must be released if any exceptions occur
			HttpWebResponse actionResponse = null;
			Stream responseStream = null;

			try
			{
				// set up the request and its headers
				var actionRequest = (HttpWebRequest)WebRequest.Create(new Uri(action.WebURL));

				if (!string.IsNullOrEmpty(action.Referer))
				{
					actionRequest.Referer = action.Referer;
				}

				if (action.IsPost)
				{
					actionRequest.Method = "POST";
				}
				else
				{
					actionRequest.Method = "GET";
				}

				actionRequest.ContentType = "application/x-www-form-urlencoded";

				if (action.UserAgent == null)
				{
					actionRequest.UserAgent =
						"Mozilla/5.0 (Windows NT 6.3; WOW64; rv:29.0) Gecko/20100101 Firefox/29.0";
				}
				else
				{
					actionRequest.UserAgent = action.UserAgent;
				}

				actionRequest.Accept =
					"text/xml,application/xml,application/xhtml+xml,text/html;q=0.9,text/plain;q=0.8,video/x-mng,image/png,image/jpeg,image/gif;q=0.2,*/*;q=0.1";
				actionRequest.AllowAutoRedirect = action.AllowRedirect;
				actionRequest.KeepAlive = false;

				if (!string.IsNullOrEmpty(action.ProxyAddress))
				{
					var parts = action.ProxyAddress.Split(':');

					if (parts.Length == 2)
					{
						actionRequest.Proxy = new WebProxy(parts[0], int.Parse(parts[1]));
					}
					else
					{
						actionRequest.Proxy = new WebProxy(parts[0]);
					}
				}

				if (timeout > 0)
				{
					actionRequest.Timeout = timeout;
				}

				actionRequest.Headers.Add("Accept-Language", "en-us,en;q=0.5");
				actionRequest.ProtocolVersion = new Version(action.Version);

				actionRequest.CookieContainer = session.Cookies;

				if (action.IsPost)
				{
					if (action.Attachment == null)
					{
						WriteContent(action, actionRequest);
					}
					else
					{
						WriteFileAttachmentContent(action, actionRequest);
					}
				}

				document = new AgentDocument();
				actionRequest.ServicePoint.Expect100Continue = false;
				actionResponse = (HttpWebResponse)actionRequest.GetResponse();

				document.RedirectUri = action.AllowRedirect
					                       ? actionResponse.ResponseUri.ToString()
					                       : actionResponse.GetResponseHeader("Location");

				responseStream = actionResponse.GetResponseStream();
				Debug.Assert(responseStream != null, "responseStream != null");
				var responseBuilder = new StringWriter();

				var buffer = new byte[1024];

				int n;
				do
				{
					n = responseStream.Read(buffer, 0, buffer.Length);

					var charBuffer = new char[buffer.Length];
					for (var i = 0; i < buffer.Length; i++)
					{
						charBuffer[i] = (char)buffer[i];
					}

					responseBuilder.Write(charBuffer, 0, n);
				} while (n > 0);

				document.ResponseString = responseBuilder.GetStringBuilder().ToString();
				responseBuilder.Close();
				responseBuilder.Dispose();

				document.Uri = actionResponse.ResponseUri.ToString();
			}
			finally
			{
				try
				{
					responseStream?.Close();
				}
				catch
				{
					// ignored
				}

				try
				{
					actionResponse?.Close();
				}
				catch
				{
					// ignored
				}
			}

			return document;
		}

		// *** Public API ***
		public AgentDocument PerformAction(AgentSession session, AgentAction action)
		{
			return PerformAction(session, action, 0);
		}

		#endregion

		#region Methods

		private static void WriteContent(AgentAction action, HttpWebRequest request)
		{
			var content =
				action.Elements.Select(x => x.Item1 + "=" + HttpUtility.UrlEncode(x.Item2)).Aggregate((x, y) => x + "&" + y);
			var writer = new StreamWriter(request.GetRequestStream());
			writer.Write(content);
			writer.Flush();
			writer.Close();
		}

		private static void WriteFileAttachmentContent(AgentAction action, HttpWebRequest wr)
		{
			var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
			var boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

			wr.ContentType = "multipart/form-data; boundary=" + boundary;
			wr.KeepAlive = true;
			wr.Credentials = CredentialCache.DefaultCredentials;

			var rs = wr.GetRequestStream();

			const string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
			
			foreach (var elements in action.Elements)
			{
				rs.Write(boundarybytes, 0, boundarybytes.Length);
				var formitem = string.Format(formdataTemplate, elements.Item1, elements.Item2);
				var formitembytes = Encoding.UTF8.GetBytes(formitem);
				rs.Write(formitembytes, 0, formitembytes.Length);
			}

			rs.Write(boundarybytes, 0, boundarybytes.Length);

			const string headerTemplate =
				"Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
			
			// unfinished - do content-type lookups for file extensions
			var header = string.Format(
				headerTemplate, action.Attachment.AttachmentName, action.Attachment.AttachmentFileName, "image/jpeg");
			
			var headerbytes = Encoding.UTF8.GetBytes(header);
			
			rs.Write(headerbytes, 0, headerbytes.Length);

			using (var client = new WebClient())
			{
				var fileStream = client.OpenRead(action.Attachment.AttachmentFileUrl);

				var buffer = new byte[4096];
				int bytesRead;
				while (fileStream != null && (bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
				{
					rs.Write(buffer, 0, bytesRead);
				}
				fileStream?.Close();
			}
			var trailer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
			rs.Write(trailer, 0, trailer.Length);
			rs.Close();
		}

		#endregion
	}
}
