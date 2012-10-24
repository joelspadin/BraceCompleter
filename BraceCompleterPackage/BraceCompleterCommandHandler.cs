using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Collections.Generic;
using System.Diagnostics;
using EnvDTE;
using System.Text.RegularExpressions;

namespace JoelSpadin.BraceCompleter
{
	/// <summary>
	/// Implements brace completion
	/// </summary>
	internal class BraceCompleterCommandHandler : IOleCommandTarget
	{
		#region Constants
		public const char OpenBrace = '{';
		public const char CloseBrace = '}';
		#endregion

		#region Private Fields
		private IOleCommandTarget _nextCommandHandler;
		private IWpfTextView _textView;
		private IEditorOperations _operations;
		private ITextUndoHistory _undoHistory;

		/// <summary>
		/// True if the last typed character is an opening brace
		/// </summary>
		private bool _lastCharIsBrace = false;
		/// <summary>
		/// True if the last typed character is an opening brace and the next character is its closing brace
		/// </summary>
		private bool _nextCharIsClosingBrace = false;
		/// <summary>
		/// The position of the last typed opening brace
		/// </summary>
		private ITrackingPoint _openBracePosition = null;
		/// <summary>
		/// The position of the closing brace added immediately after the last typed opening brace
		/// </summary>
		private ITrackingPoint _closeBracePosition = null;

		private BraceOptions _options = new BraceOptions();
		/// <summary>
		/// Utils keeps a counter of the number of times the package options have been applied.
		/// If the textview recieves focus and the options have been updated, update the local options
		/// </summary>
		private uint _optionsVersion = 0;
		/// <summary>
		/// If true, Visual Studio automatically unindents the closing brace, but only when the next line is a closing brace.
		/// </summary>
		private bool _languageUnindentsBraceBeforeBrace = true;
		/// <summary>
		/// If true, Visual Studio automatically unindents the closing brace always
		/// </summary>
		private bool _languageAlwaysUnindentsBrace = false;
		/// <summary>
		/// If true, Visual Studio does not automatically indent the block
		/// </summary>
		private bool _languageNeedsBlockIndent = false;
		/// <summary>
		/// If true, Visual Studio indents the block and closing brace when using smart format
		/// </summary>
		private bool _languageUsesSmartFormat = false;
		#endregion
		

		#region Constructors & Destructors
		public BraceCompleterCommandHandler(IVsTextView textViewAdapter,
			IWpfTextView textView, IEditorOperations operations, ITextUndoHistory undoHistory)
		{
			_textView = textView;
			_operations = operations;
			_undoHistory = undoHistory;

			_textView.GotAggregateFocus += TextView_GotFocus;

			// add the command to the command chain
			textViewAdapter.AddCommandFilter(this, out _nextCommandHandler);

			// load language and indentation settings
			LoadSettings();
		}

		#endregion


		/// <summary>
		/// Loads settings from Visual Studio
		/// </summary>
		private void LoadSettings()
		{
			float version = 10;
			float.TryParse(Utils.DTE.Version, out version);

			// get the indentation options from Visual Studio
			_options = Utils.GetOptions(_textView);

			// set options to account for differences in Visual Studio's behavior between languages
			_languageUnindentsBraceBeforeBrace = new List<string> { "CSharp" }.Contains(_options.Language);
			_languageUsesSmartFormat = new List<string> { "CSharp" }.Contains(_options.Language);

			var doesNotUnindentBrace = new List<string>	{ "CSharp", "C/C++", "JavaScript", "TypeScript", "CSS" };
			_languageAlwaysUnindentsBrace = !doesNotUnindentBrace.Contains(_options.Language);

			var doesNotNeedBlockIndent = new List<string> { "CSharp", "C/C++", "CSS", "TypeScript" };
			_languageNeedsBlockIndent = !doesNotNeedBlockIndent.Contains(_options.Language);

			Debug.Print("Brace completion {0:enabled;disabled} in {1} TextView.", _options.CompleteBraces, _options.Language);
		}

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			return _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}


		public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvalIn, IntPtr pvalOut)
		{
			// If brace completion is disabled, pass commands through
			if (_options.CompleteBraces)
			{
				if (ProcessInput(nCmdID, pvalIn))
					return VSConstants.S_OK;
			}
			
			//pass along the command so the char is added to the buffer
			return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvalIn, pvalOut);
		}

		private bool ProcessInput(uint nCmdID, IntPtr pvalIn)
		{
			// make sure the input is a char before getting it
			if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
			{
				// Make sure the number fits into a char before trying to use it
				var typedCharId = Marshal.GetObjectForNativeVariant(pvalIn);
				if (typedCharId is ushort && (ushort)typedCharId <= char.MaxValue)
				{
					char typedChar = (char)(ushort)typedCharId;
					return ProcessCharInput(typedChar);
				}
			}
			else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN && _lastCharIsBrace)
			{
				return ProcessReturnKey();
			}
			else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE && pvalIn == IntPtr.Zero)
			{
				// For uknown reason other key combinations (e.g. Ctrl+S) also send
				// BACKSPACE command, therefore trick with pvalIn is needed (found empirically)
				return ProcessBackspaceKey();
			}
			else if (nCmdID == (uint)VSConstants.VSStd97CmdID.Delete ||
				nCmdID == (uint)VSConstants.VSStd97CmdID.Undo)
			{
				return ProcessDeleteOrUndoKey();
			}
			return false;
		}

		private bool ProcessCharInput(char typedChar)
		{
			// if the character is an open brace, get ready for brace completion
			_lastCharIsBrace = typedChar == OpenBrace;

			if (_lastCharIsBrace)
			{
				// remember the position of the opening brace, so if the caret position is changed, don't do a completion
				SaveCaretPosition();
			}


			if (_options.ImmediateCompletion)
			{
				// do special stuff when immediate completion is active
				if (_lastCharIsBrace)
				{
					// close brace after an opening brace
					CloseCurlyBraceImmediate();
					return true;
				}
				else
				{
					// if closing brace was typed and next character is the automatically added closing brace,
					// instead of writing another closing brace, move the caret to the other side of the brace
					if (typedChar == CloseBrace && IsNextCharImmediateBrace())
					{
						_operations.MoveToNextCharacter(false);
						_nextCharIsClosingBrace = false;
						_closeBracePosition = null;
						return true;
					}

					_nextCharIsClosingBrace = false;
				}
			}
			return false;
		}

		private bool ProcessReturnKey()
		{
			// If the caret position hasn't been moved and the previous character is an opening brace, 
			// complete the brace, otherwise, cancel.
			if (IsCaretSamePosition() && IsPreviousCharOpeningBrace())
			{
				// add the closing brace
				CloseCurlyBrace();
				_lastCharIsBrace = false;
				_closeBracePosition = null;
				return true;
			}
			else
			{
				_lastCharIsBrace = false;
				return false;
			}		
		}

		private bool ProcessBackspaceKey()
		{
			// if backspace pressed, cancel brace completion
			// if immediate completion is on, delete the automatically added brace
			if (IsCaretSamePosition())
				RemoveCurlyBraceImmediate();
			_lastCharIsBrace = false;
			return false;
		}

		private bool ProcessDeleteOrUndoKey()
		{
			// if delete pressed or undo used, cancel brace completion
			_lastCharIsBrace = false;
			_nextCharIsClosingBrace = false;
			return false;
		}

		/// <summary>
		/// Save the position of the caret for IsCaretSamePosition
		/// </summary>
		private void SaveCaretPosition() 
		{
			// remember the position of the opening brace, so if the caret position is changed, don't do a completion
			_openBracePosition = _textView.TextSnapshot.CreateTrackingPoint(
				_textView.Caret.Position.BufferPosition.Position, PointTrackingMode.Positive);
		}

		/// <summary>
		/// Save the position of the caret after adding an immediate closing brace
		/// </summary>
		private void SaveImmediateBracePosition()
		{
			// remember the position of the closing brace, so if the user types the closing brace him/herself, it won't be doubled
			_closeBracePosition = _textView.TextSnapshot.CreateTrackingPoint(
				_textView.Caret.Position.BufferPosition.Position, PointTrackingMode.Positive);
		}

		/// <summary>
		/// Returns true if the caret is in the same position as it was when SaveCaretPosition was called
		/// </summary>
		/// <returns></returns>
		private bool IsCaretSamePosition() 
		{
			if (_openBracePosition == null)
				return false;

			// check that the caret position is still directly after the brace
			ITrackingPoint currentPosition = _textView.TextSnapshot.CreateTrackingPoint(
				_textView.Caret.Position.BufferPosition.Position, PointTrackingMode.Positive);

			int diff = currentPosition.GetPosition(_textView.TextSnapshot) - _openBracePosition.GetPosition(_textView.TextSnapshot);
			return diff == 0;
		}

		/// <summary>
		/// Returns true if the next character after the caret is the automatically added closing brace
		/// </summary>
		/// <returns></returns>
		private bool IsNextCharImmediateBrace()
		{
			// If immediate brace was just added, next char is the brace
			if (_nextCharIsClosingBrace && IsCaretSamePosition())
				return true;

			// If no immediate brace added, next char is not the brace
			if (_closeBracePosition == null)
				return false;

			// Make sure next character is a closing brace
			if (!IsNextCharClosingBrace())
				return false;

			// Check that the position directly after the caret is the automatically added closing brace
			ITrackingPoint currentPosition = _textView.TextSnapshot.CreateTrackingPoint(
				_textView.Caret.Position.BufferPosition.Position + 1, PointTrackingMode.Positive);

			int diff = currentPosition.GetPosition(_textView.TextSnapshot) - _closeBracePosition.GetPosition(_textView.TextSnapshot);
			return diff == 0;
		}


		/// <summary>
		/// Adds a closing brace immediately after an opening brace.
		/// </summary>
		private void CloseCurlyBraceImmediate() 
		{
			// Write an opening brace before the closing brace so pressing undo will undo the closing brace
			_operations.InsertText(OpenBrace.ToString());

			// Write the closing brace
			using (var undo = _undoHistory.CreateTransaction("Insert Closing Brace"))
			{
				_operations.AddBeforeTextBufferChangePrimitive();

				_operations.InsertText(CloseBrace.ToString());
				SaveImmediateBracePosition();
				// Move the caret back to between the braces
				_operations.MoveToPreviousCharacter(false);

				_operations.AddAfterTextBufferChangePrimitive();
				undo.Complete();
			}

			SaveCaretPosition();
			_nextCharIsClosingBrace = true;
		}

		/// <summary>
		/// Undoes the brace added by CloseCurlyBraceImmediate
		/// </summary>
		private void RemoveCurlyBraceImmediate()
		{
			if (_nextCharIsClosingBrace)
			{
				_undoHistory.Undo(1);
			}

			_nextCharIsClosingBrace = false;
		}

		
		private void CloseCurlyBrace()
		{
			// remove any curly brace that was added immediately after typing {
			RemoveCurlyBraceImmediate();

			if (_options.SmartFormat && _languageUsesSmartFormat)
				CloseCurlyBraceSmartFormat();
			else
				CloseCurlyBraceGeneric();
		}

		/// <summary>
		/// Inserts a closing brace, then runs VS' Smart Format on the code block 
		/// and repositions the cursor
		/// </summary>
		private void CloseCurlyBraceSmartFormat()
		{
			// Get whether the next line is a close brace
			bool nextLineIsCloseBrace = IsNextLineClosingBrace();
			// hack to fix indentation of brace before brace in C# when IndentBraces is on
			bool csharpUnindent = !nextLineIsCloseBrace;
			// determine whether or not to unindent the new line
			bool newlineUnindent = IsOpenBraceOnNewLine();

			// Insert the closing brace on the next line, then reposition the cursor between the braces
			using (var undo = _undoHistory.CreateTransaction("Insert Closing Brace"))
			{
				_operations.AddBeforeTextBufferChangePrimitive();

				_operations.InsertNewLine();
				_operations.InsertText(CloseBrace.ToString());

				if (csharpUnindent)
				{
					_operations.MoveToStartOfLineAfterWhiteSpace(true);
					_operations.Unindent();
				}

				_operations.MoveLineUp(false);
				_operations.MoveToEndOfLine(false);

				_operations.AddAfterTextBufferChangePrimitive();
				undo.Complete();
			}

			// Run Smart Format with the cursor between the braces
			TextSelection selection = (Utils.DTE.ActiveDocument.Selection as TextSelection);
			selection.SmartFormat();

			// Insert a new line and fix the cursor's indentation
			using (var undo = _undoHistory.CreateTransaction("Insert New Line"))
			{
				_operations.AddBeforeTextBufferChangePrimitive();

				_operations.InsertNewLine();
				if (!_options.IndentBraces)
					_operations.Indent();

				_operations.AddAfterTextBufferChangePrimitive();
				undo.Complete();
			}

		}

		/// <summary>
		/// Drops the caret to the next line and adds a closing brace on the line after that.
		/// </summary>
		private void CloseCurlyBraceGeneric()
		{
			// Get whether the next line is a close brace
			bool nextLineIsCloseBrace = IsNextLineClosingBrace();
			// hack to fix indentation of brace before brace in C# when IndentBraces is on
			bool csharpUnindent = (_languageUnindentsBraceBeforeBrace && nextLineIsCloseBrace && _options.IndentBraces);
			// hack to fix indentation of main block in JavaScript inside of anonymous functions
			bool jsUnindent = NeedsJavaScriptHack();
			// determine whether or not to fix the indentation of the closing brace by unindenting it one level.
			// If the language automatically unindents it, no correction is needed.
			bool indentCorrectionNeeded = (!_languageUnindentsBraceBeforeBrace || (_languageUnindentsBraceBeforeBrace && !nextLineIsCloseBrace)) && !_languageAlwaysUnindentsBrace;
			// determine whether or not to indent for the first undo point.
			bool indentFirstUndoPoint = (!indentCorrectionNeeded || _languageNeedsBlockIndent) && (_options.IndentBlock || _options.IndentBraces) && !csharpUnindent;

			// Close the brace in two steps so that the first Undo removes the closing brace and the second removes the newline
			using (var undo = _undoHistory.CreateTransaction("Insert New Line"))
			{
				_operations.AddBeforeTextBufferChangePrimitive();


				// add a new line and maybe indent it so the position will be correct after one undo
				_operations.InsertNewLine();


				if (indentFirstUndoPoint)
					_operations.Indent();

				_operations.AddAfterTextBufferChangePrimitive();
				undo.Complete();
			}


			using (var undo = _undoHistory.CreateTransaction("Insert Closing Brace"))
			{
				_operations.AddBeforeTextBufferChangePrimitive();


				// undo the previous indent so the brace position will be correct
				if (indentFirstUndoPoint)
					_operations.Unindent();


				// add closing brace
				_operations.InsertText(CloseBrace.ToString());


				// unindent it if necessary
				if (indentCorrectionNeeded)
					_operations.DecreaseLineIndent();


				// indent it if necessary
				if (_options.IndentBraces && !csharpUnindent)
					_operations.IncreaseLineIndent();


				// move back to the first line, add a new line between the braces
				_operations.MoveLineUp(false);
				_operations.MoveToEndOfLine(false);
				_operations.InsertNewLine();


				// indent/unindent the final cursor position if necessary
				if (_languageNeedsBlockIndent && !jsUnindent)
					_operations.Indent();


				if (csharpUnindent)
				{
					_operations.Unindent();
					_operations.Indent();
				}


				_operations.AddAfterTextBufferChangePrimitive();
				undo.Complete();
			}
		}

		/// <summary>
		/// Gets the text currently selected
		/// </summary>
		/// <returns></returns>
		private string GetSelection()
		{
			int start = _operations.TextView.Selection.AnchorPoint.Position.Position;
			int end = _operations.TextView.Selection.ActivePoint.Position.Position;
			if (end < start)
			{
				int temp = end;
				end = start;
				start = temp;
			}
			return _operations.TextView.TextSnapshot.GetText(start, end - start);
		}

		/// <summary>
		/// Returns true if the character to the left of the caret is an opening brace
		/// </summary>
		/// <returns></returns>
		private bool IsPreviousCharOpeningBrace()
		{
			string prevChar = null;
			// Set an undo point so no changes will be made
			using (var undo = _undoHistory.CreateTransaction("(temp) read previous char"))
			{
				_operations.AddBeforeTextBufferChangePrimitive();

				// Select the previous character and retrieve it
				_operations.MoveToPreviousCharacter(true);
				prevChar = GetSelection();

				_operations.AddAfterTextBufferChangePrimitive();
				undo.Complete();
			}
			// undo changes to the selection point
			_undoHistory.Undo(1);

			return (prevChar == OpenBrace.ToString());
		}

		/// <summary>
		/// Returns true if the character to the right of the caret is a closing brace
		/// </summary>
		/// <returns></returns>
		private bool IsNextCharClosingBrace()
		{
			string nextChar = null;
			// Set an undo point so no changes will be made
			using (var undo = _undoHistory.CreateTransaction("(temp) read next char"))
			{
				_operations.AddBeforeTextBufferChangePrimitive();

				// Select the next character and retrieve it
				_operations.MoveToNextCharacter(true);
				nextChar = GetSelection();

				_operations.AddAfterTextBufferChangePrimitive();
				undo.Complete();
			}
			// undo changes to the selection point
			_undoHistory.Undo(1);

			return (nextChar == CloseBrace.ToString());
		}

		/// <summary>
		/// Returns true if the text on the next line is a closing brace
		/// </summary>
		/// <returns></returns>
		private bool IsNextLineClosingBrace()
		{
			string nextLine = null;
			// Set an undo point so no changes will be made
			using (var undo = _undoHistory.CreateTransaction("(temp) read next line"))
			{
				_operations.AddBeforeTextBufferChangePrimitive();

				// Select the next line and retrieve its text
				_operations.MoveToStartOfNextLineAfterWhiteSpace(false);
				_operations.MoveToNextCharacter(true);
				nextLine = GetSelection();

				_operations.AddAfterTextBufferChangePrimitive();
				undo.Complete();
			}
			// undo changes to the selection point
			_undoHistory.Undo(1);

			return (nextLine == CloseBrace.ToString());
		}

		private bool IsOpenBraceOnNewLine()
		{
			string startLine = null;

			using (var undo = _undoHistory.CreateTransaction("(temp) read current line"))
			{
				_operations.AddBeforeTextBufferChangePrimitive();

				_operations.MoveToStartOfLineAfterWhiteSpace(false);
				_operations.MoveToNextCharacter(true);
				startLine = GetSelection();

				_operations.AddAfterTextBufferChangePrimitive();
				undo.Complete();
			}

			_undoHistory.Undo(1);

			return startLine == OpenBrace.ToString(); 
		}

		/// <summary>
		/// Determines whether the code to the left of the cursor is an anonymous function which is
		/// not being assigned to any variable or part of a try block.
		/// </summary>
		/// <returns></returns>
		private bool NeedsJavaScriptHack()
		{
			if (_options.Language != "JavaScript" && _options.Language != "TypeScript")
				return false;

			string prevCode = null;
			// Set an undo point so no changes will be made
			using (var undo = _undoHistory.CreateTransaction("(temp) read previous code"))
			{
				_operations.AddBeforeTextBufferChangePrimitive();

				// Select the next character and retrieve it
				_operations.MoveToStartOfLineAfterWhiteSpace(true);
				prevCode = GetSelection();

				// If the entire line is only the opening brace, include the previous line too
				if (prevCode.TrimStart() == "{") 
				{
					_operations.MoveToStartOfPreviousLineAfterWhiteSpace(true);
					prevCode = GetSelection();
				}

				_operations.AddAfterTextBufferChangePrimitive();
				undo.Complete();
			}
			// undo changes to the selection point
			_undoHistory.Undo(1);

			// Strip whitespace at the end and newlines in the middle
			prevCode = prevCode.TrimEnd().Replace("\n", "").Replace("\r", "");
			// Check whether the code before the cursor is an anonymous function
			if (Regex.IsMatch(prevCode, @"\bfunction\s*\(\s*[^)]*\)\s*{$"))
			{
				// Check that the function isn't being assigned to a value
				var functionIndex = prevCode.LastIndexOf("function");
				if (!prevCode.Substring(0, functionIndex).TrimEnd().EndsWith("="))
					return true;
			}

			// Check whether the code before the cursor is a try block
			if (Regex.IsMatch(prevCode, @"\btry\s*{$"))
				return true;

			// Check whether brace pair is inside parenthesis or after comma/colon
			if (Regex.IsMatch(prevCode, @"[,:(]\s*{$"))
				return true;

			return false;
		}

		/// <summary>
		/// Checks whether the options have been updated when the textview receives focus
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextView_GotFocus(object sender, EventArgs e)
		{
			// Update local options if VS options are changed
			if (Utils.OptionsVersion > _optionsVersion)
			{
				LoadSettings();
				_optionsVersion = Utils.OptionsVersion;
			}
		}
	}
}
