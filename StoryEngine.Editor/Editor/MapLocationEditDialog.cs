using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using StoryEngine.Models;

namespace StoryEngine.Editor
{
    public class MapLocationEditDialog : Form
    {
        private readonly MapLocationDefinition _location;
        private readonly StoryDefinition _story;
        private readonly ImageWorkspace _images;
        private readonly string _workspaceDir;

        private TextBox _txtId;
        private TextBox _txtName;
        private TextBox _txtDescription;
        private ComboBox _cmbTargetBlock;
        private NumericUpDown _numX;
        private NumericUpDown _numY;
        private TextBox _txtIcon;

        private CheckBox _chkCondition;
        private Panel _panelConditionHost;
        private ConditionDefinition _conditionCopy;

        public MapLocationEditDialog(MapLocationDefinition location, StoryDefinition story,
            ImageWorkspace images, string workspaceDir)
        {
            _location = location;
            _story = story;
            _images = images;
            _workspaceDir = workspaceDir;

            _conditionCopy = CloneCondition(location.Condition);

            Text = string.IsNullOrEmpty(location.Id) ? "Adaugă locație hartă" : "Editează locație hartă";
            Width = 760;
            Height = 650;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(18, 16, 12);
            ForeColor = Color.FromArgb(220, 205, 165);

            BuildUi();
            LoadValues();
        }

        private void BuildUi()
        {
            var main = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(18),
                BackColor = Color.FromArgb(18, 16, 12)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _txtId = NewTextBox();
            _txtName = NewTextBox();
            _txtDescription = NewTextBox(multiline: true);

            _cmbTargetBlock = new ComboBox
            {
                Width = 420,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 26, 19),
                ForeColor = Color.FromArgb(220, 205, 165),
                FlatStyle = FlatStyle.Flat
            };

            foreach (var block in _story.Blocks)
                _cmbTargetBlock.Items.Add(block.Id);

            _numX = NewNumber();
            _numY = NewNumber();

            _txtIcon = NewTextBox();
            _txtIcon.ReadOnly = true;

            var iconPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true
            };

            var btnChooseIcon = NewButton("Alege iconiță");
            btnChooseIcon.Click += btnChooseIcon_Click;

            var btnClearIcon = NewButton("Șterge iconiță");
            btnClearIcon.Click += (s, e) => _txtIcon.Text = "";

            iconPanel.Controls.Add(_txtIcon);
            iconPanel.Controls.Add(btnChooseIcon);
            iconPanel.Controls.Add(btnClearIcon);

            AddRow(layout, "Id locație:", _txtId);
            AddRow(layout, "Nume:", _txtName);
            AddRow(layout, "Bloc țintă:", _cmbTargetBlock);
            AddRow(layout, "Descriere:", _txtDescription);
            AddRow(layout, "Poziție X:", _numX);
            AddRow(layout, "Poziție Y:", _numY);
            AddRow(layout, "Iconiță:", iconPanel);

            _chkCondition = new CheckBox
            {
                Text = "Locația are condiție de acces",
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 205, 165),
                Margin = new Padding(0, 16, 0, 8)
            };
            _chkCondition.CheckedChanged += (s, e) => RebuildConditionEditor();

            _panelConditionHost = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Color.FromArgb(24, 20, 14),
                Padding = new Padding(8)
            };

            var conditionTitle = new Label
            {
                Text = "Condiție locație",
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 195, 120),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Margin = new Padding(0, 10, 0, 6)
            };

            var bottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8),
                BackColor = Color.FromArgb(24, 20, 14)
            };

            var btnOk = NewButton("OK");
            btnOk.Width = 100;
            btnOk.Click += btnOk_Click;

            var btnCancel = NewButton("Cancel");
            btnCancel.Width = 100;
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            bottom.Controls.Add(btnOk);
            bottom.Controls.Add(btnCancel);

            main.Controls.Add(_panelConditionHost);
            main.Controls.Add(conditionTitle);
            main.Controls.Add(_chkCondition);
            main.Controls.Add(layout);

            Controls.Add(main);
            Controls.Add(bottom);
        }

        private void LoadValues()
        {
            _txtId.Text = _location.Id ?? "";
            _txtName.Text = _location.Name ?? "";
            _txtDescription.Text = _location.Description ?? "";

            if (!string.IsNullOrEmpty(_location.TargetBlock) && _cmbTargetBlock.Items.Contains(_location.TargetBlock))
                _cmbTargetBlock.SelectedItem = _location.TargetBlock;
            else if (_cmbTargetBlock.Items.Count > 0)
                _cmbTargetBlock.SelectedIndex = 0;

            _numX.Value = Clamp(_location.X, 0, 5000);
            _numY.Value = Clamp(_location.Y, 0, 5000);
            _txtIcon.Text = _location.Icon ?? "";

            _chkCondition.Checked = _conditionCopy != null;
            RebuildConditionEditor();
        }

        private void RebuildConditionEditor()
        {
            _panelConditionHost.Controls.Clear();
            _panelConditionHost.Visible = _chkCondition.Checked;

            if (!_chkCondition.Checked)
                return;

            if (_conditionCopy == null)
            {
                _conditionCopy = new ConditionDefinition
                {
                    Type = "COMPARISON",
                    Property = "day",
                    Operator = ">=",
                    Value = 1
                };
            }

            var editor = new ConditionEditorControl(_conditionCopy, _story, newCondition =>
            {
                _conditionCopy = newCondition;
            });

            editor.Dock = DockStyle.Top;
            _panelConditionHost.Controls.Add(editor);
        }

        private void btnChooseIcon_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Alege iconiță locație",
                Filter = ImageWorkspace.ImageFileFilter
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                _txtIcon.Text = _images.ImportImage(_workspaceDir, dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nu s-a putut importa iconița:\n{ex.Message}", "Eroare",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtId.Text))
            {
                MessageBox.Show("Locația trebuie să aibă un Id.", "Validare",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Locația trebuie să aibă un nume.", "Validare",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_cmbTargetBlock.SelectedItem == null)
            {
                MessageBox.Show("Alege blocul țintă pentru locație.", "Validare",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _location.Id = _txtId.Text.Trim();
            _location.Name = _txtName.Text.Trim();
            _location.TargetBlock = _cmbTargetBlock.SelectedItem as string;
            _location.Description = _txtDescription.Text;
            _location.X = (int)_numX.Value;
            _location.Y = (int)_numY.Value;
            _location.Icon = string.IsNullOrWhiteSpace(_txtIcon.Text) ? null : _txtIcon.Text.Trim();
            _location.Condition = _chkCondition.Checked ? _conditionCopy : null;

            DialogResult = DialogResult.OK;
            Close();
        }

        private TextBox NewTextBox(bool multiline = false)
        {
            return new TextBox
            {
                Width = 420,
                Multiline = multiline,
                Height = multiline ? 70 : 24,
                BackColor = Color.FromArgb(30, 26, 19),
                ForeColor = Color.FromArgb(220, 205, 165),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
        }

        private NumericUpDown NewNumber()
        {
            return new NumericUpDown
            {
                Width = 120,
                Minimum = 0,
                Maximum = 5000,
                BackColor = Color.FromArgb(30, 26, 19),
                ForeColor = Color.FromArgb(220, 205, 165)
            };
        }

        private Button NewButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 40, 28),
                ForeColor = Color.FromArgb(220, 205, 165),
                Margin = new Padding(4)
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(90, 80, 55);
            return btn;
        }

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

        private int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private ConditionDefinition CloneCondition(ConditionDefinition c)
        {
            if (c == null)
                return null;

            return new ConditionDefinition
            {
                Type = c.Type,
                Property = c.Property,
                Operator = c.Operator,
                Value = c.Value,
                Operands = CloneOperands(c.Operands)
            };
        }

        private List<ConditionDefinition> CloneOperands(List<ConditionDefinition> operands)
        {
            if (operands == null)
                return null;

            var copy = new List<ConditionDefinition>();
            foreach (var op in operands)
                copy.Add(CloneCondition(op));

            return copy;
        }
    }
}