using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JoelSpadin.BraceCompleter
{
	internal class BraceOptions
	{
		public bool SmartFormat { get; set; }
		public bool IndentBraces { get; set; }
		public bool IndentBlock { get; set; }
		public bool CompleteBraces { get; set; }
		public bool ImmediateCompletion { get; set; }
		public string Language { get; set; }

		public BraceOptions()
		{
			SmartFormat = false;
			IndentBraces = false;
			IndentBlock = true;
			CompleteBraces = true;
			ImmediateCompletion = false;
			Language = "plaintext";
		}
	}
}
