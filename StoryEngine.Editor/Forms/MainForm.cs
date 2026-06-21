using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using StoryEngine.Editor.Services;
using StoryEngine.Engine;
using StoryEngine.Models;

namespace StoryEngine.Editor.Forms
{
    public class MainForm : Form
    {
        private readonly EditorRepository _repo = new();
        private readonly StoryValidator _validator = new();

        private StoryDefinition _story = new() { Title = "Poveste nouă", Properties = new(), Blocks = new() };
        private string _projectDir;
        private string? _currentZipPath;
        private bool _dirty;
        private bool _suppressEvents;

        private List<StoryBlock> _displayedBlocks = new();
        private StoryBlock? _selectedBlock;

        // ── top metadata controls ───────────────────────────────────────
        private TextBox _txtTitle = null!;
        private TextBox _txtAuthor = null!;
        private TextBox _txtDescription = null!;
        private ComboBox _cmbStartBlock = null!;

        // ── left: block list ─────────────────────────────────────────────
        private TextBox _txtSearch = null!;
        private ListBox _lstBlocks = null!;

        // ── right: block editor ──────────────────────────────────────────
        private TextBox _txtBlockId = null!;
        private TextBox _txtBlockText = null!;
        private TextBox _txtBackgroundImage = null!;
        private PictureBox _picBackgroundPreview = null!;
        private CheckBox _chkIsFinal = null!;
        private TextBox _txtEventCategory = null!;
        private DataGridView _dgvDecisions = null!;
        private Label _lblStatus = null!;

        public MainForm()
        {
            _projectDir = _repo.CreateNewProjectFolder();

            Text = "Story Engine — Editor de poveste";
            Width = 1180;
            Height = 760;
            StartPosition = FormStartPosition.CenterScreen;

            BuildMenu();
            BuildLayout();

            RefreshBlockList();
            RefreshStartBlockCombo();
            LoadBlockIntoForm(null);
            SetDirty(false);
        }

        // ════════════════════════════════════════════════════════════════
        //  MENU
        // ════════════════════════════════════════════════════════════════

        private void BuildMenu()
        {
            var menu = new MenuStrip();

            var fileMenu = new ToolStripMenuItem("Fișier");
            fileMenu.DropDownItems.Add("Poveste nouă", null, (s, e) => NewStory());
            fileMenu.DropDownItems.Add("Deschide... (.zip)", null, (s, e) => OpenStory());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Salvează", null, (s, e) => SaveStory(false));
            fileMenu.DropDownItems.Add("Salvează ca...", null, (s, e) => SaveStory(true));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Ieșire", null, (s, e) => Close());

            var editMenu = new ToolStripMenuItem("Editare");
            editMenu.DropDownItems.Add("Proprietăți de stare...", null, (s, e) => EditProperties());

            var toolsMenu = new ToolStripMenuItem("Validare");
            toolsMenu.DropDownItems.Add("Verifică povestea (Check Story)", null, (s, e) => ValidateStory(silentIfOk: false));

            menu.Items.Add(fileMenu);
            menu.Items.Add(editMenu);
            menu.Items.Add(toolsMenu);

            MainMenuStrip = menu;
            Controls.Add(menu);
        }

        // ════════════════════════════════════════════════════════════════
        //  LAYOUT
        // ════════════════════════════════════════════════════════════════

        private void BuildLayout()
        {
            // status bar
            _lblStatus = new Label { Dock = DockStyle.Bottom, Height = 24, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0), Text = "Pregătit." };

            // top metadata panel
            var pnlMeta = BuildMetaPanel();

            // main split: left list / right editor
            var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 280 };
            split.Panel1.Controls.Add(BuildBlockListPanel());
            split.Panel2.Controls.Add(BuildBlockEditorPanel());

            Controls.Add(split);
            Controls.Add(pnlMeta);
            Controls.Add(_lblStatus);
        }

        private Panel BuildMetaPanel()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 96, Padding = new Padding(8) };
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2 };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

            _txtTitle = new TextBox { Dock = DockStyle.Fill, Text = _story.Title ?? "" };
            _txtAuthor = new TextBox { Dock = DockStyle.Fill, Text = _story.Author ?? "" };
            _txtDescription = new TextBox { Dock = DockStyle.Fill, Text = _story.Description ?? "" };
            _cmbStartBlock = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };

            _txtTitle.TextChanged += (s, e) => { if (_suppressEvents) return; _story.Title = _txtTitle.Text; SetDirty(true); };
            _txtAuthor.TextChanged += (s, e) => { if (_suppressEvents) return; _story.Author = _txtAuthor.Text; SetDirty(true); };
            _txtDescription.TextChanged += (s, e) => { if (_suppressEvents) return; _story.Description = _txtDescription.Text; SetDirty(true); };
            _cmbStartBlock.SelectedIndexChanged += (s, e) =>
            {
                if (_suppressEvents) return;
                _story.StartBlock = _cmbStartBlock.SelectedItem as string ?? "";
                SetDirty(true);
            };

            grid.Controls.Add(new Label { Text = "Titlu poveste:", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 6, 0) }, 0, 0);
            grid.Controls.Add(_txtTitle, 1, 0);
            grid.Controls.Add(new Label { Text = "Bloc de start:", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(12, 6, 6, 0) }, 2, 0);
            grid.Controls.Add(_cmbStartBlock, 3, 0);

            grid.Controls.Add(new Label { Text = "Autor:", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 6, 0) }, 0, 1);
            grid.Controls.Add(_txtAuthor, 1, 1);
            grid.Controls.Add(new Label { Text = "Descriere:", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(12, 6, 6, 0) }, 2, 1);
            grid.Controls.Add(_txtDescription, 3, 1);

            panel.Controls.Add(grid);
            return panel;
        }

        private Panel BuildBlockListPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            _txtSearch = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Caută bloc (id sau text)..." };
            _txtSearch.TextChanged += (s, e) => RefreshBlockList();

            _lstBlocks = new ListBox { Dock = DockStyle.Fill };
            _lstBlocks.SelectedIndexChanged += LstBlocks_SelectedIndexChanged;

            var pnlButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 36 };
            var btnAdd = new Button { Text = "Adaugă bloc", AutoSize = true };
            var btnDelete = new Button { Text = "Șterge bloc", AutoSize = true };
            var btnRename = new Button { Text = "Redenumește...", AutoSize = true };
            btnAdd.Click += (s, e) => AddBlock();
            btnDelete.Click += (s, e) => DeleteSelectedBlock();
            btnRename.Click += (s, e) => RenameSelectedBlock();
            pnlButtons.Controls.Add(btnAdd);
            pnlButtons.Controls.Add(btnDelete);
            pnlButtons.Controls.Add(btnRename);

            panel.Controls.Add(_lstBlocks);
            panel.Controls.Add(pnlButtons);
            panel.Controls.Add(_txtSearch);
            return panel;
        }

        private Panel BuildBlockEditorPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            // header: id / background / final / category
            var header = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, AutoSize = true, Padding = new Padding(8) };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _txtBlockId = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = SystemColors.Control };

            _txtBackgroundImage = new TextBox { Width = 220, ReadOnly = true };
            var btnPickBg = new Button { Text = "Alege imagine...", AutoSize = true };
            var btnClearBg = new Button { Text = "Șterge", AutoSize = true };
            btnPickBg.Click += (s, e) => PickBackgroundImage();
            btnClearBg.Click += (s, e) => ClearBackgroundImage();
            _picBackgroundPreview = new PictureBox { Width = 64, Height = 40, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Left };

            _chkIsFinal = new CheckBox { Text = "Bloc final (Game Over / Victorie)", AutoSize = true };
            _chkIsFinal.CheckedChanged += (s, e) =>
            {
                if (_suppressEvents || _selectedBlock == null) return;
                _selectedBlock.IsFinal = _chkIsFinal.Checked;
                SetDirty(true);
            };

            _txtEventCategory = new TextBox { Dock = DockStyle.Fill };
            _txtEventCategory.TextChanged += (s, e) =>
            {
                if (_suppressEvents || _selectedBlock == null) return;
                _selectedBlock.EventCategory = string.IsNullOrWhiteSpace(_txtEventCategory.Text) ? null : _txtEventCategory.Text;
                SetDirty(true);
            };

            header.Controls.Add(new Label { Text = "Id bloc:", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 6, 0) }, 0, 0);
            header.Controls.Add(_txtBlockId, 1, 0);
            header.Controls.Add(_chkIsFinal, 2, 0);
            header.SetColumnSpan(_chkIsFinal, 2);

            var pnlBg = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            pnlBg.Controls.Add(_txtBackgroundImage);
            pnlBg.Controls.Add(btnPickBg);
            pnlBg.Controls.Add(btnClearBg);
            pnlBg.Controls.Add(_picBackgroundPreview);

            header.Controls.Add(new Label { Text = "Imagine fundal:", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 6, 0) }, 0, 1);
            header.Controls.Add(pnlBg, 1, 1);
            header.Controls.Add(new Label { Text = "Categorie eveniment:", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(12, 6, 6, 0) }, 2, 1);
            header.Controls.Add(_txtEventCategory, 3, 1);

            // bottom: decisions
            var pnlDecisions = new Panel { Dock = DockStyle.Bottom, Height = 240 };
            var lblDecisions = new Label { Dock = DockStyle.Top, Text = "Decizii:", Height = 20, Padding = new Padding(0, 4, 0, 0) };
            var pnlDecisionButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 36 };
            var btnAddDecision = new Button { Text = "Adaugă decizie", AutoSize = true };
            var btnEditDecision = new Button { Text = "Editează decizie", AutoSize = true };
            var btnDelDecision = new Button { Text = "Șterge decizie", AutoSize = true };
            btnAddDecision.Click += (s, e) => AddDecision();
            btnEditDecision.Click += (s, e) => EditSelectedDecision();
            btnDelDecision.Click += (s, e) => DeleteSelectedDecision();
            pnlDecisionButtons.Controls.Add(btnAddDecision);
            pnlDecisionButtons.Controls.Add(btnEditDecision);
            pnlDecisionButtons.Controls.Add(btnDelDecision);

            _dgvDecisions = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _dgvDecisions.Columns.Add("Text", "Text buton");
            _dgvDecisions.Columns.Add("Target", "Destinație");
            _dgvDecisions.Columns.Add("Condition", "Condiție");
            _dgvDecisions.Columns.Add("Effects", "Efecte");
            _dgvDecisions.CellDoubleClick += (s, e) => EditSelectedDecision();

            pnlDecisions.Controls.Add(_dgvDecisions);
            pnlDecisions.Controls.Add(pnlDecisionButtons);
            pnlDecisions.Controls.Add(lblDecisions);

            // middle: story text
            var pnlText = new Panel { Dock = DockStyle.Fill };
            var lblText = new Label { Dock = DockStyle.Top, Text = "Text poveste:", Height = 20, Padding = new Padding(0, 4, 0, 0) };
            _txtBlockText = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Segoe UI", 10f) };
            _txtBlockText.TextChanged += (s, e) =>
            {
                if (_suppressEvents || _selectedBlock == null) return;
                _selectedBlock.Text = _txtBlockText.Text;
                SetDirty(true);
            };
            pnlText.Controls.Add(_txtBlockText);
            pnlText.Controls.Add(lblText);

            panel.Controls.Add(header);
            panel.Controls.Add(pnlDecisions);
            panel.Controls.Add(pnlText);
            return panel;
        }

        // ════════════════════════════════════════════════════════════════
        //  BLOCK LIST
        // ════════════════════════════════════════════════════════════════

        private void RefreshBlockList()
        {
            string filter = _txtSearch?.Text?.Trim().ToLowerInvariant() ?? "";

            _displayedBlocks = string.IsNullOrEmpty(filter)
                ? _story.Blocks.ToList()
                : _story.Blocks.Where(b =>
                        (b.Id?.ToLowerInvariant().Contains(filter) ?? false) ||
                        (b.Text?.ToLowerInvariant().Contains(filter) ?? false))
                    .ToList();

            string? previouslySelectedId = _selectedBlock?.Id;

            _lstBlocks.Items.Clear();
            foreach (var b in _displayedBlocks)
                _lstBlocks.Items.Add(b.Id);

            if (previouslySelectedId != null)
            {
                int idx = _displayedBlocks.FindIndex(b => b.Id == previouslySelectedId);
                if (idx >= 0) _lstBlocks.SelectedIndex = idx;
            }
        }

        private void RefreshStartBlockCombo()
        {
            _suppressEvents = true;
            _cmbStartBlock.Items.Clear();
            foreach (var b in _story.Blocks)
                _cmbStartBlock.Items.Add(b.Id);
            if (!string.IsNullOrEmpty(_story.StartBlock) && _story.Blocks.Any(b => b.Id == _story.StartBlock))
                _cmbStartBlock.SelectedItem = _story.StartBlock;
            _suppressEvents = false;
        }

        private void LstBlocks_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int idx = _lstBlocks.SelectedIndex;
            var newBlock = (idx >= 0 && idx < _displayedBlocks.Count) ? _displayedBlocks[idx] : null;
            LoadBlockIntoForm(newBlock);
        }

        // ════════════════════════════════════════════════════════════════
        //  BLOCK EDITOR
        // ════════════════════════════════════════════════════════════════

        private void LoadBlockIntoForm(StoryBlock? block)
        {
            _selectedBlock = block;
            _suppressEvents = true;

            bool has = block != null;
            _txtBlockId.Text = block?.Id ?? "";
            _txtBlockText.Text = block?.Text ?? "";
            _txtBackgroundImage.Text = block?.BackgroundImage ?? "";
            _chkIsFinal.Checked = block?.IsFinal ?? false;
            _txtEventCategory.Text = block?.EventCategory ?? "";

            _txtBlockText.Enabled = has;
            _txtBackgroundImage.Enabled = has;
            _chkIsFinal.Enabled = has;
            _txtEventCategory.Enabled = has;

            UpdateBackgroundPreview();
            RefreshDecisionsGrid();

            _suppressEvents = false;
        }

        private void UpdateBackgroundPreview()
        {
            _picBackgroundPreview.Image?.Dispose();
            _picBackgroundPreview.Image = null;

            string filename = _txtBackgroundImage.Text;
            if (string.IsNullOrEmpty(filename)) return;

            string path = Path.Combine(_repo.GetImagesDir(_projectDir), filename);
            if (File.Exists(path))
            {
                try
                {
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                    _picBackgroundPreview.Image = Image.FromStream(fs);
                }
                catch { /* preview is best-effort */ }
            }
        }

        private void PickBackgroundImage()
        {
            if (_selectedBlock == null) return;

            using var dlg = new OpenFileDialog
            {
                Title = "Alege imaginea de fundal",
                Filter = "Imagini (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                InitialDirectory = _repo.GetImagesDir(_projectDir)
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                string filename = _repo.ImportImage(_projectDir, dlg.FileName);
                _selectedBlock.BackgroundImage = filename;
                _txtBackgroundImage.Text = filename;
                UpdateBackgroundPreview();
                SetDirty(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nu am putut importa imaginea:\n{ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearBackgroundImage()
        {
            if (_selectedBlock == null) return;
            _selectedBlock.BackgroundImage = null;
            _txtBackgroundImage.Text = "";
            UpdateBackgroundPreview();
            SetDirty(true);
        }

        // ── Add / delete / rename block ───────────────────────────────────

        private void AddBlock()
        {
            string? id = Prompt.ShowDialog("Bloc nou", "Id-ul noului bloc (ex: day2.intro):", "");
            if (string.IsNullOrWhiteSpace(id)) return;

            if (_story.Blocks.Any(b => b.Id == id))
            {
                MessageBox.Show($"Există deja un bloc cu id-ul '{id}'.", "Id duplicat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var block = new StoryBlock { Id = id, Text = "", Decisions = new List<DecisionDefinition>() };
            _story.Blocks.Add(block);

            if (string.IsNullOrEmpty(_story.StartBlock))
                _story.StartBlock = id;

            RefreshStartBlockCombo();
            RefreshBlockList();
            SelectBlockById(id);
            SetDirty(true);
        }

        private void DeleteSelectedBlock()
        {
            if (_selectedBlock == null) return;

            var result = MessageBox.Show(
                $"Ștergi blocul '{_selectedBlock.Id}'? Deciziile altor blocuri care îl țintesc vor deveni link-uri moarte (vor fi semnalate la Validare).",
                "Confirmare ștergere", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            _story.Blocks.Remove(_selectedBlock);
            if (_story.StartBlock == _selectedBlock.Id)
                _story.StartBlock = "";

            _selectedBlock = null;
            RefreshStartBlockCombo();
            RefreshBlockList();
            LoadBlockIntoForm(_displayedBlocks.FirstOrDefault());
            SetDirty(true);
        }

        private void RenameSelectedBlock()
        {
            if (_selectedBlock == null) return;
            string oldId = _selectedBlock.Id;

            string? newId = Prompt.ShowDialog("Redenumește bloc", "Id nou:", oldId);
            if (string.IsNullOrWhiteSpace(newId) || newId == oldId) return;

            if (_story.Blocks.Any(b => b.Id == newId))
            {
                MessageBox.Show($"Există deja un bloc cu id-ul '{newId}'.", "Id duplicat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedBlock.Id = newId;

            // Update every reference to the old id, so nothing silently breaks.
            if (_story.StartBlock == oldId) _story.StartBlock = newId;

            foreach (var b in _story.Blocks)
                foreach (var d in b.Decisions)
                    if (d.TargetBlock == oldId) d.TargetBlock = newId;

            foreach (var p in _story.Properties)
            {
                if (p.OnMinBlock == oldId) p.OnMinBlock = newId;
                if (p.OnMaxBlock == oldId) p.OnMaxBlock = newId;
            }

            RefreshStartBlockCombo();
            RefreshBlockList();
            SelectBlockById(newId);
            SetDirty(true);
            SetStatus($"Blocul '{oldId}' a fost redenumit în '{newId}'; toate referințele au fost actualizate.");
        }

        private void SelectBlockById(string id)
        {
            int idx = _displayedBlocks.FindIndex(b => b.Id == id);
            if (idx >= 0) _lstBlocks.SelectedIndex = idx;
        }

        // ════════════════════════════════════════════════════════════════
        //  DECISIONS
        // ════════════════════════════════════════════════════════════════

        private void RefreshDecisionsGrid()
        {
            _dgvDecisions.Rows.Clear();
            if (_selectedBlock == null) return;

            foreach (var d in _selectedBlock.Decisions)
            {
                string cond = d.Condition == null ? "—" : SummarizeCondition(d.Condition);
                string fx = d.Effects.Count == 0 ? "—" : string.Join(", ", d.Effects.Select(e => $"{e.Property} {(e.Type == EffectType.ADD ? "+=" : "=")} {e.Value}"));
                _dgvDecisions.Rows.Add(d.Text, d.TargetBlock, cond, fx);
            }
        }

        private static string SummarizeCondition(ConditionDefinition c)
        {
            if (c.Type == "COMPARISON") return $"{c.Property} {c.Operator} {c.Value}";
            string inner = string.Join($" {c.Type} ", c.Operands?.Select(SummarizeCondition) ?? Enumerable.Empty<string>());
            return $"({inner})";
        }

        private void AddDecision()
        {
            if (_selectedBlock == null)
            {
                MessageBox.Show("Selectează mai întâi un bloc.", "Niciun bloc selectat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var blank = new DecisionDefinition { Effects = new List<EffectDefinition>() };
            using var dlg = new DecisionEditorForm(blank, _story.Blocks.Select(b => b.Id).ToList(),
                _story.Properties.Select(p => p.Key).ToList(), _repo, _projectDir);

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _selectedBlock.Decisions.Add(dlg.Result);
                RefreshDecisionsGrid();
                SetDirty(true);
            }
        }

        private void EditSelectedDecision()
        {
            if (_selectedBlock == null) return;
            int idx = _dgvDecisions.CurrentRow?.Index ?? -1;
            if (idx < 0 || idx >= _selectedBlock.Decisions.Count) return;

            var existing = _selectedBlock.Decisions[idx];
            using var dlg = new DecisionEditorForm(existing, _story.Blocks.Select(b => b.Id).ToList(),
                _story.Properties.Select(p => p.Key).ToList(), _repo, _projectDir);

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _selectedBlock.Decisions[idx] = dlg.Result;
                RefreshDecisionsGrid();
                SetDirty(true);
            }
        }

        private void DeleteSelectedDecision()
        {
            if (_selectedBlock == null) return;
            int idx = _dgvDecisions.CurrentRow?.Index ?? -1;
            if (idx < 0 || idx >= _selectedBlock.Decisions.Count) return;

            _selectedBlock.Decisions.RemoveAt(idx);
            RefreshDecisionsGrid();
            SetDirty(true);
        }

        // ════════════════════════════════════════════════════════════════
        //  PROPERTIES
        // ════════════════════════════════════════════════════════════════

        private void EditProperties()
        {
            using var dlg = new PropertiesForm(_story.Properties, _story.Blocks.Select(b => b.Id).ToList());
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _story.Properties = dlg.Result;
                SetDirty(true);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  VALIDATION
        // ════════════════════════════════════════════════════════════════

        /// <summary>Returns true if the story has no blocking errors (warnings are always allowed through).</summary>
        private bool ValidateStory(bool silentIfOk)
        {
            var result = _validator.Validate(_story);

            if (result.IsValid)
            {
                if (!silentIfOk)
                {
                    string msg = result.Warnings.Count == 0
                        ? "Povestea este validă. Niciun link mort, nicio problemă de structură."
                        : "Povestea este validă, dar există avertismente:\n\n" + string.Join("\n", result.Warnings.Select(w => "• " + w));
                    MessageBox.Show(msg, "Validare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return true;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"S-au găsit {result.Errors.Count} eroare/erori:");
            foreach (var err in result.Errors) sb.AppendLine("• " + err);
            if (result.Warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Avertismente:");
                foreach (var w in result.Warnings) sb.AppendLine("• " + w);
            }

            MessageBox.Show(sb.ToString(), "Povestea conține erori", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        // ════════════════════════════════════════════════════════════════
        //  FILE: NEW / OPEN / SAVE
        // ════════════════════════════════════════════════════════════════

        private bool ConfirmDiscardIfDirty()
        {
            if (!_dirty) return true;
            var result = MessageBox.Show(
                "Ai modificări nesalvate. Continui și le pierzi?",
                "Modificări nesalvate", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            return result == DialogResult.Yes;
        }

        private void NewStory()
        {
            if (!ConfirmDiscardIfDirty()) return;

            _repo.CleanupProjectFolder(_projectDir);
            _projectDir = _repo.CreateNewProjectFolder();
            _story = new StoryDefinition { Title = "Poveste nouă", Properties = new(), Blocks = new() };
            _currentZipPath = null;
            _selectedBlock = null;

            _suppressEvents = true;
            _txtTitle.Text = _story.Title;
            _txtAuthor.Text = "";
            _txtDescription.Text = "";
            _suppressEvents = false;

            RefreshStartBlockCombo();
            RefreshBlockList();
            LoadBlockIntoForm(null);
            SetDirty(false);
            SetStatus("Poveste nouă creată.");
        }

        private void OpenStory()
        {
            if (!ConfirmDiscardIfDirty()) return;

            using var dlg = new OpenFileDialog { Title = "Deschide poveste", Filter = "Story files (*.zip)|*.zip" };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                string newProjectDir = _repo.ExtractToProjectFolder(dlg.FileName);
                var loaded = _repo.LoadStory(newProjectDir);

                _repo.CleanupProjectFolder(_projectDir);
                _projectDir = newProjectDir;
                _story = loaded;
                _currentZipPath = dlg.FileName;
                _selectedBlock = null;

                _suppressEvents = true;
                _txtTitle.Text = _story.Title ?? "";
                _txtAuthor.Text = _story.Author ?? "";
                _txtDescription.Text = _story.Description ?? "";
                _suppressEvents = false;

                RefreshStartBlockCombo();
                RefreshBlockList();
                LoadBlockIntoForm(_displayedBlocks.FirstOrDefault());
                SetDirty(false);
                SetStatus($"Poveste încărcată din {Path.GetFileName(dlg.FileName)}.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare la încărcare:\n{ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveStory(bool forceSaveAs)
        {
            var result = _validator.Validate(_story);
            if (!result.IsValid)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Povestea conține erori:");
                foreach (var err in result.Errors) sb.AppendLine("• " + err);
                sb.AppendLine();
                sb.AppendLine("Salvezi oricum?");

                if (MessageBox.Show(sb.ToString(), "Erori de validare", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
            }

            string? targetPath = (forceSaveAs || _currentZipPath == null) ? PickSaveAsPath() : _currentZipPath;
            if (targetPath == null) return;

            try
            {
                _repo.SaveStoryJson(_story, _projectDir);
                _repo.ExportZip(_projectDir, targetPath);
                _currentZipPath = targetPath;
                SetDirty(false);
                SetStatus($"Poveste salvată în {Path.GetFileName(targetPath)}.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare la salvare:\n{ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string? PickSaveAsPath()
        {
            string suggested = SanitizeFileName(string.IsNullOrWhiteSpace(_story.Title) ? "poveste" : _story.Title) + ".zip";
            using var dlg = new SaveFileDialog { Title = "Salvează povestea ca...", Filter = "Story files (*.zip)|*.zip", FileName = suggested };
            return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : null;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        // ════════════════════════════════════════════════════════════════
        //  STATUS / DIRTY
        // ════════════════════════════════════════════════════════════════

        private void SetDirty(bool dirty)
        {
            _dirty = dirty;
            Text = "Story Engine — Editor de poveste" + (dirty ? "  •  modificări nesalvate" : "");
        }

        private void SetStatus(string message) => _lblStatus.Text = message;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_dirty)
            {
                var result = MessageBox.Show(
                    "Ai modificări nesalvate. Sigur vrei să închizi fără să salvezi?",
                    "Modificări nesalvate", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes) { e.Cancel = true; return; }
            }
            _repo.CleanupProjectFolder(_projectDir);
            base.OnFormClosing(e);
        }
    }
}
