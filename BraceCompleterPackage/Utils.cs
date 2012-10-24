using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Diagnostics;
using EnvDTE;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ComponentModelHost;

namespace JoelSpadin.BraceCompleter
{


	/// <summary>
	/// Provides utilities and options needed by the brace completer
	/// </summary>
	internal class Utils
	{
		private static Utils _instance = null;
		internal static Utils Instance
		{
			get
			{
				if (_instance == null)
					_instance = new Utils();
				return _instance;
			}
		}

		/// <summary>
		/// Keeps track of the number of times the options page has been updated
		/// </summary>
		internal static uint OptionsVersion = 0;

		private DTE _dte = null;
		private Properties _packageProperties = null;
		private Properties _csharpProperties = null;

		//[Import]
		internal IContentTypeRegistryService _contentTypeRegistryService = null;

		//[Import]
		//internal SVsServiceProvider _serviceProvider = null; 


		/// <summary>
		/// Gets the DTE automation object
		/// </summary>
		public static DTE DTE
		{
			get
			{
				return Instance._dte;
			}
		}

		private static Properties PackageProperties
		{
			get { return Instance._packageProperties; }
			set { Instance._packageProperties = value; }
		}

		private static Properties CSharpProperties
		{
			get { return Instance._csharpProperties; }
			set { Instance._csharpProperties = value; }
		}

		private static IContentTypeRegistryService ContentTypeRegistryService
		{
			get { return Instance._contentTypeRegistryService; }
		}


		private Utils()
		{
			_dte = (DTE)ServiceProvider.GlobalProvider.GetService(typeof(DTE));

			IComponentModel componentModel = (IComponentModel)Package.GetGlobalService(
				typeof(SComponentModel));
			_contentTypeRegistryService = componentModel.GetService<IContentTypeRegistryService>();
		}

		/// <summary>
		/// Gets the language of the code in a textview
		/// </summary>
		/// <param name="textView"></param>
		/// <returns></returns>
		public static string GetLanguage(ITextView textView)
		{
			return textView.TextDataModel.ContentType.TypeName;
		}

		public static IEnumerable<IContentType> GetCodeTypes(string baseType = "code")
		{
			foreach (IContentType type in ContentTypeRegistryService.ContentTypes)
			{
				if (baseType == null)
				{
					yield return type;
					continue;
				}

				foreach (IContentType parent in type.BaseTypes)
				{
					if (parent.TypeName == baseType)
					{
						yield return type;
						break;
					}
				}
			}
		}

		/// <summary>
		/// Gets options for how brace completion should be executed
		/// </summary>
		/// <param name="textView"></param>
		/// <returns></returns>
		public static BraceOptions GetOptions(ITextView textView)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			
			BraceOptions options = new BraceOptions();

			//Get the language (datatype) from the textview
			options.Language = GetLanguage(textView);

			//Get language specific options
			if (PackageProperties == null)
				PackageProperties = DTE.get_Properties("Environment", "Brace Completion");

			options.ImmediateCompletion = (bool)PackageProperties.Item("ImmediateCompletion").Value;
			options.SmartFormat = (bool)PackageProperties.Item("SmartFormat").Value;
			
			switch (options.Language)
			{
			case "CSharp":
				if (CSharpProperties == null)
					CSharpProperties = DTE.get_Properties("TextEditor", "CSharp-Specific");
				options.CompleteBraces = (bool)PackageProperties.Item("CSharp").Value;
				options.IndentBraces = ParseBool(CSharpProperties.Item("Indent_Braces").Value);
				options.IndentBlock = ParseBool(CSharpProperties.Item("Indent_BlockContents").Value, true);
				break;
			case "C/C++":
				//As much as this is supposed to work, it doesn't :(
				//Properties langProperties = _dte.get_Properties("TextEditor", "C/C++ Specific");
				//options.indentBraces = ParseValue(langProperties.Item("IndentBraces").Value);
				//options.indentBlock = true;
				options.CompleteBraces = (bool)PackageProperties.Item("Cpp").Value;
				options.IndentBraces = (bool)PackageProperties.Item("CppIndentBraces").Value;
				options.IndentBlock = true;
				break;
			case "CSS":
				options.CompleteBraces = (bool)PackageProperties.Item("Css").Value;
				break;
			case "JScript":
				options.CompleteBraces = (bool)PackageProperties.Item("JScript").Value;
				break;
			case "JavaScript":
			case "TypeScript":
				options.CompleteBraces = (bool)PackageProperties.Item("JavaScript").Value;
				break;
			case "plaintext":
				options.CompleteBraces = (bool)PackageProperties.Item("PlainText").Value;
				break;
			default:
				string[] otherLangs = ((string)PackageProperties.Item("OtherLanguages").Value).Split(',');
				options.CompleteBraces = false;

				//search for the language in OtherLanguages.  If "All" is present, activate completion
				foreach (string activelang in otherLangs)
				{
					if (string.Compare(options.Language, activelang.Trim(), StringComparison.OrdinalIgnoreCase) == 0 || 
						string.Compare("All", activelang.Trim(), StringComparison.OrdinalIgnoreCase) == 0)
					{
						options.CompleteBraces = true;
						break;
					}
				}
				break;
			}
			
#if DEBUG
			TimeSpan span = DateTime.Now - start;
			Debug.Print("retrieve options: {0}ms", span.TotalMilliseconds);
#endif

			return options;
		}

		/// <summary>
		/// Attempts to parse a bool/int/uint to a bool.  If no conversion is possible, defVal is returned.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="defVal"></param>
		/// <returns></returns>
		public static bool ParseBool(object value, bool defVal = false)
		{
			if (value is bool)
			{
				if ((bool)value == false)
					return defVal;
				else
					return (bool)value;
			}
			else if (value is int)
				return (int)value != 0;
			else if (value is uint)
				return (uint)value != 0;
			else
				return defVal;
		}
	}
}
