using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace JoelSpadin.BraceCompleter
{
	public partial class OptionsControlForm : UserControl
	{
		public OptionsControlForm()
		{
			InitializeComponent();
		}

		public void Initialize(BraceCompleterOptionsPage options)
		{
			OptionsControl child = elementHost.Child as OptionsControl;
			child.Initialize(options);
		}

		public void SaveSettings()
		{
			OptionsControl child = elementHost.Child as OptionsControl;
			child.SaveSettings();
		}
	}
}
