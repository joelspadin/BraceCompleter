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
        internal bool IsImmediateCompletion { get { return immediateCompletion(); } }

        private Func<bool> immediateCompletion;

        public CompletionItem(char openingToken, char closingToken, Func<bool> immediateCompletion)
        {
            OpeningToken = openingToken;
            ClosingToken = closingToken;
            this.immediateCompletion = immediateCompletion;
        }
    }

    internal class ImmediateCompletionItem : CompletionItem
    {
        public ImmediateCompletionItem(char openingToken, char closingToken)
            : base(openingToken, closingToken, () => true)
        {
            // empty
        }
    }

    internal class SymmetricalCompletionItem : ImmediateCompletionItem
    {
        public SymmetricalCompletionItem(char token) : base(token, token)
        {
            // empty
        }
    }
}
