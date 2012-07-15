using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace JoelSpadin.BraceCompleter
{
	[Export(typeof(IVsTextViewCreationListener))]
	[Name("brace completion controller")]
	[ContentType("text")]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
	internal class BraceCompleterHandlerProvider : IVsTextViewCreationListener
	{
		[Import]
		internal IVsEditorAdaptersFactoryService AdapterService = null;

		[Import]
		internal IEditorOperationsFactoryService OperationsService = null;

		[Import]
		internal ITextUndoHistoryRegistry UndoHistoryRegistry = null;


		public void VsTextViewCreated(IVsTextView textViewAdapter)
		{
			IWpfTextView textView = AdapterService.GetWpfTextView(textViewAdapter);
			if (textView == null)
			{
				Debug.Fail("Unexpected: couldn't get the text view");
				return;
			}

			IEditorOperations operations = OperationsService.GetEditorOperations(textView);
			if (operations == null)
			{
				Debug.Fail("Unexpected: couldn't get the editor operations object");
				return;
			}

			ITextUndoHistory undoHistory;
			if (!UndoHistoryRegistry.TryGetHistory(textView.TextBuffer, out undoHistory))
			{
				Debug.Fail("Unexpected: couldn't get an undo history for the text buffer");
				return;
			}

			Func<BraceCompleterCommandHandler> createCommandHandler = delegate()
			{
				return new BraceCompleterCommandHandler(textViewAdapter, textView, operations, undoHistory);
			};

			textView.Properties.GetOrCreateSingletonProperty(createCommandHandler);
		}

	}
}
