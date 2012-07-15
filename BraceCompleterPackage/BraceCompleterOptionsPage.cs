using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;

namespace JoelSpadin.BraceCompleter
{
	/// <summary>
	/// Options page for Brace Completer
	/// </summary>
	[CLSCompliant(false), ComVisible(true)]
	[ClassInterface(ClassInterfaceType.AutoDual)]
	[Guid("AD4840CB-EECE-4228-B9DC-44E2B2B00FF7")]
	public class BraceCompleterOptionsPage : DialogPage
	{
		private bool _plaintext = false;
		private bool _csharp = true;
		private bool _cpp = true;
		private bool _css = true;
		private bool _jscript = true;
		private bool _javascript = true;
		private string _others = string.Empty;

		private bool _immediateCompletion = false;
		private bool _smartFormat = false;
		private bool _cppIndentBraces = false;

		[Category("Languages")]
		[DisplayName("Plain Text")]
		[Description("Complete braces in plain text files.")]
		public bool PlainText
		{
			get { return _plaintext; }
			set { _plaintext = value; }
		}

		[Category("Languages")]
		[DisplayName("C#")]
		[Description("Complete braces in C# files.")]
		public bool CSharp
		{
			get { return _csharp; }
			set { _csharp = value; }
		}

		[Category("Languages")]
		[DisplayName("C/C++")]
		[Description("Complete braces in C/C++ files.")]
		public bool Cpp
		{
			get { return _cpp; }
			set { _cpp = value; }
		}

		[Category("Languages")]
		[DisplayName("CSS")]
		[Description("Complete braces in CSS files.")]
		public bool Css
		{
			get { return _css; }
			set { _css = value; }
		}

		[Category("Languages")]
		[DisplayName("JScript")]
		[Description("Complete braces in JScript files.")]
		public bool JScript
		{
			get { return _jscript; }
			set { _jscript = value; }
		}

		[Category("Languages")]
		[DisplayName("JavaScript")]
		[Description("Complete braces in JavaScript files.")]
		public bool JavaScript
		{
			get { return _javascript; }
			set { _javascript = value; }
		}

		[Category("Languages")]
		[DisplayName("Others")]
		[Description("A comma separated list of types to enable brace completion in.  " +
			"Use \"All\" to enable brace completion on all other types.")]
		public string OtherLanguages
		{
			get { return _others; }
			set { _others = value; }
		}

		[Category("Indentation (C/C++)")]
		[DisplayName("Indent Braces")]
		[Description("Indent braces in C/C++ files.")]
		public bool CppIndentBraces
		{
			get { return _cppIndentBraces; }
			set { _cppIndentBraces = value; }
		}

		[Category("General")]
		[DisplayName("Complete brace immediately")]
		[Description("Add a closing brace immediately after typing an opening brace")]
		public bool ImmediateCompletion
		{
			get { return _immediateCompletion; }
			set { _immediateCompletion = value; }
		}

		[Category("General")]
		[DisplayName("Use Smart Formatting")]
		[Description("Use Visual Studio's automatic formatting")]
		public bool SmartFormat
		{
			get { return _smartFormat; }
			set { _smartFormat = value; }
		}

		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		private OptionsControlForm _page;

		public BraceCompleterOptionsPage()
		{
			_page = new OptionsControlForm();
			
		}

		protected override void OnApply(DialogPage.PageApplyEventArgs e)
		{
			// increment the options version
			Utils.OptionsVersion++;
			base.OnApply(e);
		}

		public override void SaveSettingsToStorage()
		{
			_page.SaveSettings();
			base.SaveSettingsToStorage();
		}

		public override void SaveSettingsToXml(IVsSettingsWriter writer)
		{
			_page.SaveSettings();
			base.SaveSettingsToXml(writer);
		}

		public override void LoadSettingsFromStorage()
		{
			base.LoadSettingsFromStorage();
			_page.Initialize(this);
		}

		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		protected override IWin32Window Window
		{
			get { return _page as IWin32Window;	}
		}

		public IEnumerable<string> GetOtherLanguages()
		{
			string[] langs = OtherLanguages.Split(',');
			foreach (string lang in langs)
			{
				yield return lang.Trim();
			}
		}
	}
}
