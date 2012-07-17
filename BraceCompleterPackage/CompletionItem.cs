using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JoelSpadin.BraceCompleter
{
    internal class CompletionItem
    {
        internal char OpeningToken { get; private set; }
        internal char ClosingToken { get; private set; }
		internal bool FormatOnEnter { get; private set; }
        internal bool IsImmediateCompletion { get { return immediateCompletion(); } }

        private Func<bool> immediateCompletion;

        public CompletionItem(char openingToken, char closingToken, bool formatOnEnter, Func<bool> immediateCompletion)
        {
            OpeningToken = openingToken;
            ClosingToken = closingToken;
			FormatOnEnter = formatOnEnter;
            this.immediateCompletion = immediateCompletion;
        }
    }

    internal class ImmediateCompletionItem : CompletionItem
    {
        public ImmediateCompletionItem(char openingToken, char closingToken, bool formatOnEnter)
            : base(openingToken, closingToken, formatOnEnter, () => true)
        {
            // empty
        }
    }

    internal class SymmetricalCompletionItem : ImmediateCompletionItem
    {
        public SymmetricalCompletionItem(char token, bool formatOnEnter) : base(token, token, formatOnEnter)
        {
            // empty
        }
    }
}
