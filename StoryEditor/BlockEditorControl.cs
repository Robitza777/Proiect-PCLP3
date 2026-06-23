using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using StoryEngine.Models;

namespace StoryEngine.Editor
{
    /// <summary>
    /// Editor pentru un StoryBlock: câmpuri de bază + lista de decizii (Add/Edit/Delete).
    /// </summary>
    public class BlockEditorControl : UserControl
    {
        public event Action BlockChanged;
        public event Action StartBlockRequested;

        private readonly StoryBlock _block;
        private readonly StoryDefinition _story;
        private readonly ImageWorkspace _images;
        private readonly string _workspaceDir;

        private TextBox _txtId;
        private TextBox _txtText;
        private CheckBox _chkIsFinal;
        private TextBox _txtBackgroundImage;
        private PictureBox _picBackgroundPreview;
        private TextBox _txtEventCategory;
        private Button _btnMakeStart;
        private Label _lblStartIndicator;

        private ListBox _listDecisions;
        private Button _btnAddDecision;
        private Button _btnEditDecision;
        private Button _btnDeleteDecision;
        private Button _btnMoveUp;
        private Button _btnMoveDown;

        public BlockEditorControl(StoryBlock block, StoryDefinition story, ImageWorkspace images, string workspaceDir)
        {
            _block = block;
            _story = story;
            _images = images;
            _workspaceDir = workspaceDir;
            BuildUi();
        }

        private void BuildUi()
        {
            BackColor = Color.FromArgb(18, 16, 12);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(20)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            root.Controls.Add(BuildFieldsPanel(), 0, 0);
            root.Controls.Add(BuildDecisionsPanel(), 0, 1);

            Controls.Add(root);
        }

        // ------------------------------------------------------------------ //
        //  Top: basic fields
        // ------------------------------------------------------------------ //

        private Control BuildFieldsPanel()
        {
            var panel = new Panel { AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(0, 0, 0, 12) };

            var lblHeader = new Label
            {
                Text = $"Bloc: {_block.Id}",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 195, 120),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            bool isStart = _block.Id == _story.StartBlock;
            _lblStartIndicator = new Label
            {
                Text = isStart ? "⭐ Bloc de start" : "",
                ForeColor = Color.FromArgb(220, 195, 120),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(260, 4)
            };

            _btnMakeStart = new Button
            {
                Text = "Setează ca Start",
                Location = new Point(420, 0),
                Size = new Size(140, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 40, 28),
                ForeColor = Color.FromArgb(210, 190, 130),
                Visible = !isStart
            };
            _btnMakeStart.Click += (s, e) =>
            {
                StartBlockRequested?.Invoke();
                _btnMakeStart.Visible = false;
                _lblStartIndicator.Text = "⭐ Bloc de start";
            };

            var layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                Location = new Point(0, 40)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _txtId               = NewTextBox(_block.Id);
            _txtText             = NewTextBox(_block.Text, multiline: true, height: 90);
            _chkIsFinal          = new CheckBox { Checked = _block.IsFinal, ForeColor = Color.FromArgb(220, 205, 165), AutoSize = true };
            _txtBackgroundImage  = NewTextBox(_block.BackgroundImage);
            _txtBackgroundImage.ReadOnly = true;
            _txtBackgroundImage.Width = 220;
            _txtEventCategory    = NewTextBox(_block.EventCategory);

            AddRow(layout, "Id:", _txtId);
            AddRow(layout, "Text:", _txtText);
            AddRow(layout, "Este final (ending):", _chkIsFinal);
            AddRow(layout, "Imagine fundal:", BuildBackgroundImagePicker());
            AddRow(layout, "Categorie eveniment:", _txtEventCategory);

            _txtId.Leave += (s, e) =>
            {
                string newId = _txtId.Text.Trim();
                if (string.IsNullOrEmpty(newId) || newId == _block.Id) return;

                if (_story.Blocks.Any(b => b.Id == newId))
                {
                    MessageBox.Show($"Există deja un bloc cu id-ul '{newId}'.", "Id duplicat",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtId.Text = _block.Id;
                    return;
                }

                bool wasStart = _story.StartBlock == _block.Id;
                string oldId = _block.Id;
                _block.Id = newId;
                if (wasStart) _story.StartBlock = newId;

                // Fără asta, orice decizie din alt bloc care țintea spre vechiul id
                // rămâne agățată de un id care nu mai există (link mort la Validare).
                foreach (var b in _story.Blocks)
                    foreach (var d in b.Decisions)
                        if (d.TargetBlock == oldId) d.TargetBlock = newId;

                foreach (var p in _story.Properties)
                {
                    if (p.OnMinBlock == oldId) p.OnMinBlock = newId;
                    if (p.OnMaxBlock == oldId) p.OnMaxBlock = newId;
                }

                lblHeader.Text = $"Bloc: {_block.Id}";
                BlockChanged?.Invoke();
            };
            _txtText.TextChanged += (s, e) => { _block.Text = _txtText.Text; BlockChanged?.Invoke(); };
            _chkIsFinal.CheckedChanged += (s, e) =>
            {
                _block.IsFinal = _chkIsFinal.Checked;
                _listDecisions.Enabled = !_block.IsFinal;
                BlockChanged?.Invoke();
            };
            _txtBackgroundImage.TextChanged += (s, e) =>
            {
                _block.BackgroundImage = string.IsNullOrWhiteSpace(_txtBackgroundImage.Text) ? null : _txtBackgroundImage.Text.Trim();
                BlockChanged?.Invoke();
            };
            _txtEventCategory.TextChanged += (s, e) =>
            {
                _block.EventCategory = string.IsNullOrWhiteSpace(_txtEventCategory.Text) ? null : _txtEventCategory.Text.Trim();
                BlockChanged?.Invoke();
            };

            panel.Controls.Add(lblHeader);
            panel.Controls.Add(_lblStartIndicator);
            panel.Controls.Add(_btnMakeStart);
            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildBackgroundImagePicker()
        {
            var row = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };

            var btnPick = NewSmallButton("Alege...");
            var btnClear = NewSmallButton("Șterge");
            _picBackgroundPreview = new PictureBox
            {
                Width = 48,
                Height = 32,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(30, 26, 19),
                Margin = new Padding(6, 0, 0, 0)
            };

            btnPick.Click += (s, e) =>
            {
                using var dlg = new OpenFileDialog { Title = "Alege imaginea de fundal", Filter = ImageWorkspace.ImageFileFilter, InitialDirectory = _images.GetImagesDir(_workspaceDir) };
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    string filename = _images.ImportImage(_workspaceDir, dlg.FileName);
                    _txtBackgroundImage.Text = filename; // declanșează TextChanged-ul existent, care setează _block.BackgroundImage
                    UpdateBackgroundPreview();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Nu am putut importa imaginea:\n{ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnClear.Click += (s, e) =>
            {
                _txtBackgroundImage.Text = "";
                UpdateBackgroundPreview();
            };

            row.Controls.Add(_txtBackgroundImage);
            row.Controls.Add(btnPick);
            row.Controls.Add(btnClear);
            row.Controls.Add(_picBackgroundPreview);

            UpdateBackgroundPreview();
            return row;
        }

        private void UpdateBackgroundPreview()
        {
            _picBackgroundPreview.Image?.Dispose();
            _picBackgroundPreview.Image = null;

            string filename = _txtBackgroundImage.Text;
            if (string.IsNullOrEmpty(filename)) return;

            string path = Path.Combine(_images.GetImagesDir(_workspaceDir), filename);
            if (File.Exists(path))
            {
                try
                {
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                    _picBackgroundPreview.Image = Image.FromStream(fs);
                }
                catch { /* preview e best-effort, nu blocăm editarea */ }
            }
        }

        // ------------------------------------------------------------------ //
        //  Bottom: decisions list
        // ------------------------------------------------------------------ //

        private Control BuildDecisionsPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var lblHeader = new Label
            {
                Text = "Decizii",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 195, 120)
            };

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                FlowDirection = FlowDirection.LeftToRight
            };
            _btnAddDecision    = NewSmallButton("+ Adaugă");
            _btnEditDecision   = NewSmallButton("Editează");
            _btnDeleteDecision = NewSmallButton("Șterge");
            _btnMoveUp         = NewSmallButton("▲");
            _btnMoveDown       = NewSmallButton("▼");
            toolbar.Controls.AddRange(new Control[] { _btnAddDecision, _btnEditDecision, _btnDeleteDecision, _btnMoveUp, _btnMoveDown });

            _listDecisions = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 20, 14),
                ForeColor = Color.FromArgb(215, 200, 160),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                IntegralHeight = false,
                Enabled = !_block.IsFinal
            };
            RefreshDecisionsList();

            _btnAddDecision.Click += (s, e) =>
            {
                var newDecision = new DecisionDefinition { Text = "Decizie nouă" };
                using var dlg = new DecisionEditDialog(newDecision, _story, _images, _workspaceDir);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _block.Decisions.Add(newDecision);
                    RefreshDecisionsList();
                    BlockChanged?.Invoke();
                }
            };

            _btnEditDecision.Click += (s, e) =>
            {
                if (_listDecisions.SelectedItem is not DecisionDefinition decision) return;
                using var dlg = new DecisionEditDialog(decision, _story, _images, _workspaceDir);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshDecisionsList();
                    BlockChanged?.Invoke();
                }
            };

            _btnDeleteDecision.Click += (s, e) =>
            {
                if (_listDecisions.SelectedItem is not DecisionDefinition decision) return;
                if (MessageBox.Show($"Ștergi decizia '{decision.Text}'?", "Confirmare",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

                _block.Decisions.Remove(decision);
                RefreshDecisionsList();
                BlockChanged?.Invoke();
            };

            _btnMoveUp.Click += (s, e) => MoveSelectedDecision(-1);
            _btnMoveDown.Click += (s, e) => MoveSelectedDecision(1);

            _listDecisions.DoubleClick += (s, e) => _btnEditDecision.PerformClick();

            panel.Controls.Add(_listDecisions);
            panel.Controls.Add(toolbar);
            panel.Controls.Add(lblHeader);
            return panel;
        }

        private void MoveSelectedDecision(int direction)
        {
            if (_listDecisions.SelectedItem is not DecisionDefinition decision) return;
            int index = _block.Decisions.IndexOf(decision);
            int newIndex = index + direction;
            if (newIndex < 0 || newIndex >= _block.Decisions.Count) return;

            _block.Decisions.RemoveAt(index);
            _block.Decisions.Insert(newIndex, decision);
            RefreshDecisionsList();
            _listDecisions.SelectedIndex = newIndex;
            BlockChanged?.Invoke();
        }

        private void RefreshDecisionsList()
        {
            _listDecisions.Items.Clear();
            foreach (var decision in _block.Decisions)
                _listDecisions.Items.Add(decision);
            _listDecisions.DisplayMember = ""; // folosim ToString() custom mai jos
        }

        // ------------------------------------------------------------------ //
        //  UI helpers
        // ------------------------------------------------------------------ //

        private void AddRow(TableLayoutPanel layout, string labelText, Control input)
        {
            var lbl = new Label
            {
                Text = labelText,
                ForeColor = Color.FromArgb(190, 175, 140),
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 8, 8)
            };
            input.Margin = new Padding(0, 4, 0, 4);
            layout.RowCount++;
            int row = layout.RowCount - 1;
            layout.Controls.Add(lbl, 0, row);
            layout.Controls.Add(input, 1, row);
        }

        private TextBox NewTextBox(string value, bool multiline = false, int height = 24)
        {
            return new TextBox
            {
                Text = value ?? "",
                Width = 460,
                Multiline = multiline,
                Height = multiline ? height : 24,
                BackColor = Color.FromArgb(30, 26, 19),
                ForeColor = Color.FromArgb(220, 205, 165),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
            };
        }

        private Button NewSmallButton(string text)
        {
            return new Button
            {
                Text = text,
                Size = new Size(86, 28),
                Margin = new Padding(0, 0, 6, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 40, 28),
                ForeColor = Color.FromArgb(210, 190, 130),
                Font = new Font("Segoe UI", 8.5f)
            };
        }
    }
}
