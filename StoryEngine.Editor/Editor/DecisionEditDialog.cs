using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using StoryEngine.Models;

namespace StoryEngine.Editor
{
    /// <summary>
    /// Dialog modal pentru editarea unei DecisionDefinition: text, target, icon,
    /// listă de efecte (Add/Edit/Delete) și condiție (via ConditionEditorControl).
    /// La OK, modifică obiectul `decision` direct (pass-by-reference pe clasă).
    ///
    /// NOTĂ DE DESIGN: tot conținutul stă într-un FlowLayoutPanel vertical
    /// (TopDown, WrapContents = false). Fiecare "secțiune" e un control cu
    /// AutoSize = true adăugat ca rând în flow. Asta elimină complet nevoia
    /// de a calcula Location pe baza lui .Bottom al elementului anterior —
    /// sursa suprapunerilor din versiunea precedentă, unde layout-ul unui
    /// TableLayoutPanel cu AutoSize nu era încă recalculat în momentul în
    /// care citeam .Bottom pentru elementul următor.
    /// </summary>
    public class DecisionEditDialog : Form
    {
        private readonly DecisionDefinition _decision;
        private readonly StoryDefinition _story;
        private readonly ImageWorkspace _images;
        private readonly string _workspaceDir;

        private TextBox _txtText;
        private TextBox _txtResultText;
        private ComboBox _cmbTargetBlock;
        private TextBox _txtIcon;
        private PictureBox _picIconPreview;

        private ListBox _listEffects;
        private Button _btnAddEffect;
        private Button _btnEditEffect;
        private Button _btnDeleteEffect;

        private CheckBox _chkHasCondition;
        private ConditionEditorControl _conditionEditor;
        private Panel _panelConditionHost;

        private FlowLayoutPanel _rootFlow;

        private const int ContentWidth = 500;

        public DecisionEditDialog(DecisionDefinition decision, StoryDefinition story,
                                   ImageWorkspace images, string workspaceDir)
        {
            _decision = decision;
            _story = story;
            _images = images;
            _workspaceDir = workspaceDir;
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Editare decizie";
            Size = new Size(580, 680);
            MinimumSize = new Size(520, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            BackColor = Color.FromArgb(22, 19, 14);
            ForeColor = Color.FromArgb(210, 195, 160);

            // ── Container principal: scroll + flow vertical ─────────────────
            var scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16) };

            _rootFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Width = ContentWidth + 20
            };

            // ── Câmpuri de bază (fiecare rând e propriul FlowLayoutPanel orizontal) ──
            _txtText = NewTextBox(_decision.Text, multiline: true, height: 50);
            _txtResultText = NewTextBox(_decision.ResultText, multiline: true, height: 50);
            _cmbTargetBlock = NewBlockCombo(_decision.TargetBlock);
            _txtIcon = NewTextBox(_decision.Icon);
            _txtIcon.ReadOnly = true;
            _txtIcon.Width = 180;

            _rootFlow.Controls.Add(MakeFieldRow("Text buton:", _txtText));
            _rootFlow.Controls.Add(MakeFieldRow("Text rezultat (opțional):", _txtResultText));
            _rootFlow.Controls.Add(MakeFieldRow("Bloc țintă:", _cmbTargetBlock));
            _rootFlow.Controls.Add(MakeFieldRow("Iconiță (opțional):", BuildIconPicker()));

            // ── Separator vizual ──────────────────────────────────────────
            _rootFlow.Controls.Add(MakeSeparator());

            // ── Effects ───────────────────────────────────────────────────
            var lblEffects = new Label
            {
                Text = "Efecte",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 195, 120),
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4)
            };
            _rootFlow.Controls.Add(lblEffects);

            var effectsToolbar = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 4)
            };
            _btnAddEffect = NewSmallButton("+ Adaugă");
            _btnEditEffect = NewSmallButton("Editează");
            _btnDeleteEffect = NewSmallButton("Șterge");
            effectsToolbar.Controls.AddRange(new Control[] { _btnAddEffect, _btnEditEffect, _btnDeleteEffect });
            _rootFlow.Controls.Add(effectsToolbar);

            _listEffects = new ListBox
            {
                Width = ContentWidth,
                Height = 100,
                BackColor = Color.FromArgb(24, 20, 14),
                ForeColor = Color.FromArgb(215, 200, 160),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, 0, 0, 12)
            };
            RefreshEffectsList();
            _rootFlow.Controls.Add(_listEffects);

            _btnAddEffect.Click += (s, e) =>
            {
                var effect = new EffectDefinition { Property = _story.Properties.FirstOrDefault()?.Key ?? "", Type = EffectType.ADD, Value = 0 };
                using var dlg = new EffectEditDialog(effect, _story);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _decision.Effects.Add(effect);
                    RefreshEffectsList();
                }
            };
            _btnEditEffect.Click += (s, e) =>
            {
                if (_listEffects.SelectedItem is not EffectDefinition effect) return;
                using var dlg = new EffectEditDialog(effect, _story);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    RefreshEffectsList();
            };
            _btnDeleteEffect.Click += (s, e) =>
            {
                if (_listEffects.SelectedItem is not EffectDefinition effect) return;
                _decision.Effects.Remove(effect);
                RefreshEffectsList();
            };

            // ── Separator vizual ──────────────────────────────────────────
            _rootFlow.Controls.Add(MakeSeparator());

            // ── Condition ─────────────────────────────────────────────────
            _chkHasCondition = new CheckBox
            {
                Text = "Această decizie are o condiție",
                Checked = _decision.Condition != null,
                ForeColor = Color.FromArgb(220, 205, 165),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            _rootFlow.Controls.Add(_chkHasCondition);

            _panelConditionHost = new Panel
            {
                Width = ContentWidth,
                Height = 220,
                BackColor = Color.FromArgb(24, 20, 14),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = _decision.Condition != null,
                Margin = new Padding(0, 0, 0, 16),
                AutoScroll = true
            };
            _rootFlow.Controls.Add(_panelConditionHost);

            RebuildConditionEditor();

            _chkHasCondition.CheckedChanged += (s, e) =>
            {
                if (_chkHasCondition.Checked)
                {
                    _decision.Condition ??= new ConditionDefinition
                    {
                        Type = "COMPARISON",
                        Property = _story.Properties.FirstOrDefault()?.Key ?? "",
                        Operator = ">=",
                        Value = 0
                    };
                }
                else
                {
                    _decision.Condition = null;
                }
                _panelConditionHost.Visible = _chkHasCondition.Checked;
                RebuildConditionEditor();
            };

            // ── Buttons (rând final, separat de flow-ul de scroll) ──────────
            var buttonsRow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 8, 0, 0)
            };
            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(90, 32),
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 90, 50),
                ForeColor = Color.FromArgb(220, 240, 200)
            };
            var btnCancel = new Button
            {
                Text = "Anulează",
                DialogResult = DialogResult.Cancel,
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 40, 30),
                ForeColor = Color.FromArgb(210, 195, 160)
            };
            buttonsRow.Controls.Add(btnOk);
            buttonsRow.Controls.Add(btnCancel);
            _rootFlow.Controls.Add(buttonsRow);

            btnOk.Click += (s, e) => CommitFields();

            scrollPanel.Controls.Add(_rootFlow);
            Controls.Add(scrollPanel);

            AcceptButton = null; // evităm submit accidental din TextBox multiline
            CancelButton = btnCancel;
        }

        // ------------------------------------------------------------------ //
        //  Helpers de layout: un "rând" e mereu un FlowLayoutPanel orizontal
        //  cu AutoSize=true, deci înălțimea lui reală e mereu cea corectă —
        //  nu depinde de citirea unui .Bottom calculat prea devreme.
        // ------------------------------------------------------------------ //

        private Control MakeFieldRow(string labelText, Control input)
        {
            var row = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 8)
            };

            var lbl = new Label
            {
                Text = labelText,
                ForeColor = Color.FromArgb(190, 175, 140),
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = false,
                Width = 130,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.TopLeft,
                Margin = new Padding(0, 6, 8, 0)
            };
            input.Margin = new Padding(0);

            row.Controls.Add(lbl);
            row.Controls.Add(input);
            return row;
        }

        private Control MakeSeparator()
        {
            return new Panel
            {
                Width = ContentWidth,
                Height = 1,
                BackColor = Color.FromArgb(60, 53, 38),
                Margin = new Padding(0, 8, 0, 12)
            };
        }

        // ------------------------------------------------------------------ //
        //  Icon picker
        // ------------------------------------------------------------------ //

        private Control BuildIconPicker()
        {
            var row = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };

            var btnPick = NewSmallButton("Alege...");
            var btnClear = NewSmallButton("Șterge");
            _picIconPreview = new PictureBox
            {
                Width = 32,
                Height = 32,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(30, 26, 19),
                Margin = new Padding(6, 0, 0, 0)
            };

            btnPick.Click += (s, e) =>
            {
                using var dlg = new OpenFileDialog { Title = "Alege iconița deciziei", Filter = ImageWorkspace.ImageFileFilter, InitialDirectory = _images.GetImagesDir(_workspaceDir) };
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    string filename = _images.ImportImage(_workspaceDir, dlg.FileName);
                    _txtIcon.Text = filename;
                    UpdateIconPreview();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Nu am putut importa imaginea:\n{ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnClear.Click += (s, e) => { _txtIcon.Text = ""; UpdateIconPreview(); };

            row.Controls.Add(_txtIcon);
            row.Controls.Add(btnPick);
            row.Controls.Add(btnClear);
            row.Controls.Add(_picIconPreview);

            UpdateIconPreview();
            return row;
        }

        private void UpdateIconPreview()
        {
            _picIconPreview.Image?.Dispose();
            _picIconPreview.Image = null;

            string filename = _txtIcon.Text;
            if (string.IsNullOrEmpty(filename)) return;

            string path = Path.Combine(_images.GetImagesDir(_workspaceDir), filename);
            if (File.Exists(path))
            {
                try
                {
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                    _picIconPreview.Image = Image.FromStream(fs);
                }
                catch { /* preview e best-effort */ }
            }
        }

        // ------------------------------------------------------------------ //
        //  Condition editor host
        // ------------------------------------------------------------------ //

        private void RebuildConditionEditor()
        {
            _panelConditionHost.Controls.Clear();
            if (_decision.Condition == null) return;

            _conditionEditor = new ConditionEditorControl(_decision.Condition, _story, (newRoot) =>
            {
                _decision.Condition = newRoot;
            })
            { Dock = DockStyle.Top, AutoSize = true };
            _panelConditionHost.Controls.Add(_conditionEditor);
        }

        // ------------------------------------------------------------------ //
        //  Commit + effects list refresh
        // ------------------------------------------------------------------ //

        private void CommitFields()
        {
            _decision.Text = _txtText.Text;
            _decision.ResultText = string.IsNullOrWhiteSpace(_txtResultText.Text) ? null : _txtResultText.Text;
            _decision.TargetBlock = _cmbTargetBlock.SelectedItem as string;
            _decision.Icon = string.IsNullOrWhiteSpace(_txtIcon.Text) ? null : _txtIcon.Text.Trim();
        }

        private void RefreshEffectsList()
        {
            _listEffects.Items.Clear();
            foreach (var effect in _decision.Effects)
                _listEffects.Items.Add(effect);
        }

        // ------------------------------------------------------------------ //
        //  UI control factories
        // ------------------------------------------------------------------ //

        private TextBox NewTextBox(string value, bool multiline = false, int height = 24)
        {
            return new TextBox
            {
                Text = value ?? "",
                Width = ContentWidth - 138, // alocă spațiu corect lângă label-ul de 130px + margin
                Multiline = multiline,
                Height = multiline ? height : 24,
                BackColor = Color.FromArgb(30, 26, 19),
                ForeColor = Color.FromArgb(220, 205, 165),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
            };
        }

        private ComboBox NewBlockCombo(string selected)
        {
            var combo = new ComboBox
            {
                Width = ContentWidth - 138,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 26, 19),
                ForeColor = Color.FromArgb(220, 205, 165),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f)
            };
            foreach (var block in _story.Blocks)
                combo.Items.Add(block.Id);

            if (!string.IsNullOrEmpty(selected) && combo.Items.Contains(selected))
                combo.SelectedItem = selected;

            return combo;
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