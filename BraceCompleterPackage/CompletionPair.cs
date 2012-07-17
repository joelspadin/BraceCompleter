using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;

namespace JoelSpadin.BraceCompleter
{
	internal class CompletionPair
	{
		internal CompletionItem CompletionItem { get; private set; }
		internal ITrackingPoint OpeningPoint { get; set; }
		internal ITrackingPoint ClosingPoint { get; set; }
		
		internal char OpeningToken
		{
			get { return CompletionItem.OpeningToken; }
		}

		internal char ClosingToken
		{
			get { return CompletionItem.ClosingToken; }
		}

		public CompletionPair(CompletionItem completionItem)
		{
			CompletionItem = completionItem;
		}
	}
}
