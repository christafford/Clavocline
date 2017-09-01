using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Mono.Web;

namespace Engulfer.Agent
{
	[Serializable]
	public class FormContainer
	{
		#region Constants and Fields

		private static readonly Regex RxDomain = new Regex("(http[s]?\\://[^/]*?)/", RegexOptions.IgnoreCase);

		#endregion

		#region Constructors and Destructors

		private FormContainer()
		{
		}

		public FormContainer(HtmlNode form, string requestUrl)
		{
			RequestUrl = requestUrl;
			Form = form;
			Elements = new List<InputElement>();

			ParseForm();
		}

		#endregion

		#region Public Properties

		public bool DoPost { get; set; }

		public List<InputElement> Elements { get; set; }

		public string FormAction { get; set; }

		public string RequestUrl { get; set; }

		#endregion

		#region Properties

		private HtmlNode Form { get; set; }

		#endregion

		#region Public Methods

		public AgentAction CreateLocalAction()
		{
			var formElements = Elements.Where(
				element =>
					element.SendFieldInResponse &&
					!string.IsNullOrEmpty(element.Name) &&
					!string.IsNullOrEmpty(element.Value));

			var action = new AgentAction(FormAction, DoPost);

			if (DoPost)
			{
				foreach (var element in formElements)
				{
					action.AddParam(element.Name, element.Value);
				}
			}
			else
			{
				if (action.WebURL.Contains("?"))
				{
					action.WebURL += "&";
				}
				else
				{
					action.WebURL += "?";
				}

				foreach (var element in formElements)
				{
					action.WebURL += HttpUtility.UrlEncode(element.Name) + "=" + HttpUtility.UrlEncode(element.Value) + "&";
				}

				action.WebURL = action.WebURL.TrimEnd("&".ToCharArray());
			}

			return action;
		}

		public static IEnumerable<FormContainer> ParseAll(string html, string requestUrl)
		{
			return ParseAll(TextUtil.LoadHtmlDocument(html), requestUrl);
		}

		public static IEnumerable<FormContainer> ParseAll(HtmlDocument document, string requestUrl)
		{
			if (document == null)
			{
				return new FormContainer[0];
			}

			var forms = document.DocumentNode.SelectNodes("//form");
			return forms?.Select(form => new FormContainer(form, requestUrl)).ToArray() ?? new FormContainer[0];
		}

		public bool SetIfExists(string name, string value, bool truncateToMaxLength = false)
		{
			var element = Elements.FirstOrDefault(x => x.Name == name);
			if (element == null)
			{
				return false;
			}

			if (element.MaxLength.HasValue && value != null && value.Length > element.MaxLength.Value && truncateToMaxLength)
			{
				value = value.Substring(0, element.MaxLength.Value);
			}

			element.Value = value;
			return true;
		}

		#endregion

		#region Methods

		private void ParseForm()
		{
			SetFormAction();
			SetPostMethod();
			SetElements();
		}

		private void SetElements()
		{
			ParseInputs();
			ParseButtons();
			ParseSelects();
			ParseTextAreas();
		}

		private void ParseTextAreas()
		{
			var textAreas = Form.SelectNodes(".//textarea");
			if (textAreas == null)
			{
				return;
			}

			foreach (var textArea in textAreas)
			{
				AddElement(
					new InputElement(InputElement.TagTypeEnum.TextArea)
					{
						Name = textArea.GetAttributeValue("name", null),
						Value = HttpUtility.HtmlDecode(textArea.InnerText)
					});
			}
		}

		private void ParseSelects()
		{
			var selects = Form.SelectNodes(".//select");
			if (selects == null)
			{
				return;
			}

			foreach (var select in selects)
			{
				var options = select.SelectNodes(".//option");
				if (options == null)
				{
					continue;
				}

				var name = select.GetAttributeValue("name", null);
				var dropdown = new InputElement(InputElement.TagTypeEnum.DropDown)
				{
					Name = name,
					PossibleValues = new List<string>()
				};

				foreach (var option in options)
				{
					var value = option.GetAttributeValue("value", null);
					if (value != null)
					{
						dropdown.PossibleValues.Add(value);

						if (IsSelected(option))
						{
							dropdown.Value = value;
						}
					}
				}

				AddElement(dropdown);
			}
		}

		private void ParseInputs()
		{
			var inputs = Form.SelectNodes(".//input");
			if (inputs == null)
			{
				return;
			}

			foreach (var input in inputs)
			{
				var type = input.GetAttributeValue("type", null);
				var value = input.GetAttributeValue("value", null);
				var name = input.GetAttributeValue("name", null);

				switch (type?.ToLower())
				{
					case "password":
					case "text":
					case "number":
					case null:
						var maxLength = input.GetAttributeValue("maxlength", 0);
						AddElement(
							new InputElement(InputElement.TagTypeEnum.TextInput)
							{
								Name = name,
								Value = value,
								MaxLength = maxLength == 0 ? default(int?) : maxLength
							});
						break;
					case "hidden":
						AddElement(
							new InputElement(InputElement.TagTypeEnum.Hidden)
							{
								Name = name,
								Value = value
							});
						break;
					case "submit":
						AddElement(
							new InputElement(InputElement.TagTypeEnum.SubmitButton)
							{
								Name = name,
								Value = value
							});
						break;
					case "checkbox":
						AddElement(
							new InputElement(InputElement.TagTypeEnum.CheckBox)
							{
								Name = name,
								Value = IsChecked(input) ? "on" : string.Empty
							});
						break;
					case "radio":
						var radioButton = Elements.FirstOrDefault(e => !string.IsNullOrEmpty(e.Name) && e.Name == name);
						if (radioButton == null)
						{
							radioButton = new InputElement(InputElement.TagTypeEnum.RadioButton)
							{
								Name = name,
								PossibleValues = new List<string>()
							};

							AddElement(radioButton);
						}

						if (!string.IsNullOrEmpty(value))
						{
							radioButton.PossibleValues.Add(value);
						}

						if (IsChecked(input))
						{
							radioButton.Value = value;
						}

						break;
					default:
						continue;
				}
			}
		}

		private void ParseButtons()
		{
			var buttons = Form.SelectNodes(".//button");
			if (buttons == null)
			{
				return;
			}

			foreach (var button in buttons)
			{
				AddElement(
					new InputElement(InputElement.TagTypeEnum.Button)
					{
						Name = button.GetAttributeValue("name", null),
						Value = button.GetAttributeValue("value", null)
					});
			}
		}

		private static bool IsChecked(HtmlNode input)
		{
			if (!input.HasAttributes)
			{
				return false;
			}

			var checkedAttr = input.Attributes.FirstOrDefault(a => a.Name == "checked" || a.Name == "data-checked");
			if (checkedAttr == null)
			{
				return false;
			}

			var attrValue = checkedAttr.Value;
			return string.IsNullOrEmpty(attrValue) || attrValue == "1" || attrValue == "true" || attrValue == "checked";
		}

		private static bool IsSelected(HtmlNode option)
		{
			if (!option.HasAttributes)
			{
				return false;
			}

			var selectedAttr = option.Attributes.FirstOrDefault(a => a.Name == "selected");
			if (selectedAttr == null)
			{
				return false;
			}

			var attrValue = selectedAttr.Value;
			return string.IsNullOrEmpty(attrValue) || attrValue == "1" || attrValue == "true" || attrValue == "selected";
		}

		private void AddElement(InputElement element)
		{
			Elements.RemoveAll(x => x.Name == element.Name);
			Elements.Add(element);
		}

		private void SetPostMethod()
		{
			DoPost = Form.GetAttributeValue("method", string.Empty).ToLower() == "post";
		}

		private void SetFormAction()
		{
			var actionPath = Form.GetAttributeValue("action", null);

			if (string.IsNullOrEmpty(actionPath) || actionPath.Contains("://"))
			{
				FormAction = actionPath;
				return;
			}

			if (actionPath.StartsWith("//"))
			{
				var protocol = RequestUrl.StartsWith("https://") ? "https:" : "http:";
				FormAction = protocol + actionPath;
				return;
			}

			if (actionPath.StartsWith("/"))
			{
				var domain = RxDomain.Match(RequestUrl);

				if (!domain.Success)
				{
					FormAction = RequestUrl + actionPath;
					return;
				}

				FormAction = domain.Result("$1") + actionPath;
				return;
			}

			var lastdirectory = RequestUrl.LastIndexOf('/', 10);

			if (lastdirectory < 0)
			{
				FormAction = RequestUrl + '/' + actionPath;
				return;
			}

			FormAction = RequestUrl.Substring(0, lastdirectory + 1) + actionPath;
		}

		#endregion

		[Serializable]
		public class InputElement
		{
			#region Constructors and Destructors

			private InputElement()
			{
			}

			public InputElement(TagTypeEnum tagtype)
			{
				SendFieldInResponse = true;
				TagType = tagtype;
			}

			#endregion

			#region Enums

			public enum TagTypeEnum
			{
				TextInput,

				Hidden,

				TextArea,

				CheckBox,

				RadioButton,

				DropDown,

				ListBox,

				Button,

				SubmitButton
			}

			#endregion

			#region Public Properties

			public int? MaxLength { get; set; }

			public string Name { get; set; }

			public IList<string> PossibleValues { get; set; }

			public bool SendFieldInResponse { get; set; }

			public TagTypeEnum TagType { get; set; }

			public string Value { get; set; }

			#endregion
		}
	}
}
