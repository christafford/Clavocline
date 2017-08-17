using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Mono.Web;

namespace Engulfer.Agent
{
	/// <summary>
	///     Collection of text manipulation methods
	/// </summary>
	public static class TextUtil
	{
		#region Constants and Fields

		public static readonly char[] EmailAddressSplitChars = { ',', ';' };

		private static readonly Lazy<Regex> ExtractDomainNamePattern =
			new Lazy<Regex>(() => new Regex(@"^([a-zA-Z]+:\/\/)?([^\/]+)\/.*?$"));

		private static readonly Lazy<Regex> HRefPattern =
			new Lazy<Regex>(
				() =>
					new Regex(@"href\s*=\s*(?:[""'](?<url>[^""']*)[""']|(?<url>\S+))", RegexOptions.IgnoreCase | RegexOptions.Compiled));

		private static readonly Lazy<Regex> HtmlSpecialCharPattern = new Lazy<Regex>(() => new Regex("&#([0-9]+);"));

		private static readonly Lazy<Regex> HtmlTagPattern = new Lazy<Regex>(() => new Regex("<[^>]*>", RegexOptions.None));

		private static readonly Lazy<Regex> HttpURLPattern =
			new Lazy<Regex>(() => new Regex("((http(s?)\\://){1}\\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled));

		private static readonly Lazy<Regex> ObjAlphaNumericPattern = new Lazy<Regex>(() => new Regex("[^a-zA-Z0-9_]"));

		private static readonly string[] PermittedCraigslistHtmlTags =
		{
			"b", "u", "i", "br", "hr", "h1", "h2", "h3", "h4",
			"h5", "h6", "ul", "ol", "li", "big", "small"
		};

		private static readonly Lazy<Regex> QuotedPrintablePattern =
			new Lazy<Regex>(() => new Regex("((\\=([0-9A-F][0-9A-F]))*)", RegexOptions.IgnoreCase));

		private static readonly Lazy<Regex> SocialSecurityPattern = new Lazy<Regex>(() => new Regex(@"^\d{3}-\d{2}-\d{4}$"));

		private static readonly Lazy<Regex> ValidEmailPattern =
			new Lazy<Regex>(() => new Regex(@"\b([A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4})\b", RegexOptions.IgnoreCase));

		private static readonly Lazy<Regex> ValidFloatPattern = new Lazy<Regex>(() => new Regex(@"^[0-9,\.]+$"));

		private static readonly Lazy<Regex> ZipCodePattern = new Lazy<Regex>(() => new Regex(@"^\d{5}([\-]\d{4})?$"));

		#endregion

		#region Public Methods

		public static string AlphaNumeric(string text, char? replacement = null)
		{
			if (text == null)
			{
				return string.Empty;
			}

			var builder = new StringBuilder();
			foreach (var c in text)
			{
				if (c >= 'A' && c <= 'Z')
				{
					builder.Append((char)(c - ('A' - 'a')));
				}
				else if (c >= 'a' && c <= 'z')
				{
					builder.Append(c);
				}
				else if (c >= '0' && c <= '9')
				{
					builder.Append(c);
				}
				else if (replacement.HasValue)
				{
					builder.Append(replacement.Value);
				}
			}

			return builder.ToString();
		}

		public static string Ascii(string input)
		{
			return input == null ? null : Regex.Replace(input, @"[^\u0000-\u007F]", string.Empty);
		}

		public static string ByteArrayToHexadecimalString(byte[] bytes)
		{
			var sBuilder = new StringBuilder(bytes.Length * 2);
			foreach (var element in bytes)
			{
				sBuilder.Append(element.ToString("x2"));
			}

			return sBuilder.ToString();
		}

		public static string CreateRandomAlphaNumeric(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			var random = new Random();
			return new string(
				Enumerable.Repeat(chars, length)
					.Select(s => s[random.Next(s.Length)])
					.ToArray());
		}

		public static bool DataChanged(string original, string current, bool ignoreCase = true)
		{
			if (string.IsNullOrWhiteSpace(original) && string.IsNullOrWhiteSpace(current))
			{
				return false;
			}

			var o = original?.Trim();
			var c = current?.Trim();

			var comparer = ignoreCase 
				? StringComparison.InvariantCultureIgnoreCase 
				: StringComparison.InvariantCulture;

			return !string.Equals(o, c, comparer);
		}

		// based on http://webandlife.blogspot.com/2011/12/refactoring-legacy-unsafe-c-codes-into.html
		public static string DecodeQuotedPrintable(string encoded)
		{
			if (string.IsNullOrEmpty(encoded))
			{
				return encoded;
			}

			var decoded = encoded.Replace("=" + Environment.NewLine, "");

			return QuotedPrintablePattern
				.Value
				.Replace(decoded, HexDecoderEvaluator);
		}

		public static string ExtractDomainNameFromUrl(string url)
		{
			return ExtractDomainNamePattern.Value.Replace(url, "$2");
		}

		public static string FixCurrency(string currency)
		{
			double val = 0;

			if ((currency.Trim() != string.Empty) && (currency != "0.00"))
			{
				double.TryParse(currency, NumberStyles.Currency, CultureInfo.CurrentCulture, out val);
			}

			return val.ToString("c");
		}

		public static string FixCurrency(int currency)
		{
			return $"{currency:C}";
		}

		public static string FixCurrency(decimal currency)
		{
			currency = Math.Round(currency, 2);
			return FixCurrency(currency.ToString());
		}

		public static string FormatNumber(int number)
		{
			var numberStr = number.ToString(CultureInfo.InvariantCulture);
			if (numberStr.Length > 3)
			{
				numberStr = numberStr.Substring(0, numberStr.Length - 3) + "," + numberStr.Substring(numberStr.Length - 3);
			}

			if (numberStr == "0")
			{
				return string.Empty;
			}

			return numberStr;
		}

		public static string GetCleanValueOrNull(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
		}

		public static string[] GetLinks(string html, bool plainText = false)
		{
			if (string.IsNullOrEmpty(html))
			{
				return null;
			}

			var hrefs = new List<string>();

			var match = plainText
				? HttpURLPattern.Value.Match(html)
				: HRefPattern.Value.Match(html);
			while (match.Success)
			{
				hrefs.Add(match.Groups[1].ToString());
				match = match.NextMatch();
			}

			return hrefs.ToArray();
		}

		public static string HTMLEmailLink(string email)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				return null;
			}

			return "<a href=\"mailto:" + email + "\">" + email + "</a>";
		}

		/// <summary>
		///     Encode Html
		/// </summary>
		/// <param name="text">
		///     The text to encode
		/// </param>
		/// <param name="replaceNewlinesWithBreakEntity">
		///     Replace new line matches with BR Html tag
		/// </param>
		/// <returns>
		///     The encoded text e.g. '&gt;' became '&gt;'
		/// </returns>
		public static string HtmlEncode(string text, bool replaceNewlinesWithBreakEntity = false)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}

			text = HttpUtility.HtmlEncode(text);

			return text.Replace("\r", string.Empty).Replace("\n", replaceNewlinesWithBreakEntity ? "<br />" : string.Empty);
		}

		public static string HTMLEncodeSpecialChars(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}

			var sb = new StringBuilder();
			foreach (var c in text)
			{
				if (c > 127)
				{
					// special chars
					sb.Append($"&#{(int)c};");
				}
				else
				{
					sb.Append(c);
				}
			}

			return sb.ToString();
		}

		public static string HTMLDecodeSpecialChars(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}

			var decodedText = text;
			var matches = HtmlSpecialCharPattern.Value.Matches(text);

			foreach (Match match in matches)
			{
				var intVal = int.Parse(match.Result("$1"));
				var charVal = (char)intVal;

				decodedText = decodedText.Replace(match.Value, charVal.ToString());
			}

			return decodedText;
		}

		public static string ImportScrub(int num)
		{
			return num < 1 ? "--" : num.ToString(CultureInfo.InvariantCulture);
		}

		public static string ImportScrub(DateTime date)
		{
			return date == DateTime.MinValue ? "--" : date.ToString("g");
		}

		public static bool IsAlphaNumeric(string strToCheck)
		{
			return !ObjAlphaNumericPattern.Value.IsMatch(strToCheck);
		}

		/// <summary>
		///     Calculates the check sum for a VIN, and determines if the VIN passes the checksum: Sum of products for all weighted
		///     values of digits 1-8 and 10-17
		/// </summary>
		/// <param name="stringToCheck">
		///     The string To Check.
		/// </param>
		/// <returns>
		///     The is valid check sum for vin.
		/// </returns>
		public static bool IsValidCheckSumForVin(string stringToCheck)
		{
			if (stringToCheck.Length != 17)
			{
				return false;
			}

			var vinToCheck = stringToCheck.ToLower().ToCharArray();

			if (!char.IsNumber(vinToCheck[8]) && vinToCheck[8] != 'x')
			{
				return false;
			}

			var ninthChar = (vinToCheck[8] == 'x') ? 10 : int.Parse(vinToCheck[8].ToString());
			var alphaPool = new[]
			{
				'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'n', 'p', // allowable alpha digits
				'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
			};
			var alphaValue = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 7, 9, 2, 3, 4, 5, 6, 7, 8, 9 };

			// corresponding numeric value for alpha digits
			var digitWeight = new[] { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 }; // corresponding weight of VIN digits
			var vinValueArray = new int[17];

			var totalWeight = 0;

			for (var i = 0; i < 17; i++)
			{
				if (i == 8)
				{
					continue;
				}

				if (char.IsNumber(vinToCheck[i]))
				{
					vinValueArray[i] = int.Parse(vinToCheck[i].ToString());
				}
				else
				{
					var goodAlpha = false;

					for (var j = 0; j < 23; j++)
					{
						if (vinToCheck[i] == alphaPool[j])
						{
							vinValueArray[i] = alphaValue[j];
							goodAlpha = true;
							j = 23;
						}
					}

					if (!goodAlpha)
					{
						return false;
					}
				}

				totalWeight += vinValueArray[i] * digitWeight[i];
			}

			return totalWeight % 11 == ninthChar;
		}

		public static bool IsValidEmailAddress(string strToCheck)
		{
			return !string.IsNullOrEmpty(strToCheck) &&
			       ValidEmailPattern.Value.Replace(strToCheck, string.Empty, 1) == string.Empty;
		}

		public static bool IsValidSocialSecurityNumber(string number)
		{
			return !string.IsNullOrEmpty(number) && SocialSecurityPattern.Value.IsMatch(number);
		}

		public static bool IsValidZipCode(string zipCode)
		{
			return !string.IsNullOrEmpty(zipCode) && ZipCodePattern.Value.IsMatch(zipCode);
		}

		public static int LevenshteinDistance(string s, string t)
		{
			var n = s.Length;
			var m = t.Length;
			var d = new int[n + 1, m + 1];

			if (n == 0)
			{
				return m;
			}

			if (m == 0)
			{
				return n;
			}

			for (var i = 0; i <= n; d[i, 0] = i++)
			{
			}

			for (var j = 0; j <= m; d[0, j] = j++)
			{
			}

			for (var i = 1; i <= n; i++)
			{
				for (var j = 1; j <= m; j++)
				{
					var cost = t.Substring(j - 1, 1) == s.Substring(i - 1, 1) ? 0 : 1;
					d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
				}
			}

			return d[n, m];
		}

		public static HtmlDocument LoadHtmlDocument(string html)
		{
			// fix to allow parsing multiple forms (http://stackoverflow.com/questions/2385840/)
			HtmlNode.ElementsFlags.Remove("form");

			var doc = new HtmlDocument
			{
				OptionDefaultStreamEncoding = Encoding.UTF8,
				OptionFixNestedTags = true
			};

			doc.LoadHtml(html);

			return doc;
		}

		public static string ObfuscateCreditCard(string cardNumber)
		{
			if (cardNumber.StartsWith("*") || cardNumber.Length < 12)
			{
				return cardNumber;
			}

			return cardNumber.Substring(cardNumber.Length - 4).PadLeft(cardNumber.Length, '*');
		}

		public static string[] ParseCsv(string csv)
		{
			if (string.IsNullOrWhiteSpace(csv))
			{
				return new string[0];
			}

			return csv.Split(',').Select(x => x.Trim()).OrderBy(x => x).Distinct().ToArray();
		}

		public static string RemoveLineBreaks(string unclean)
		{
			return unclean.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\"", "'").Replace("\\n", "<br>");
		}

		public static string RemoveNonDigitCharacters(string text)
		{
			return text == null ? string.Empty : new string(text.Where(char.IsDigit).ToArray());
		}

		/// <summary>
		///     Sanitize data for use in JavaScript. This will prevent data from breaking scripts.
		/// </summary>
		/// <param name="text">
		///     The data to sanitize
		/// </param>
		/// <param name="preserveNewLines">
		///     Escape newlines instead of stripping them.
		/// </param>
		/// <returns>
		///     The data sanitized for JavaScript.
		/// </returns>
		public static string SanitizeJs(string text, bool preserveNewLines = false)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}

			text = text.Replace(@"\", @"\\").Replace("\"", "\\\"").Replace("'", @"\'");
			return preserveNewLines
				? text.Replace("\r", "\\r").Replace("\n", "\\n")
				: text.Replace("\r", " ").Replace("\n", " ");
		}

		/// <summary>
		///     Removes any html tags which we don't want web site users entering
		///     For Now: script, frame, iframe, frameset, meta
		/// </summary>
		/// <returns>
		///     A string with disallowed tags removed.
		/// </returns>
		public static string SanitizeUserHTML(string html)
		{
			if (string.IsNullOrEmpty(html))
			{
				return string.Empty;
			}

			var lastResult = string.Empty;
			while (lastResult != html)
			{
				lastResult = html;
				html = Regex.Replace(html, @"</?(script|frameset|frame|iframe|meta)[^>]*>+", string.Empty, RegexOptions.IgnoreCase);
			}

			return html;
		}

		public static string Scrub(string str)
		{
			return string.IsNullOrWhiteSpace(str) ? "--" : str;
		}

		public static string Scrub(decimal dec)
		{
			return dec < 1 ? "--" : dec.ToString("C");
		}

		public static string Scrub(DateTime inInventorySince)
		{
			return inInventorySince == DateTime.MinValue ? "--" : inInventorySince.ToString("d");
		}

		public static string Scrub(int? value)
		{
			return value?.ToString() ?? "--";
		}

		public static byte[] StringToByteArray(string str)
		{
			var bytes = new byte[str.Length * sizeof(char)];
			Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);

			return bytes;
		}

		public static string StripHTMLTags(string html)
		{
			return string.IsNullOrEmpty(html)
				? html
				: HtmlTagPattern.Value
					.Replace(html, string.Empty)
					.Replace("&nbsp;", " ");
		}

		public static string StripHTMLTags(string html, bool keepLineBreaks)
		{
			if (string.IsNullOrEmpty(html))
			{
				return html;
			}

			if (!keepLineBreaks)
			{
				return StripHTMLTags(html);
			}

			var htmlDocument = LoadHtmlDocument(html);

			// Remove all attributes from <p> tags and replace all empty <p> tags with <br> tags
			var pTagNodes = htmlDocument.DocumentNode.Descendants("p").ToList();
			foreach (var pNode in pTagNodes)
			{
				if (string.IsNullOrWhiteSpace(pNode.InnerHtml))
				{
					var breakNode = HtmlNode.CreateNode("<br></br>");
					pNode.ParentNode.ReplaceChild(breakNode, pNode);
					continue;
				}

				var newNode = HtmlNode.CreateNode("<p></p>");
				newNode.InnerHtml = pNode.InnerHtml;
				pNode.ParentNode.ReplaceChild(newNode, pNode);
			}

			html = htmlDocument.DocumentNode.InnerHtml;

			return string.IsNullOrEmpty(html)
				? html
				: HtmlTagPattern.Value
					.Replace(html, PreserveLineBreaks)
					.Replace("&nbsp;", " ");
		}

		/// <summary>
		///     Strip all prohibited tag which are not allowed to post on Craigslist
		/// </summary>
		/// <param name="html">Raw html needs to be stripped</param>
		/// <returns>Return the stripped html string contains only allowed tags</returns>
		public static string StripProhibitedCraigslistHtmlTags(string html)
		{
			if (string.IsNullOrEmpty(html))
			{
				return html;
			}

			try
			{
				var doc = LoadHtmlDocument(html);

				// remove all prohibited tags with <p> tags and replace all <strong> tags with <b> tags
				var allElements = doc.DocumentNode.SelectNodes("//*");
				if (allElements != null)
				{
					foreach (var element in allElements)
					{
						// remove all inline CSS
						element.Attributes.RemoveAll();

						// only replace if they are prohibited tags
						if (PermittedCraigslistHtmlTags.Contains(element.Name))
						{
							continue;
						}

						// somehow, Microsoft Edge or stupid IE uses <strong> instead of <b> to make the text bold. So make sure to use <b> which is allowed on CR
						var node = element.Name == "strong" ? HtmlNode.CreateNode("<b></b>") : doc.CreateTextNode("");
						node.InnerHtml = element.InnerHtml;
						element.ParentNode.ReplaceChild(node, element);
					}
				}

				html = doc.DocumentNode.InnerHtml;

				return string.IsNullOrEmpty(html)
					? html
					: HtmlTagPattern.Value
						.Replace(
							html,
							m =>
							{
								var tag = HtmlNode.CreateNode(m.ToString().ToLower());
								if (tag != null)
								{
									if (PermittedCraigslistHtmlTags.Contains(tag.Name))
									{
										return "<" + tag.Name + ">";
									}
									return string.Empty;
								}
								return m.ToString();
							})
						.Replace("&nbsp;", " ");
			}
			catch (Exception ex)
			{
				throw new Exception("Error while stripping prohibited craigslist html tags");
			}
		}

		public static string ToCurrencyWithOptionalCents(decimal value)
		{
			var hasCents = Math.Round(value) != value;
			return value.ToString(hasCents ? "c" : "c0");
		}

		public static bool ToDecimalFromString(string encodedValue, out decimal result)
		{
			var onlyNumerics = new StringBuilder();

			foreach (var c in encodedValue)
			{
				if ((c >= '0' && c <= '9') || c == '.')
				{
					onlyNumerics.Append(c);
				}
			}

			return decimal.TryParse(onlyNumerics.ToString(), out result);
		}

		public static string ToNonBreakingHtml(string text)
		{
			// only spaces and hyphens are commonly used candidates for line breaking
			const string nonBreakingSpace = "&nbsp;";
			const string nonBreakingHyphen = "&#8209;";

			return string.IsNullOrEmpty(text)
				? string.Empty
				: text.Replace(" ", nonBreakingSpace).Replace("-", nonBreakingHyphen);
		}

		public static string ToNumbersOnly(string str)
		{
			var builder = new StringBuilder();
			var strChars = str.ToCharArray();

			foreach (var c in strChars)
			{
				if (c >= '0' && c <= '9')
				{
					builder.Append(c);
				}
			}

			return builder.ToString();
		}

		public static string Truncate(string text, int length)
		{
			return text == null || text.Length <= length ? text : text.Substring(0, length);
		}

		public static string UrlEncode(string text, bool excludeSpaceFromEncode = true)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}

			text = text.Replace("\r", string.Empty).Replace("\n", string.Empty);
			text = HttpUtility.UrlEncode(text);
			if (excludeSpaceFromEncode)
			{
				// For some reason, some mailto apps (Gmail/Outlook) don't decode the encoded spaces. Hopefully this trick made it works for all.
				text = text.Replace('+', ' ');
			}

			return text;
		}

		#endregion

		#region Methods

		private static string HexDecoderEvaluator(Match m)
		{
			if (string.IsNullOrEmpty(m.Value))
			{
				return null;
			}

			var captures = m.Groups[3].Captures;
			var bytes = new byte[captures.Count];

			for (var i = 0; i < captures.Count; i++)
			{
				bytes[i] = Convert.ToByte(captures[i].Value, 16);
			}

			return Encoding.UTF8.GetString(bytes);
		}

		private static string PreserveLineBreaks(Match m)
		{
			var tag = AlphaNumeric(m.ToString());

			if (tag == "p" || tag == "br")
			{
				return m.ToString();
			}

			return string.Empty;
		}

		#endregion
	}
}
