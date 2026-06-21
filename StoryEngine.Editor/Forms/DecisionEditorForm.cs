using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using StoryEngine.Editor.Services;
using StoryEngine.Models;

namespace StoryEngine.Editor.Forms
{
    /// <summary>Dialog for adding/editing a single DecisionDefinition.</summary>
    public class DecisionEditorForm : Form
    {
        private readonly TextBox _txtText;
        private readonly ComboBox _cmbTarget;
        private readonly TextBox _txtIcon;
        private readonly PictureBox _picIcon;
        private readonly Label _lblCondition;
        private readonly DataGridView _gridEffects;

        private readonly EditorRepository _repo;
        private readonly string _projectDir;
        private readonly List<string> _propertyKeys;

        private ConditionDefinition? _condition;

        public DecisionDefinition Result { get; private set; } = new DecisionDefinition();

        public DecisionEditorForm(DecisionDefinition existing, List<string> blockIds, List<string> propertyKeys,
            EditorRepository repo, string projectDir)
        {
            _repo = repo;
            _projectDir = projectDir;
            _propertyKeys = propertyKeys;
            _condition = CloneCondition(existing.Condition);

            Text = "Editare decizie";
            Width = 620;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(10) };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Text
            var grpText = new GroupBox { Text = "Text buton", Dock = DockStyle.Fill, Height = 60, Padding = new Padding(8) };
            _txtText = new TextBox { Dock = DockStyle.Top, Text = existing.Text ?? "" };
            grpText.Controls.Add(_txtText);

            // Target
            var grpTarget = new GroupBox { Text = "Bloc destinație (TargetBlock)", Dock = DockStyle.Fill, Height = 60, Padding = new Padding(8) };
            _cmbTarget = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbTarget.Items.AddRange(blockIds.Cast<object>().ToArray());
            if (!string.IsNullOrEmpty(existing.TargetBlock) && blockIds.Contains(existing.TargetBlock))
                _cmbTarget.SelectedItem = existing.TargetBlock;
            grpTarget.Controls.Add(_cmbTarget);

            // Icon
            var grpIcon = new GroupBox { Text = "Iconiță (opțional)", Dock = DockStyle.Fill, Height = 60, Padding = new Padding(8) };
            var pnlIcon = new Panel { Dock = DockStyle.Fill };
            _txtIcon = new TextBox { Left = 0, Top = 4, Width = 300, ReadOnly = true, Text = existing.Icon ?? "" };
            var btnPickIcon = new Button { Left = 308, Top = 2, Width = 80, Text = "Alege..." };
            var btnClearIcon = new Button { Left = 392, Top = 2, Width = 70, Text = "Șterge" };
            _picIcon = new PictureBox { Left = 470, Top = 0, Width = 32, Height = 32, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            btnPickIcon.Click += (s, e) => PickIcon();
            btnClearIcon.Click += (s, e) => { _txtIcon.Text = ""; UpdateIconPreview(); };
            pnlIcon.Controls.Add(_txtIcon);
            pnlIcon.Controls.Add(btnPickIcon);
            pnlIcon.Controls.Add(btnClearIcon);
            pnlIcon.Controls.Add(_picIcon);
            grpIcon.Controls.Add(pnlIcon);

            // Condition
            var grpCondition = new GroupBox { Text = "Condiție de validare (opțional)", Dock = DockStyle.Fill, Height = 70, Padding = new Padding(8) };
            var pnlCondition = new Panel { Dock = DockStyle.Fill };
            _lblCondition = new Label { Left = 0, Top = 6, Width = 380, Height = 30, Text = SummarizeCondition() };
            var btnEditCondition = new Button { Left = 390, Top = 2, Width = 110, Text = "Editează..." };
            var btnClearCondition = new Button { Left = 504, Top = 2, Width = 70, Text = "Șterge" };
            btnEditCondition.Click += (s, e) => EditCondition();
            btnClearCondition.Click += (s, e) => { _condition = null; _lblCondition.Text = SummarizeCondition(); };
            pnlCondition.Controls.Add(_lblCondition);
            pnlCondition.Controls.Add(btnEditCondition);
            pnlCondition.Controls.Add(btnClearCondition);
            grpCondition.Controls.Add(pnlCondition);

            // Effects
            var grpEffects = new GroupBox { Text = "Efecte asupra stării (Effects)", Dock = DockStyle.Fill, Padding = new Padding(8) };
            _gridEffects = BuildEffectsGrid();
            FillEffectsGrid(existing.Effects);
            var pnlEffectsButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 36 };
            var btnAddEffect = new Button { Text = "Adaugă efect", AutoSize = true };
            var btnDelEffect = new Button { Text = "Șterge efect selectat", AutoSize = true };
            btnAddEffect.Click += (s, e) => AddEffectRow();
            btnDelEffect.Click += (s, e) => DeleteSelectedEffectRow();
            pnlEffectsButtons.Controls.Add(btnAddEffect);
            pnlEffectsButtons.Controls.Add(btnDelEffect);
            grpEffects.Controls.Add(_gridEffects);
            grpEffects.Controls.Add(pnlEffectsButtons);

            // OK / Cancel
            var pnlBottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Height = 40 };
            var btnOk = new Button { Text = "OK", Width = 90 };
            var btnCancel = new Button { Text = "Anulează", Width = 90 };
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(btnOk);

            layout.Controls.Add(grpText, 0, 0);
            layout.Controls.Add(grpTarget, 0, 1);
            layout.Controls.Add(grpIcon, 0, 2);
            layout.Controls.Add(grpCondition, 0, 3);
            layout.Controls.Add(grpEffects, 0, 4);
            layout.Controls.Add(pnlBottom, 0, 5);

            Controls.Add(layout);
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            UpdateIconPreview();
        }

        // ── Icon ─────────────────────────────────────────────────────────

        private void PickIcon()
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Alege iconița deciziei",
                Filter = "Imagini (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                InitialDirectory = _repo.GetImagesDir(_projectDir)
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                string filename = _repo.ImportImage(_projectDir, dlg.FileName);
                _txtIcon.Text = filename;
                UpdateIconPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nu am putut importa imaginea:\n{ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateIconPreview()
        {
            _picIcon.Image?.Dispose();
            _picIcon.Image = null;

            if (string.IsNullOrEmpty(_txtIcon.Text)) return;
            string path = Path.Combine(_repo.GetImagesDir(_projectDir), _txtIcon.Text);
            if (File.Exists(path))
            {
                try
                {
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                    _picIcon.Image = Image.FromStream(fs);
                }
                catch { /* preview is best-effort */ }
            }
        }

        // ── Condition ────────────────────────────────────────────────────

        private string SummarizeCondition()
        {
            if (_condition == null) return "(fără condiție — decizia este mereu disponibilă)";
            return SummarizeNode(_condition);
        }

        private static string SummarizeNode(ConditionDefinition c)
        {
            if (c.Type == "COMPARISON") return $"{c.Property} {c.Operator} {c.Value}";
            string inner = string.Join($" {c.Type} ", c.Operands?.Select(SummarizeNode) ?? Enumerable.Empty<string>());
            return $"({inner})";
        }

        private void EditCondition()
        {
            using var dlg = new ConditionEditorForm(_condition, _propertyKeys);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _condition = dlg.Result;
                _lblCondition.Text = SummarizeCondition();
            }
        }

        private static ConditionDefinition? CloneCondition(ConditionDefinition? src)
        {
            if (src == null) return null;
            var clone = new ConditionDefinition { Type = src.Type, Property = src.Property, Operator = src.Operator, Value = src.Value };
            if (src.Operands != null)
                clone.Operands = src.Operands.Select(CloneCondition).Where(c => c != null).Select(c => c!).ToList();
            return clone;
        }

        // ── Effects grid ─────────────────────────────────────────────────

        private DataGridView BuildEffectsGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersWidth = 24,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            var colProp = new DataGridViewComboBoxColumn { Name = "Property", HeaderText = "Proprietate", DataSource = _propertyKeys.ToList() };
            var colType = new DataGridViewComboBoxColumn { Name = "Type", HeaderText = "Tip (ADD/SET)" };
            colType.Items.AddRange("ADD", "SET");
            var colValue = new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "Valoare" };

            grid.Columns.AddRange(colProp, colType, colValue);
            grid.DataError += (s, e) => { e.ThrowException = false; };
            return grid;
        }

        private void FillEffectsGrid(List<EffectDefinition> effects)
        {
            foreach (var eff in effects)
            {
                int idx = _gridEffects.Rows.Add();
                var row = _gridEffects.Rows[idx];
                row.Cells["Property"].Value = eff.Property;
                row.Cells["Type"].Value = eff.Type.ToString();
                row.Cells["Value"].Value = eff.Value.ToString();
            }
        }

        private void AddEffectRow()
        {
            int idx = _gridEffects.Rows.Add();
            var row = _gridEffects.Rows[idx];
            row.Cells["Property"].Value = _propertyKeys.FirstOrDefault();
            row.Cells["Type"].Value = "ADD";
            row.Cells["Value"].Value = "0";
        }

        private void DeleteSelectedEffectRow()
        {
            if (_gridEffects.CurrentRow != null && !_gridEffects.CurrentRow.IsNewRow)
                _gridEffects.Rows.Remove(_gridEffects.CurrentRow);
        }

        // ── OK ───────────────────────────────────────────────────────────

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtText.Text))
            {
                MessageBox.Show("Textul butonului este obligatoriu.", "Date incomplete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_cmbTarget.SelectedItem == null)
            {
                MessageBox.Show("Alege un bloc destinație.", "Date incomplete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var effects = new List<EffectDefinition>();
            foreach (DataGridViewRow row in _gridEffects.Rows)
            {
                if (row.IsNewRow) continue;
                string? prop = row.Cells["Property"].Value as string;
                string? type = row.Cells["Type"].Value as string;
                string? valStr = row.Cells["Value"].Value as string;

                if (string.IsNullOrWhiteSpace(prop)) continue; // skip blank rows

                if (!int.TryParse(valStr, out int val))
                {
                    MessageBox.Show($"Valoarea efectului pentru '{prop}' trebuie să fie un număr întreg.",
                        "Valoare invalidă", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                effects.Add(new EffectDefinition
                {
                    Property = prop,
                    Type = string.Equals(type, "SET", StringComparison.OrdinalIgnoreCase) ? EffectType.SET : EffectType.ADD,
                    Value = val
                });
            }

            Result = new DecisionDefinition
            {
                Text = _txtText.Text.Trim(),
                TargetBlock = (string)_cmbTarget.SelectedItem!,
                Icon = string.IsNullOrWhiteSpace(_txtIcon.Text) ? null : _txtIcon.Text,
                Condition = _condition,
                Effects = effects
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
