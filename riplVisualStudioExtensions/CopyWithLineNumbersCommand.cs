﻿/*
ISC License

Copyright(c) 2021, Raghavendra Chandrashekara

Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualBasic.Devices;
using System.Windows;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

namespace riplVisualStudioExtensions {
  internal sealed class CopyWithLineNumbersCommand {
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 0x0100;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = new Guid("a031575e-f457-4911-9320-0070c91bb138");

    /// <summary>
    /// VS Package that provides this command, not null.
    /// </summary>
    private readonly AsyncPackage package;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopyWithLineNumbersCommand"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private CopyWithLineNumbersCommand(AsyncPackage package, OleMenuCommandService commandService) {
      this.package = package ?? throw new ArgumentNullException(nameof(package));
      commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

      var menuCommandID = new CommandID(CommandSet, CommandId);
      var menuItem = new MenuCommand(this.Execute, menuCommandID);
      commandService.AddCommand(menuItem);
    }

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static CopyWithLineNumbersCommand Instance {
      get;
      private set;
    }

    /// <summary>
    /// Gets the service provider from the owner package.
    /// </summary>
    private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider {
      get {
        return this.package;
      }
    }

    /// <summary>
    /// Initializes the singleton instance of the command.
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    public static async Task InitializeAsync(AsyncPackage package) {
      // Switch to the main thread - the call to AddCommand in CopyWithLineNumbersCommand's constructor requires
      // the UI thread.
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

      OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
      Instance = new CopyWithLineNumbersCommand(package, commandService);
    }

    /// <summary>
    /// This function is the callback used to execute the command when the menu item is clicked.
    /// See the constructor to see how the menu item is associated with this function using
    /// OleMenuCommandService service and MenuCommand class.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event args.</param>
    private void Execute(object sender, EventArgs e) {
      var task = this.ServiceProvider.GetServiceAsync(typeof(SVsTextManager));
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
      task.Wait();
      var txtMgr = task.Result as IVsTextManager;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
      IVsTextView vTextView = null;
      int mustHaveFocus = 1;
      txtMgr.GetActiveView(mustHaveFocus, null, out vTextView);

      IVsUserData userData = vTextView as IVsUserData;
      if (userData == null) {
        Console.WriteLine("No text view is currently open");
        return;
      }
      IWpfTextViewHost viewHost;
      object holder;
      Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
      userData.GetData(ref guidViewHost, out holder);
      viewHost = (IWpfTextViewHost)holder;

      var textView = viewHost.TextView;
      if (textView.Selection == null) {
        return;
      }

      var isKeyword = new Func<string, bool>(w => {
        return new[] { "let", "for", "do", "if", "then", "elif", "else", "open",
        "module", "namespace", "<-", ":"}.Contains(w);
      });

      var start = textView.Selection.Start.Position.Position;
      var end = textView.Selection.End.Position.Position;
      var lineStart = textView.TextSnapshot.GetLineFromPosition(start).LineNumber;
      var lineEnd = textView.TextSnapshot.GetLineFromPosition(end).LineNumber;
      var sTxt = "";
      var sRtf = @"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033{\fonttbl{\f0\fnil\fcharset0 Courier New;}}
{\colortbl ;\red163\green21\blue21;\red34\green177\blue76;\red63\green72\blue204;}
{\*\generator riplVisualStudioExtensions 1.0}\viewkind4\uc1 
\pard\sa200\sl276\slmult1\f0\fs22\lang9 ";
      for (var i = lineStart; i <= lineEnd; i++) {
        var line = textView.TextSnapshot.GetLineFromLineNumber(i).GetText();
        sTxt += string.Format("{0,4:d}:    {1}\n", i + 1, line);
        var words = line.Split(' ', '\t');
        sRtf += string.Format("{0,4:d}:    ", i + 1);
        bool inComment = false;
        bool inString = false;
        for (var j = 0; j < words.Length; j++) {
          if (string.IsNullOrEmpty(words[j])) {
            sRtf += string.Format("{0}", " ");
          } else if (words[j].StartsWith("//")) {
            sRtf += string.Format("\\cf2 {0} ", words[j]);
            inComment = true;
          }
          else if (inComment) {
            sRtf += string.Format("{0} ", words[j]);
          }
          else if (words[j].StartsWith("\"")) {
            sRtf += string.Format("\\cf1 {0} ", words[j]);
            inString = true;
          }
          else if (inString) {
            sRtf += string.Format("\\cf1 {0} ", words[j]);
            if (words[j].EndsWith("\"")) {
              inString = false;
            }
          }
          else if (isKeyword(words[j])) {
            sRtf += string.Format("\\cf3 {0} ", words[j]);
          } else { 
            sRtf += string.Format("\\cf0 {0} ", words[j]);
          }
        }
        sRtf += "\\line\n";
      }
      sRtf += "\\par}\n";

      var dataObject = new DataObject();
      dataObject.SetData(DataFormats.Text, sTxt);
      dataObject.SetData(DataFormats.Rtf, sRtf);
      Clipboard.SetDataObject(dataObject, true);
    }
  }
}
