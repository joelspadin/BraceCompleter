using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Utilities;

namespace JoelSpadin.BraceCompleter
{
	/// <summary>
	/// Interaction logic for OptionsControl.xaml
	/// </summary>
	public partial class OptionsControl : UserControl
	{
		public class LangItem
		{
			public string Name { get; private set; }
			public string DisplayName { get; set; }
			public bool Active { get; set; }

			public LangItem(string name, bool active = false)
			{
				Name = name;
				DisplayName = name;
				Active = active;
			}

			public LangItem(string name, string displayName, bool active = false)
			{
				Name = name;
				DisplayName = displayName;
				Active = active;
			}
		}


		public BraceCompleterOptionsPage OptionsPage { get; set; }


		public bool PlainText
		{
			get { return (bool)GetValue(PlainTextProperty); }
			set { SetValue(PlainTextProperty, value); }
		}

		// Using a DependencyProperty as the backing store for PlainText.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PlainTextProperty =
			DependencyProperty.Register("PlainText", typeof(bool), typeof(OptionsControl), new UIPropertyMetadata(false));

		public bool CSharp
		{
			get { return (bool)GetValue(CSharpProperty); }
			set { SetValue(CSharpProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CSharp.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CSharpProperty =
			DependencyProperty.Register("CSharp", typeof(bool), typeof(OptionsControl), new UIPropertyMetadata(true));

		public bool Cpp
		{
			get { return (bool)GetValue(CppProperty); }
			set { SetValue(CppProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Cpp.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CppProperty =
			DependencyProperty.Register("Cpp", typeof(bool), typeof(OptionsControl), new UIPropertyMetadata(true));

		public bool CppIndentBraces
		{
			get { return (bool)GetValue(CppIndentBracesProperty); }
			set { SetValue(CppIndentBracesProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CppIndentBraces.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CppIndentBracesProperty =
			DependencyProperty.Register("CppIndentBraces", typeof(bool), typeof(OptionsControl), new UIPropertyMetadata(false));

		public bool Css
		{
			get { return (bool)GetValue(CssProperty); }
			set { SetValue(CssProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Css.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CssProperty =
			DependencyProperty.Register("Css", typeof(bool), typeof(OptionsControl), new UIPropertyMetadata(true));

		public bool JScript
		{
			get { return (bool)GetValue(JScriptProperty); }
			set { SetValue(JScriptProperty, value); }
		}

		// Using a DependencyProperty as the backing store for JScript.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty JScriptProperty =
			DependencyProperty.Register("JScript", typeof(bool), typeof(OptionsControl), new UIPropertyMetadata(true));



		public bool JavaScript
		{
			get { return (bool)GetValue(JavaScriptProperty); }
			set { SetValue(JavaScriptProperty, value); }
		}

		// Using a DependencyProperty as the backing store for JavaScript.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty JavaScriptProperty =
			DependencyProperty.Register("JavaScript", typeof(bool), typeof(OptionsControl), new UIPropertyMetadata(true));




		public List<LangItem> OtherLanguages
		{
			get { return (List<LangItem>)GetValue(OtherLanguagesProperty); }
			set { SetValue(OtherLanguagesProperty, value); }
		}

		// Using a DependencyProperty as the backing store for OtherLanguages.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty OtherLanguagesProperty =
			DependencyProperty.Register("OtherLanguages", typeof(List<LangItem>), typeof(OptionsControl), new UIPropertyMetadata(new List<LangItem>()));



		public bool ImmediateCompletion
		{
			get { return (bool)GetValue(ImmediateCompletionProperty); }
			set { SetValue(ImmediateCompletionProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ImmediateCompletion.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ImmediateCompletionProperty =
			DependencyProperty.Register("ImmediateCompletion", typeof(bool), typeof(OptionsControl), new UIPropertyMetadata(false));



		public bool SmartFormat
		{
			get { return (bool)GetValue(SmartFormatProperty); }
			set { SetValue(SmartFormatProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SmartFormat.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SmartFormatProperty =
			DependencyProperty.Register("SmartFormat", typeof(bool), typeof(OptionsControl), new UIPropertyMetadata(false));





		public bool ShowAll
		{
			get { return (bool)GetValue(ShowAllProperty); }
			set { SetValue(ShowAllProperty, value);	}
		}

		// Using a DependencyProperty as the backing store for ShowAll.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowAllProperty =
			DependencyProperty.Register("ShowAll", typeof(bool), typeof(OptionsControl), 
			new UIPropertyMetadata(false, new PropertyChangedCallback(OnShowAllChanged)));


		

		public OptionsControl()
		{
			InitializeComponent();
		}

		public void Initialize(BraceCompleterOptionsPage options)
		{
			OptionsPage = options;
			PlainText = OptionsPage.PlainText;
			CSharp = OptionsPage.CSharp;
			Cpp = OptionsPage.Cpp;
			Css = OptionsPage.Css;
			JScript = OptionsPage.JScript;
			JavaScript = OptionsPage.JavaScript;
			CppIndentBraces = OptionsPage.CppIndentBraces;

			ImmediateCompletion = OptionsPage.ImmediateCompletion;
			SmartFormat = OptionsPage.SmartFormat;

			FillOtherLanguages();
		}

		/// <summary>
		/// Applies the changes made to the DialogPage
		/// </summary>
		public void SaveSettings()
		{
			OptionsPage.PlainText = PlainText;
			OptionsPage.CSharp = CSharp;
			OptionsPage.Cpp = Cpp;
			OptionsPage.Css = Css;
			OptionsPage.JScript = JScript;
			OptionsPage.JavaScript = JavaScript;
			OptionsPage.CppIndentBraces = CppIndentBraces;

			OptionsPage.ImmediateCompletion = ImmediateCompletion;
			OptionsPage.SmartFormat = SmartFormat;

			// build the OptionsPage.OtherLanguages string
			List<string> activeLangs = new List<string>();
			// Languages that are active but not shown must be kept
			List<string> oldLangs = OptionsPage.GetOtherLanguages().Where(x => !String.IsNullOrWhiteSpace(x)).ToList();
			foreach (LangItem lang in OtherLanguages)
			{
				oldLangs.Remove(lang.Name);
				if (lang.Active == true)
					activeLangs.Add(lang.Name);
			}

			activeLangs.AddRange(oldLangs);
			OptionsPage.OtherLanguages = String.Join(",", activeLangs);
		}

		/// <summary>
		/// Builds the extra languages list
		/// </summary>
		private void FillOtherLanguages()
		{
			// Get available content types and currently active types
			IEnumerable<IContentType> langs = Utils.GetCodeTypes(ShowAll ? null : "code");
			IEnumerable<string> activeLangs = OptionsPage.GetOtherLanguages();

			// Filter out normally handled languages
			List<string> ignoredLangs = new List<string> {
				"plaintext", "CSharp", "C/C++", "CSS", "JScript", "JavaScript"
			};

			// Filter out types that are not normally code files
			if (!ShowAll)
				ignoredLangs.AddRange(new string[] { "Command", "Immediate", "Register", "Memory", "RDL Expression" });
			
			// Build the extra languages list
			foreach (IContentType type in langs)
			{
				if (ignoredLangs.Contains(type.TypeName))
					continue;

				bool active = activeLangs.Contains(type.TypeName);
				OtherLanguages.Add(new LangItem(type.TypeName, type.DisplayName, active));
			}
		}

		/// <summary>
		/// Saves the current settings and rebuilds the extra languages list
		/// </summary>
		private void RefillOtherLanguages()
		{
			SaveSettings();
			OtherLanguages.Clear();
			FillOtherLanguages();
			langList.ItemsSource = null;
			langList.ItemsSource = OtherLanguages;
		}

		private static void OnShowAllChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) 
		{
			(d as OptionsControl).RefillOtherLanguages();
		}

	}
}
