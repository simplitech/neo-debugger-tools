using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using ScintillaNET;
using Neo.Emulation.Utils;
using Neo.Emulation;
using Neo.Debugger.Utils;
using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;
using Neo.Debugger.Core.Data;
using Neo.VM;
using Neo.Debugger.Core.Generator;

namespace Neo.Debugger.Forms
{
    public partial class MainForm : Form
    {
        //Command line param
        private string _sourceAvmPath;
        private string _argumentsAvmFile;
        private DebuggerSettings _settings;
        private DebugManager _debugger;
        private Scintilla TextArea;

        private string _sourceFileName;
        private SourceLanguage _sourceLanguage;

        private Dictionary<SourceLanguage, List<string>> templates = new Dictionary<SourceLanguage, List<string>>();

        private MouseHoverManager hoverManager;

        private string TitleCaption;

        public MainForm(string argumentsAvmFile)
        {
            InitializeComponent();

            TitleCaption = this.Text;
            this.Text = $"{TitleCaption} {DebuggerUtils.DebuggerVersion}";

            stackPanel.Columns.Add("Index", "Index");
            stackPanel.Columns.Add("Eval", "Eval");
            stackPanel.Columns.Add("Alt", "Alt");

            if (!string.IsNullOrEmpty(argumentsAvmFile))
            {
                argumentsAvmFile = argumentsAvmFile.Replace("\\", "/");
                if (!argumentsAvmFile.Contains("/"))
                {
                    argumentsAvmFile = Directory.GetCurrentDirectory().Replace("\\", "/") + "/" + argumentsAvmFile;
                }
            }

            _argumentsAvmFile = argumentsAvmFile;
            _sourceAvmPath = argumentsAvmFile;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //Use settings from the My Documents folder
            _settings = new DebuggerSettings(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            //Init the UI controls
            InitUI();

            //Setup emulator log
            Emulation.API.Runtime.OnLogMessage = SendLogToPanel;

            if (string.IsNullOrEmpty(_sourceAvmPath) && !String.IsNullOrEmpty(_settings.lastOpenedFile))
            {
                _sourceAvmPath = _settings.lastOpenedFile;
            }

            //Init the debugger
            InitDebugger();

            // load all templates in the Contracts folder and sort them by languuage
            LoadTemplates();

            if (string.IsNullOrEmpty(_sourceAvmPath))
            {
                //Let's create a new file since we have nothing loaded from the command line and we haven't opened any files before
                LoadContractTemplate("HelloWorld.cs");
            }
        }

        private void LoadTemplates()
        {
            try
            {
                var exePath = AppDomain.CurrentDomain.BaseDirectory;
                string[] filePaths = Directory.GetFiles(exePath + "Contracts", "*.*", SearchOption.TopDirectoryOnly);
                foreach (var fileName in filePaths)
                {
                    var language = LanguageSupport.DetectLanguage(fileName);
                    if (language == SourceLanguage.Other)
                    {
                        continue;
                    }

                    List<string> templateList;
                    if (templates.ContainsKey(language))
                    {
                        templateList = templates[language];
                    }
                    else
                    {
                        templateList = new List<string>();
                        templates[language] = templateList;
                    }

                    templateList.Add(fileName);
                }

                foreach (var language in templates.Keys)
                {
                    {
                        var item = newToolStripMenuItem.DropDownItems.Add(LanguageSupport.GetLanguageName(language));
                        item.Click += newToolStripMenuItem_Click;
                    }

                    {
                        var item = (ToolStripMenuItem)newFromTemplateToolStripMenuItem.DropDownItems.Add(LanguageSupport.GetLanguageName(language));

                        var list = templates[language];
                        foreach (var entry in list)
                        {
                            var templateName = Path.GetFileNameWithoutExtension(entry);
                            var subItem = item.DropDownItems.Add(templateName);
                            subItem.Click += newFromTemplateToolStripMenuItem_Click;
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("Failed loading templates");
            }
        }

        #region Initializers
        private void InitUI()
        {
            // CREATE CONTROL
            TextArea = new ScintillaNET.Scintilla();
            TextArea.MouseClick += TextArea_MouseClick;
            TextArea.CaretForeColor = Color.White;
            TextPanel.Controls.Add(TextArea);

            // BASIC CONFIG
            TextArea.Dock = System.Windows.Forms.DockStyle.Fill;
            TextArea.TextChanged += (this.TextArea_OnTextChanged);
            TextArea.Enabled = true;

            // INITIAL VIEW CONFIG
            TextArea.WrapMode = WrapMode.None;
            TextArea.IndentationGuides = IndentView.LookBoth;

            // STYLING
            InitColors();
            InitSyntaxColoring();

            // NUMBER MARGIN
            InitNumberMargin();

            // BOOKMARK MARGIN
            InitBookmarkMargin();

            // CODE FOLDING MARGIN
            InitCodeFolding();

            // DRAG DROP
            InitDragDropFile();

            // INIT HOTKEYS
            InitHotkeys();

            hoverManager = new MouseHoverManager(TextArea, 2, ValidateHover);
        }

        private void TextArea_MouseClick(object sender, MouseEventArgs e)
        {
            TextArea.GotoPosition(TextArea.CharPositionFromPoint(e.X, e.Y) + 1);
        }

        private void InitDebugger()
        {
            _debugger = new DebugManager(_settings);
            _debugger.SendToLog += _debugger_SendToLog;

            //Load if we had a file on the command line or a previously opened
            bool success = false;
            try
            {
                success = LoadContract(_sourceAvmPath);
            }
            catch (Exception)
            {
                success = false;
            }

            if (!success)
            {
                if (!String.IsNullOrEmpty(_argumentsAvmFile)) // display when launched with command line arg (e.g. from Visual Studio/Start)
                {
                    string cwd = Environment.CurrentDirectory;
                    MessageBox.Show("Can't open '" + _sourceAvmPath + "'. Current directory is '" + cwd + "'", "Open AVM File");
                }
            }
        }

        private DateTime _contractModTime;

        private bool LoadContract(string avmFilePath)
        {
            if (!_debugger.LoadContract(avmFilePath))
            {
                return false;
            }

            _contractModTime = File.GetLastWriteTime(_debugger.AvmFilePath);

            ReloadProjectTree();
            
            ReloadTextArea(_debugger.CurrentFilePath);

            return true;
        }

        #endregion

        #region Debugger Actions

        private bool CompileContract()
        {
            ClearLog();

            if (!_settings.compilerPaths.ContainsKey(_sourceLanguage))
            {
                string compilerPath = "";
                string compilerFile = null;

                switch (_sourceLanguage)
                {
                    case SourceLanguage.CSharp:
                        {
                            compilerFile = "neon.exe";
                            break;
                        }

                    case SourceLanguage.Python:
                        {
                            string pythonPath = DebuggerUtils.FindExecutablePath("python.exe");
                            if (string.IsNullOrEmpty(pythonPath))
                            {
                                SendLogToPanel("To compile smart contracts written in Python you need to have Python.exe installed");
                                return false;
                            }

                            compilerFile = "boa/compiler.py";

                            break;
                        }

                    default: {
                            SendLogToPanel($"Compilation of {_sourceAvmPath} is currently not supported.");
                            return false;
                        }
                }

                var paths = new List<string>();
                var exePath = AppDomain.CurrentDomain.BaseDirectory;
                var potentialPath = exePath + "Compilers/" + _sourceLanguage.ToString().ToLower();
                paths.Add(potentialPath);
                compilerPath = DebuggerUtils.FindExecutablePath(compilerFile, paths);

                browserFolder:

                if (string.IsNullOrEmpty(compilerPath))
                {

                    using (var fbd = new FolderBrowserDialog())
                    {
                        fbd.Description = "Select location of " + compilerFile;
                        DialogResult result = fbd.ShowDialog();

                        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                        {
                            compilerPath = fbd.SelectedPath;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                var fullPath = compilerPath + "/" + compilerFile;
                if (!File.Exists(fullPath))
                {
                    SendLogToPanel($"{compilerFile} not found at {compilerPath}");
                    compilerPath = null;
                    goto browserFolder;
                }

                SendLogToPanel($"Found {LanguageSupport.GetLanguageName(_sourceLanguage)} Neo compiler: " + compilerPath);

                if (string.IsNullOrEmpty(compilerPath))
                {
                    return false;
                }

                _settings.compilerPaths[_sourceLanguage] = compilerPath;
                _settings.Save();
            }

            return _debugger.CompileContract(TextArea.Text, _sourceLanguage);
        }

        private void RunDebugger()
        {
            //We need to make sure there is a file loaded
            if (!_debugger.AvmFileLoaded)
            {
                MessageBox.Show("Please load an .avm file first!");
                return;
            }

            if (!_debugger.IsCompiled)
            {
                ClearLog();

                if (!CompileContract())
                {
                    return;
                }

                LoadContract(_debugger.AvmFilePath);
            }

            if (_debugger.ResetFlag && !ResetDebugger())
            {
                //MessageBox.Show("Error initializing debugger");
                return;
            }

            if(RunForm.SelectedTestSequence != null)
            {
                _debugger.RunSequence(RunForm.SelectedTestSequence);
            }
            else
            {
                _debugger.Run();
            }
            
            UpdateDebuggerStateUI();
        }

        private void StepDebugger()
        {
            //We need to make sure there is a file loaded
            if (!_debugger.AvmFileLoaded)
            {
                MessageBox.Show("Please load an .avm file first!");
                return;
            }

            if (!_debugger.IsCompiled)
            {
                ClearLog();

                if (!CompileContract())
                    return;

                LoadContract(_debugger.AvmFilePath);
            }

            if (_debugger.ResetFlag && !ResetDebugger())
                return;

            var previousLine = _debugger.CurrentLine;
            var previousFilePath = _debugger.CurrentFilePath;
            do
            {
                _debugger.Step();

                UpdateDebuggerStateUI();

                if (_debugger.ResetFlag)
                    return;

            } while (previousLine == _debugger.CurrentLine && previousFilePath == _debugger.CurrentFilePath);

            //Update UI
            UpdateStackPanel();
            UpdateGasCost(_debugger.Emulator.usedGas);
            UpdateDebuggerStateUI();
        }

        private bool ResetDebugger()
        {
            //We need to make sure there is a file loaded
            if (!_debugger.AvmFileLoaded)
            {
                MessageBox.Show("Please load an .avm file first!");
                return false;
            }

            if (!_debugger.DeployContract())
            {
                MessageBox.Show("Failed to deploy the contract in the virtual blockchain!");
                return false;
            }

            //Get the parameters to execute the debugger
            if (!GetDebugParameters())
                return false;

            //Reset the UI
            RemoveCurrentHighlight();
            logView.Clear();
            stackPanel.Rows.Clear();
            UpdateGasCost(_debugger.UsedGasCost);
            return true;
        }

        private bool GetDebugParameters()
        {
            //Run form with defaults from settings if available
            RunForm runForm = new RunForm(_debugger.ABI, _debugger.Tests, _debugger.ContractName, _settings.lastPrivateKey, _settings.lastParams, _settings.lastFunction);
            var result = runForm.ShowDialog();
            var debugParams = runForm.DebugParameters;
            if (result != DialogResult.OK)
                return false;

            if (runForm.currentMethod != null)
            {
                _settings.lastFunction = runForm.currentMethod.name;
            }

            if(RunForm.SelectedTestSequence == null)
            {
                return _debugger.SetDebugParameters(debugParams);
            }
            else
            {
                return true;
            }
           
        }

        #endregion

        #region Numbers, Bookmarks, Code Folding

        /// <summary>
        /// the background color of the text area
        /// </summary>
        private const int BACK_COLOR = 0x2A211C;

        /// <summary>
        /// default text color of the text area
        /// </summary>
        private const int FORE_COLOR = 0xB7B7B7;

        /// <summary>
        /// change this to whatever margin you want the line numbers to show in
        /// </summary>
        private const int NUMBER_MARGIN = 1;

        /// <summary>
        /// change this to whatever margin you want the bookmarks/breakpoints to show in
        /// </summary>
        private const int BOOKMARK_MARGIN = 2;
        private const int BREAKPOINT_MARKER = 2;
        private const int BREAKPOINT_BG = 3;
        private const int STEP_BG = 4;

        /// <summary>
        /// The mask to detect a breakpoint marker
        /// </summary>
        const uint BREAKPOINT_MASK = (1 << BREAKPOINT_MARKER);

        /// <summary>
        /// change this to whatever margin you want the code folding tree (+/-) to show in
        /// </summary>
        private const int FOLDING_MARGIN = 3;

        /// <summary>
        /// set this true to show circular buttons for code folding (the [+] and [-] buttons on the margin)
        /// </summary>
        private const bool CODEFOLDING_CIRCULAR = true;

        private void InitNumberMargin()
        {

            TextArea.Styles[Style.LineNumber].BackColor = ColorUtil.IntToColor(BACK_COLOR);
            TextArea.Styles[Style.LineNumber].ForeColor = ColorUtil.IntToColor(FORE_COLOR);
            TextArea.Styles[Style.IndentGuide].ForeColor = ColorUtil.IntToColor(FORE_COLOR);
            TextArea.Styles[Style.IndentGuide].BackColor = ColorUtil.IntToColor(BACK_COLOR);

            var nums = TextArea.Margins[NUMBER_MARGIN];
            nums.Width = 30;
            nums.Type = MarginType.Number;
            nums.Sensitive = true;
            nums.Mask = 0;

            TextArea.MarginClick += TextArea_MarginClick;
        }

        private void InitBookmarkMargin()
        {

            //TextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));

            var margin = TextArea.Margins[BOOKMARK_MARGIN];
            margin.Width = 20;
            margin.Sensitive = true;
            margin.Type = MarginType.Symbol;
            margin.Mask = (1 << BREAKPOINT_MARKER);
            //margin.Cursor = MarginCursor.Arrow;

            var marker = TextArea.Markers[BREAKPOINT_MARKER];
            marker.Symbol = MarkerSymbol.Circle;
            marker.SetBackColor(ColorUtil.IntToColor(0xFF003B));
            marker.SetForeColor(ColorUtil.IntToColor(0x000000));
            marker.SetAlpha(100);

            marker = TextArea.Markers[BREAKPOINT_BG];
            marker.Symbol = MarkerSymbol.Background;
            marker.SetBackColor(ColorUtil.IntToColor(0x600000));

            marker = TextArea.Markers[STEP_BG];
            marker.Symbol = MarkerSymbol.Background;
            marker.SetBackColor(ColorUtil.IntToColor(0x5a5a23));
        }

        private void InitCodeFolding()
        {

            TextArea.SetFoldMarginColor(true, ColorUtil.IntToColor(BACK_COLOR));
            TextArea.SetFoldMarginHighlightColor(true, ColorUtil.IntToColor(BACK_COLOR));

            // Enable code folding
            TextArea.SetProperty("fold", "1");
            TextArea.SetProperty("fold.compact", "1");

            // Configure a margin to display folding symbols
            TextArea.Margins[FOLDING_MARGIN].Type = MarginType.Symbol;
            TextArea.Margins[FOLDING_MARGIN].Mask = Marker.MaskFolders;
            TextArea.Margins[FOLDING_MARGIN].Sensitive = true;
            TextArea.Margins[FOLDING_MARGIN].Width = 20;

            // Set colors for all folding markers
            for (int i = 25; i <= 31; i++)
            {
                TextArea.Markers[i].SetForeColor(ColorUtil.IntToColor(BACK_COLOR)); // styles for [+] and [-]
                TextArea.Markers[i].SetBackColor(ColorUtil.IntToColor(FORE_COLOR)); // styles for [+] and [-]
            }

            // Configure folding markers with respective symbols
            TextArea.Markers[Marker.Folder].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlus : MarkerSymbol.BoxPlus;
            TextArea.Markers[Marker.FolderOpen].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinus : MarkerSymbol.BoxMinus;
            TextArea.Markers[Marker.FolderEnd].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlusConnected : MarkerSymbol.BoxPlusConnected;
            TextArea.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            TextArea.Markers[Marker.FolderOpenMid].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinusConnected : MarkerSymbol.BoxMinusConnected;
            TextArea.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            TextArea.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            // Enable automatic folding
            TextArea.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);

        }

        private void InitColors()
        {
            TextArea.SetSelectionBackColor(true, ColorUtil.IntToColor(0x114D9C));
        }

        private void InitSyntaxColoring()
        {

            // Configure the default style
            TextArea.StyleResetDefault();
            TextArea.Styles[Style.Default].Font = "Consolas";
            TextArea.Styles[Style.Default].Size = 10;
            TextArea.Styles[Style.Default].BackColor = ColorUtil.IntToColor(0x212121);
            TextArea.Styles[Style.Default].ForeColor = ColorUtil.IntToColor(0xFFFFFF);
            TextArea.StyleClearAll();

            // Configure the CPP (C#) lexer styles
            TextArea.Styles[Style.Cpp.Identifier].ForeColor = ColorUtil.IntToColor(0xD0DAE2);
            TextArea.Styles[Style.Cpp.Comment].ForeColor = ColorUtil.IntToColor(0xBD758B);
            TextArea.Styles[Style.Cpp.CommentLine].ForeColor = ColorUtil.IntToColor(0x40BF57);
            TextArea.Styles[Style.Cpp.CommentDoc].ForeColor = ColorUtil.IntToColor(0x2FAE35);
            TextArea.Styles[Style.Cpp.Number].ForeColor = ColorUtil.IntToColor(0xD69D85);
            TextArea.Styles[Style.Cpp.String].ForeColor = ColorUtil.IntToColor(0xD69D85);
            TextArea.Styles[Style.Cpp.Character].ForeColor = ColorUtil.IntToColor(0xE95454);
            TextArea.Styles[Style.Cpp.Preprocessor].ForeColor = ColorUtil.IntToColor(0x8AAFEE);
            TextArea.Styles[Style.Cpp.Operator].ForeColor = ColorUtil.IntToColor(0xE0E0E0);
            TextArea.Styles[Style.Cpp.Regex].ForeColor = ColorUtil.IntToColor(0xD69D85);
            TextArea.Styles[Style.Cpp.CommentLineDoc].ForeColor = ColorUtil.IntToColor(0x77A7DB);
            TextArea.Styles[Style.Cpp.Word].ForeColor = ColorUtil.IntToColor(0x48A8EE);
            TextArea.Styles[Style.Cpp.Word2].ForeColor = ColorUtil.IntToColor(0xF98906);
            TextArea.Styles[Style.Cpp.CommentDocKeyword].ForeColor = ColorUtil.IntToColor(0xB3D991);
            TextArea.Styles[Style.Cpp.CommentDocKeywordError].ForeColor = ColorUtil.IntToColor(0xFF0000);
            TextArea.Styles[Style.Cpp.GlobalClass].ForeColor = ColorUtil.IntToColor(0x48A8EE);

            TextArea.Lexer = Lexer.Cpp;
        }

        private void InitHotkeys()
        {
            // register the hotkeys with the form
            HotKeyManager.AddHotKey(this, OpenSearch, Keys.F, true);
            HotKeyManager.AddHotKey(this, OpenFindDialog, Keys.F, true, false, true);
            HotKeyManager.AddHotKey(this, OpenReplaceDialog, Keys.R, true);
            HotKeyManager.AddHotKey(this, OpenReplaceDialog, Keys.H, true);
            HotKeyManager.AddHotKey(this, Uppercase, Keys.U, true);
            HotKeyManager.AddHotKey(this, Lowercase, Keys.L, true);
            HotKeyManager.AddHotKey(this, ZoomIn, Keys.Oemplus, true);
            HotKeyManager.AddHotKey(this, ZoomOut, Keys.OemMinus, true);
            HotKeyManager.AddHotKey(this, ZoomDefault, Keys.D0, true);
            HotKeyManager.AddHotKey(this, CloseSearch, Keys.Escape);

            // remove conflicting hotkeys from scintilla
            TextArea.ClearCmdKey(Keys.Control | Keys.F);
            TextArea.ClearCmdKey(Keys.Control | Keys.R);
            TextArea.ClearCmdKey(Keys.Control | Keys.H);
            TextArea.ClearCmdKey(Keys.Control | Keys.L);
            TextArea.ClearCmdKey(Keys.Control | Keys.U);

            TextArea.ClearCmdKey(Keys.F5);
            TextArea.ClearCmdKey(Keys.F6);
            TextArea.ClearCmdKey(Keys.F10);
            TextArea.ClearCmdKey(Keys.F12);

            HotKeyManager.AddHotKey(this, RunDebugger, Keys.F5);
            HotKeyManager.AddHotKey(this, OpenStorage, Keys.F6);
            HotKeyManager.AddHotKey(this, StepDebugger, Keys.F10);
       }

        public void InitDragDropFile()
        {
            TextArea.AllowDrop = true;
            TextArea.DragEnter += delegate (object sender, DragEventArgs e)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.None;
            };
            TextArea.DragDrop += delegate (object sender, DragEventArgs e)
            {

                // get file drop
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {

                    Array a = (Array)e.Data.GetData(DataFormats.FileDrop);
                    if (a != null)
                    {
                        string path = a.GetValue(0).ToString();

                        LoadContract(path);
                    }
                }
            };

        }

        #endregion

        #region Main Form Events and Commands

        private void TextArea_MarginClick(object sender, MarginClickEventArgs e)
        {
            if (e.Margin == BOOKMARK_MARGIN)
            {
                var line = TextArea.Lines[TextArea.LineFromPosition(e.Position)];

                // Do we have a marker for this line?
                if ((line.MarkerGet() & BREAKPOINT_MASK) > 0)
                {
                    if (!_debugger.RemoveBreakpoint(line.Index))
                    {
                        SendLogToPanel("Error removing breakpoint.");
                        return;
                    }

                    // Remove existing from UI
                    line.MarkerDelete(BREAKPOINT_MARKER);
                }
                else
                {
                    if (!_debugger.AddBreakpoint(line.Index))
                    {
                        SendLogToPanel("Error adding breakpoint.");
                        return;
                    }

                    // Add breakpoint to UI
                    line.MarkerAdd(BREAKPOINT_MARKER);                
                }
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            var padding = 18;

            var logWidthPercent = 0.6f;

            logView.Width = (int)(this.ClientSize.Width * logWidthPercent) - (padding * 2);
            logView.Top = this.ClientSize.Height - (padding + logView.Height);
            logLabel.Top = logView.Top - 18;

            stackPanel.Width = this.ClientSize.Width - (logView.Width + padding * 2);
            stackPanel.Left = padding + logView.Width;
            stackPanel.Top = logView.Top;
            stackLabel.Left = stackPanel.Left;
            stackLabel.Top = logLabel.Top;

            projectTree.Height = stackPanel.Top - (projectTree.Top + padding);

            gasCostLabel.Left = this.ClientSize.Width - 105;
        }

        private bool ignoreTextChanges;

        private void TextArea_OnTextChanged(object sender, EventArgs e)
        {
            if (ignoreTextChanges)
            {
                return;
            }

            //Document is dirty, we will need to force a recompile before next debug
            _debugger.IsCompiled = false;
        }

        #endregion

        #region Main Menu Commands

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "NEO AVM files|*.avm";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ClearLog();
                
                if (LoadContract(openFileDialog.FileName))
                {
                    _settings.lastOpenedFile = openFileDialog.FileName;
                    _settings.Save();
                }
            }
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenSearch();
        }

        private void findDialogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFindDialog();
        }

        private void findAndReplaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenReplaceDialog();
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextArea.Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextArea.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextArea.Paste();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextArea.SelectAll();
        }

        private void selectLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Line line = TextArea.Lines[TextArea.CurrentLine];
            TextArea.SetSelection(line.Position + line.Length, line.Position);
        }

        private void clearSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextArea.SetEmptySelection(0);
        }

        private void indentSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Indent();
        }

        private void outdentSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Outdent();
        }

        private void uppercaseSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Uppercase();
        }

        private void lowercaseSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Lowercase();
        }

        private void wordWrapToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // toggle word wrap
            wordWrapItem.Checked = !wordWrapItem.Checked;
            TextArea.WrapMode = wordWrapItem.Checked ? WrapMode.Word : WrapMode.None;
        }

        private void indentGuidesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // toggle indent guides
            indentGuidesItem.Checked = !indentGuidesItem.Checked;
            TextArea.IndentationGuides = indentGuidesItem.Checked ? IndentView.LookBoth : IndentView.None;
        }

        private void hiddenCharactersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // toggle view whitespace
            hiddenCharactersItem.Checked = !hiddenCharactersItem.Checked;
            TextArea.ViewWhitespace = hiddenCharactersItem.Checked ? WhitespaceMode.VisibleAlways : WhitespaceMode.Invisible;
        }

        private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ZoomIn();
        }

        private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ZoomOut();
        }

        private void zoom100ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ZoomDefault();
        }

        private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextArea.FoldAll(FoldAction.Contract);
        }

        private void expandAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextArea.FoldAll(FoldAction.Expand);
        }

        private void LoadContractTemplate(string fileName)
        {
            string templatePath = Path.Combine(System.Environment.CurrentDirectory, "Contracts", fileName);

            if (!File.Exists(templatePath))
            {
                SendLogToPanel("Could not load template: "+templatePath);
                return;
            }

            try
            {
                _sourceFileName = fileName;
                _debugger.Clear();

                string templateCode = File.ReadAllText(templatePath);

                var language = LanguageSupport.DetectLanguage(fileName);

                ReloadTextArea(fileName, templateCode);

                if (!this.CompileContract())
                {
                    MessageBox.Show($"Could not compile {LanguageSupport.GetLanguageName(language)} template!");
                    return;
                }

                // We force reload of the avm in order to initialize everything properly (eg: build the project file explorer)
                LoadContract(_debugger.AvmFilePath);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Loading contract template failed with exception: " + e.Message);
            }
        }

        private SourceLanguage FromMenuItem(ToolStripItem item)
        {
            var language = (item.Text == "C#") ? SourceLanguage.CSharp : (SourceLanguage)Enum.Parse(typeof(SourceLanguage), item.Text);
            return language;
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;


            var language = FromMenuItem(item);
            string extension = LanguageSupport.GetExtension(language);


            var templateFileName = $"HelloWorld{extension}";
            LoadContractTemplate(templateFileName);
        }

        private void newFromTemplateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;


            var parent = (ToolStripMenuItem)item.OwnerItem;
            var language = FromMenuItem(parent);
            var extension = LanguageSupport.GetExtension(language);

            var templateFileName = String.Format("{0}{1}", Regex.Replace(item.Text, @"\s+", ""), extension);

            LoadContractTemplate(templateFileName);
        }

        #endregion

        #region Uppercase / Lowercase

        private void Lowercase()
        {

            // save the selection
            int start = TextArea.SelectionStart;
            int end = TextArea.SelectionEnd;

            // modify the selected text
            TextArea.ReplaceSelection(TextArea.GetTextRange(start, end - start).ToLower());

            // preserve the original selection
            TextArea.SetSelection(start, end);
        }

        private void Uppercase()
        {

            // save the selection
            int start = TextArea.SelectionStart;
            int end = TextArea.SelectionEnd;

            // modify the selected text
            TextArea.ReplaceSelection(TextArea.GetTextRange(start, end - start).ToUpper());

            // preserve the original selection
            TextArea.SetSelection(start, end);
        }

        #endregion

        #region Indent / Outdent

        private void Indent()
        {
            // we use this hack to send "Shift+Tab" to scintilla, since there is no known API to indent,
            // although the indentation function exists. Pressing TAB with the editor focused confirms this.
            GenerateKeystrokes("{TAB}");
        }

        private void Outdent()
        {
            // we use this hack to send "Shift+Tab" to scintilla, since there is no known API to outdent,
            // although the indentation function exists. Pressing Shift+Tab with the editor focused confirms this.
            GenerateKeystrokes("+{TAB}");
        }

        private void GenerateKeystrokes(string keys)
        {
            HotKeyManager.Enable = false;
            TextArea.Focus();
            SendKeys.Send(keys);
            HotKeyManager.Enable = true;
        }

        #endregion

        #region Zoom

        private void ZoomIn()
        {
            TextArea.ZoomIn();
        }

        private void ZoomOut()
        {
            TextArea.ZoomOut();
        }

        private void ZoomDefault()
        {
            TextArea.Zoom = 0;
        }


        #endregion

        #region Quick Search Bar

        bool SearchIsOpen = false;

        public object IEnumerabl { get; private set; }

        private void OpenSearch()
        {

            SearchManager.SearchBox = TxtSearch;
            SearchManager.TextArea = TextArea;

            if (!SearchIsOpen)
            {
                SearchIsOpen = true;
                InvokeIfNeeded(delegate ()
                {
                    PanelSearch.Visible = true;
                    TxtSearch.Text = SearchManager.LastSearch;
                    TxtSearch.Focus();
                    TxtSearch.SelectAll();
                });
            }
            else
            {
                InvokeIfNeeded(delegate ()
                {
                    TxtSearch.Focus();
                    TxtSearch.SelectAll();
                });
            }
        }
        private void CloseSearch()
        {
            if (SearchIsOpen)
            {
                SearchIsOpen = false;
                InvokeIfNeeded(delegate ()
                {
                    PanelSearch.Visible = false;
                    //CurBrowser.GetBrowser().StopFinding(true);
                });
            }
        }

        private void BtnClearSearch_Click(object sender, EventArgs e)
        {
            CloseSearch();
        }

        private void BtnPrevSearch_Click(object sender, EventArgs e)
        {
            SearchManager.Find(false, false);
        }
        private void BtnNextSearch_Click(object sender, EventArgs e)
        {
            SearchManager.Find(true, false);
        }
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            SearchManager.Find(true, true);
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (HotKeyManager.IsHotkey(e, Keys.Enter))
            {
                SearchManager.Find(true, false);
            }
            if (HotKeyManager.IsHotkey(e, Keys.Enter, true) || HotKeyManager.IsHotkey(e, Keys.Enter, false, true))
            {
                SearchManager.Find(false, false);
            }
        }

        #endregion

        #region Find & Replace Dialog

        private void OpenFindDialog()
        {

        }

        private void OpenReplaceDialog()
        {


        }

        #endregion

        #region Debugger UI Helpers

        private void UpdateDebuggerStateUI()
        {
            RemoveCurrentHighlight();

            if (_debugger.CurrentFilePath != activeFilePath)
            {
                ReloadTextArea(_debugger.CurrentFilePath);
                JumpToLine(_debugger.CurrentLine);
            }

            //Update the UI to reflect the debugger state
            switch (_debugger.Info.state)
            {
                case DebuggerState.State.Running:
                    {
                        HighlightLine(_debugger.CurrentLine);
                        break;
                    }

                case DebuggerState.State.Finished:
                    {
                        StackItem val;
                        try
                        {
                            val = _debugger.Emulator.GetOutput();
                        }
                        catch
                        {
                            val = null;
                        }
                        
                        var gasStr = string.Format("{0:N4}", _debugger.Emulator.usedGas);

                        var hintType = !string.IsNullOrEmpty(_settings.lastFunction) && _debugger.ABI != null && _debugger.ABI.functions.ContainsKey(_settings.lastFunction) ? _debugger.ABI.functions[_settings.lastFunction].returnType : Emulator.Type.Unknown;

                        MessageBox.Show("Execution finished.\nGAS cost: " + gasStr + "\nInstruction count: "+_debugger.Emulator.usedOpcodeCount+"\nResult: " + FormattingUtils.StackItemAsString(val, false, hintType));

                        Exception ex = _debugger.profiler.DumpCSV(_debugger.AvmFilePath);
                        if (ex != null)
                        {
                            MessageBox.Show(ex.Message, "Profiler Dump CSV");
                        }
                        break;
                    }

                case DebuggerState.State.Exception:
                    {
                        MessageBox.Show("Execution failed with an exception at address " + _debugger.Emulator.GetInstructionPtr().ToString() + " lastOffset: " + _debugger.Offset.ToString());
                        JumpToLine(_debugger.CurrentLine);
                        TextArea.Lines[TextArea.CurrentLine].MarkerAdd(BREAKPOINT_BG); //Highlight red
                        break;
                    }

                case DebuggerState.State.Break:
                    {
                        JumpToLine(_debugger.CurrentLine);
                        TextArea.Lines[TextArea.CurrentLine].MarkerAdd(BREAKPOINT_BG);
                        break;
                    }
            }
        }

        private Dictionary<string, int> _currentHightlight = new Dictionary<string, int>();

        private int GetHighlightedLine()
        {
            if (string.IsNullOrEmpty(activeFilePath))
            {
                return -1;
            }

            if (_currentHightlight.ContainsKey(activeFilePath))
            {
                return _currentHightlight[activeFilePath];
            }

            return -1;
        }

        private void RemoveCurrentHighlight()
        {
            int line = GetHighlightedLine();
            if (line != -1)
            {
                TextArea.Lines[line].MarkerDelete(BREAKPOINT_BG);
                TextArea.Lines[line].MarkerDelete(STEP_BG);

                _currentHightlight.Remove(activeFilePath);
            }
        }

        private void HighlightLine(int line)
        {
            RemoveCurrentHighlight();
            JumpToLine(_debugger.CurrentLine);

            TextArea.Lines[line].MarkerAdd(STEP_BG);
            _currentHightlight[activeFilePath] = line;
        }

        private void JumpToLine(int line)
        {
            if (line < 0)
            {
                return;
            }

            var targetLine = TextArea.Lines[line];
            targetLine.EnsureVisible();
            targetLine.Goto();

            var totalLines = TextArea.Lines.Count();
            var firstVisible = TextArea.FirstVisibleLine;
            var lastVisible = TextArea.FirstVisibleLine + TextArea.LinesOnScreen;
            var targetIndex = targetLine.Index;
            var paddingLines = 10;

            if (targetIndex < firstVisible)
            {
                TextArea.LineScroll(firstVisible - targetIndex + paddingLines, 0);
            }
            else if (targetIndex > (lastVisible - paddingLines) && lastVisible < (totalLines - paddingLines))
            {
                TextArea.LineScroll(lastVisible - targetIndex + paddingLines, 0);
            }
            else if (targetIndex < (firstVisible + paddingLines) && targetIndex < (totalLines - paddingLines))
            {
                TextArea.LineScroll(targetIndex - firstVisible - paddingLines, 0);
            }
        }

        private TreeNode selectedNode = null;
        private Dictionary<string, TreeNode> nodeMap = new Dictionary<string, TreeNode>();

        // use to compare with debugger current file, to check if something changed when stepping through code
        private string activeFilePath;

        private void ReloadTextArea(string filePath)
        {
            string content;

            try
            {
                content = _debugger.GetContentFor(filePath);
            }
            catch
            {
                if (File.Exists(filePath))
                {
                    content = File.ReadAllText(filePath);
                }
                else
                {
                    SendLogToPanel($"Could not load file: {filePath}");
                    return;
                }
            }

            ReloadTextArea(filePath, content);
        }

        // file path must exist in current project!
        private void ReloadTextArea(string filePath, string content)
        {
            if (selectedNode != null)
            {
                selectedNode.NodeFont = new Font(this.Font, FontStyle.Regular);
            }

            if (_debugger.CurrentFilePath != filePath)
            {
                _debugger.CurrentFilePath = filePath;
            }

            FileName.Text = filePath;
            activeFilePath = filePath;

            _sourceLanguage = LanguageSupport.DetectLanguage(filePath);

            var keywords = LanguageSupport.GetLanguageKeywords(_sourceLanguage);

            if (keywords.Length == 2)
            {
                TextArea.SetKeywords(0, keywords[0]);
                TextArea.SetKeywords(1, keywords[1]);
            }

            ignoreTextChanges = true;
            TextArea.ReadOnly = false;
            TextArea.Text = content;
            TextArea.ReadOnly = filePath == _debugger.AvmFilePath;
            ignoreTextChanges = false;

            foreach (TreeNode node in projectTree.Nodes)
            {
                if (node.Name == filePath)
                {
                    node.NodeFont = new Font(this.Font, FontStyle.Bold);
                    selectedNode = node;
                    break;
                }
            }

            // here we iterate over all breakpoints set by the user in the current file
            foreach (var breakpoint in _debugger.Breakpoints)
            {
                if (breakpoint.filePath != filePath)
                {
                    continue;
                }

                var line = TextArea.Lines[breakpoint.lineNumber];
                line.MarkerAdd(BREAKPOINT_MARKER);
            }

            if (_debugger.IsSteppingOrOnBreakpoint)
            {
                HighlightLine(_debugger.CurrentLine);
            }

            if (_debugger.Info.state == DebuggerState.State.Break || _debugger.Info.state == DebuggerState.State.Exception)
            {
                try
                {
                    if (filePath == _debugger.AvmFilePath)
                    {
                        int currentLine = _debugger.avmDisassemble.ResolveLine(_debugger.Offset);

                        HighlightLine(currentLine);
                    }
                    else
                    {
                        string temp;
                        int currentLine = _debugger.Map.ResolveLine(_debugger.Offset, out temp);

                        if (temp == filePath)
                        {
                            HighlightLine(currentLine);
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }


            if (!nodeMap.ContainsKey(filePath))
            {
                AddNodeToProjectTree(filePath);
            }
        }

        private TreeNode AddNodeToProjectTree(string path)
        {
            var fileName = Path.GetFileName(path);
            var node = projectTree.Nodes.Add(path, fileName);
            nodeMap[path] = node;

            return node;
        }

        private void ReloadProjectTree()
        {
            selectedNode = null;
            projectTree.Nodes.Clear();

            AddNodeToProjectTree(_debugger.AvmFilePath);

            _debugger.Emulator.ClearAssignments();

            if (_debugger.IsMapLoaded)
            {
                foreach (var path in _debugger.Map.FileNames)
                {
                    AddNodeToProjectTree(path);

                    _debugger.LoadAssignmentsFromContent(path);
                }
            }
        }

        private string ValidateHover(string text)
        {
            if (_debugger == null || _debugger.Emulator == null)
            {
                return null;
            }

            var variable = _debugger.Emulator.GetVariable(text);
            if (variable == null)
            {
                return null;
            }

            return text + " = " +FormattingUtils.StackItemAsString(variable.value, false, variable.type);
        }

        private void OpenStorage()
        {
            if (!_debugger.SmartContractDeployed)
            {
                MessageBox.Show("Please deploy the smart contract first!");
                return;
            }

            var form = new StorageForm(_debugger.Emulator);
            form.ShowDialog();
        }

        #endregion

        #region DEBUG PANELS

        public void SendLogToPanel(string s)
        {
            logView.Text += s + "\n";
        }

        public void ClearLog()
        {
            logView.Text = "";
        }

        private void UpdateStackPanel()
        {
            var sb = new StringBuilder();

            var evalStack = _debugger.Emulator.GetEvaluationStack().ToArray();
            var altStack = _debugger.Emulator.GetAltStack().ToArray();

            stackPanel.Rows.Clear();

            int index = Math.Max(evalStack.Length, altStack.Length) - 1;
            while (index>=0)
            {
                string a = index < evalStack.Length ? FormattingUtils.StackItemAsString(evalStack[index]) : "";
                string b = index < altStack.Length ? FormattingUtils.StackItemAsString(altStack[index]) : "";
                stackPanel.Rows.Add(new object[] { index, a,b });
                index--;
            }
        }

        private void UpdateGasCost(decimal gasUsed)
        {
            gasCostLabel.Visible = true;
            gasCostLabel.Text = "GAS used: " + gasUsed; 
        }

        #endregion

        #region DEBUG MENU

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_debugger.AvmFileLoaded)
                return;

            RunDebugger();
        }
        
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (!_debugger.AvmFileLoaded)
                return;

            OpenStorage();
        }

        private void stepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(!_debugger.AvmFileLoaded)
                return;

            StepDebugger();
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_debugger.AvmFileLoaded)
                return;

            ResetDebugger();
        }

        private void originalToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void assemblyToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void blockchainToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_debugger.BlockchainLoaded)
            {
                MessageBox.Show("Please deploy the smart contract first!");
                return;
            }

            var form = new BlockchainForm(_debugger.Blockchain);
            form.ShowDialog();
        }

        private void cCompilerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearLog();

            if (!CompileContract())
                return;

            LoadContract(_debugger.AvmFilePath);
        }

        private void Form_LoadCompiledContract(object sender, LoadCompiledContractEventArgs e)
        {
            LoadContract(e.AvmPath);
        }

        private void keyDecoderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = new KeyToolForm();
            form.ShowDialog();
        }

        #endregion

        #region UI Helpers

        //Make sure the update executes on the main UI thread
        public void InvokeIfNeeded(Action action)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(action);
            }
            else
            {
                action.Invoke();
            }
        }

        #endregion

        #region Helpers

        private void _debugger_SendToLog(object sender, DebugManagerLogEventArgs e)
        {
            SendLogToPanel(e.Message);
        }

        #endregion

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void resetToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (!_debugger.BlockchainLoaded)
            {
                MessageBox.Show("No blockchain loaded yet!");
                return;
            }

            if (_debugger.Blockchain.currentHeight > 1)
            {
                if (MessageBox.Show("The current loaded Blockchain already has some transactions. This action can not be reversed, are you sure you want to reset it?", "Blockchain Reset",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1) != System.Windows.Forms.DialogResult.Yes)
                {
                    return;
                }
            }

            _debugger.Blockchain.Reset();
            _debugger.Blockchain.Save();

            SendLogToPanel("Reset to virtual blockchain at path: "+_debugger.Blockchain.fileName);
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "Virtual blockchain files|*.chain.json";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _debugger.Blockchain.Load(openFileDialog.FileName);
            }
        }

        private void projectTree_DoubleClick(object sender, EventArgs e)
        {
            var node = projectTree.GetNodeAt(projectTree.PointToClient(Cursor.Position));
            if (node != null)
            {
                ReloadTextArea(node.Name);
            }
        }

        private void rebuildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_debugger.IsCompiled)
            {
                _debugger.IsCompiled = false;

                var fileList = new List<string>();
                fileList.Add(_debugger.AvmFilePath);
                fileList.Add(_debugger.AvmFilePath.Replace(".avm", ".debug.json"));
                fileList.Add(_debugger.AvmFilePath.Replace(".avm", ".abi.json"));
                fileList.Add(_debugger.AvmFilePath.Replace(".avm", ".test.json"));

                foreach (var file in fileList)
                {
                    if (File.Exists(file))
                    {
                        try
                        {
                            File.Delete(file);
                            SendLogToPanel("Deleted " + file);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }

            CompileContract();
        }

        private void neoLuxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_debugger.ABI == null || _debugger.ABI.functions.Count == 0)
            {
                MessageBox.Show("No ABI loaded for this contract!");
                return;
            }

            var code = NeoLux.GenerateInterface(_debugger.ABI);

            var path = _debugger.ContractName + "Client.cs";
            _debugger.SetContentFor(path, code);
            AddNodeToProjectTree(path);

            ReloadTextArea(path);
        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
            if (_debugger != null && _debugger.AvmFileLoaded)
            {
                var curTime = File.GetLastWriteTime(_debugger.AvmFilePath);
                if (curTime != _contractModTime)
                {
                    if (MessageBox.Show("The AVM was changed externally, reload it?", "AVM changed", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        LoadContract(_debugger.AvmFilePath);
                    }
                }
            }
        }
    }
}
