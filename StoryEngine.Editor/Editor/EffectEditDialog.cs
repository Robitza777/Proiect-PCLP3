using System;
using System.Drawing;
using System.Windows.Forms;
using StoryEngine.Models;

namespace StoryEngine.Editor
{
    /// <summary>Dialog mic pentru editarea unui EffectDefinition (Property / Type / Value).</summary>
    public class EffectEditDialog : Form
    {
        private readonly EffectDefinition _effect;
        private readonly StoryDefinition _story;

        private ComboBox _cmbProperty;
        private ComboBox _cmbType;
        private NumericUpDown _numValue;

        public EffectEditDialog(EffectDefinition effect, StoryDefinition story)
        {
            _effect = effect;
            _story = story;
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Editare efect";
            Size = new Size(360, 230);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(22, 19, 14);
            ForeColor = Color.FromArgb(210, 195, 160);

            var layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                Location = new Point(16, 16)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _cmbProperty = new ComboBox
            {
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 26, 19),
                ForeColor = Color.FromArgb(220, 205, 165),
                FlatStyle = FlatStyle.Flat
            };
            foreach (var prop in _story.Properties)
                _cmbProperty.Items.Add(prop.Key);
            if (_cmbProperty.Items.Contains(_effect.Property))
                _cmbProperty.SelectedItem = _effect.Property;

            _cmbType = new ComboBox
            {
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 26, 19),
                ForeColor = Color.FromArgb(220, 205, 165),
                FlatStyle = FlatStyle.Flat
            };
            _cmbType.Items.AddRange(new object[] { EffectType.ADD, EffectType.SET });
            _cmbType.SelectedItem = _effect.Type;

            _numValue = new NumericUpDown
            {
                Minimum = -100000,
                Maximum = 100000,
                Value = _effect.Value,
                Width = 200,
                BackColor = Color.FromArgb(30, 26, 19),
                ForeColor = Color.FromArgb(220, 205, 165)
            };

            AddRow(layout, "Proprietate:", _cmbProperty);
            AddRow(layout, "Tip:", _cmbType);
            AddRow(layout, "Valoare:", _numValue);

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(90, 32),
                Location = new Point(16, layout.Bottom + 16),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 90, 50),
                ForeColor = Color.FromArgb(220, 240, 200)
            };
            var btnCancel = new Button
            {
                Text = "Anulează",
                DialogResult = DialogResult.Cancel,
                Size = new Size(90, 32),
                Location = new Point(116, layout.Bottom + 16),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 40, 30),
                ForeColor = Color.FromArgb(210, 195, 160)
            };
            btnOk.Click += (s, e) =>
            {
                _effect.Property = _cmbProperty.SelectedItem as string;
                _effect.Type = (EffectType)_cmbType.SelectedItem;
                _effect.Value = (int)_numValue.Value;
            };

            Controls.Add(layout);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            CancelButton = btnCancel;
        }

        private void AddRow(TableLayoutPanel layout, string labelText, Control input)
        {
            var lbl = new Label
            {
                Text = labelText,
                ForeColor = Color.FromArgb(190, 175, 140),
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
    }
}
