using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using StoryEngine.Models;

namespace StoryEngine.Editor
{
    /// <summary>
    /// Editor pentru o singură StatePropertyDefinition (resursă: Food, Health, item.*, etc).
    /// </summary>
    public class PropertyEditorControl : UserControl
    {
        public event Action PropertyChanged;

        private readonly StatePropertyDefinition _prop;
        private readonly List<StoryBlock> _blocks;

        private TextBox _txtKey;
        private TextBox _txtDisplayName;
        private NumericUpDown _numMin;
        private NumericUpDown _numMax;
        private NumericUpDown _numInitial;
        private CheckBox _chkShowInHud;
        private TextBox _txtHudIcon;
        private ComboBox _cmbOnMinBlock;
        private ComboBox _cmbOnMaxBlock;

        public PropertyEditorControl(StatePropertyDefinition prop, List<StoryBlock> blocks)
        {
            _prop = prop;
            _blocks = blocks;
            BuildUi();
        }

        private void BuildUi()
        {
            BackColor = Color.FromArgb(18, 16, 12);

            var lblHeader = new Label
            {
                Text = $"Proprietate: {_prop.Key}",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 195, 120),
                AutoSize = true,
                Location = new Point(20, 16)
            };

            var layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                Location = new Point(20, 56)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _txtKey         = NewTextBox(_prop.Key);
            _txtDisplayName = NewTextBox(_prop.DisplayName);
            _numMin         = NewNumeric(_prop.Min, -100000, 100000);
            _numMax         = NewNumeric(_prop.Max, -100000, 100000);
            _numInitial     = NewNumeric(_prop.Initial, -100000, 100000);
            _chkShowInHud   = NewCheckBox(_prop.ShowInHud);
            _txtHudIcon     = NewTextBox(_prop.HudIcon);
            _cmbOnMinBlock  = NewBlockCombo(_prop.OnMinBlock);
            _cmbOnMaxBlock  = NewBlockCombo(_prop.OnMaxBlock);

            AddRow(layout, "Key (id intern):", _txtKey);
            AddRow(layout, "Nume afișat:", _txtDisplayName);
            AddRow(layout, "Min:", _numMin);
            AddRow(layout, "Max:", _numMax);
            AddRow(layout, "Valoare inițială:", _numInitial);
            AddRow(layout, "Arată în HUD:", _chkShowInHud);
            AddRow(layout, "Iconiță (ex: food.png):", _txtHudIcon);
            AddRow(layout, "Redirect la Min:", _cmbOnMinBlock);
            AddRow(layout, "Redirect la Max:", _cmbOnMaxBlock);

            var lblHint = new Label
            {
                Text = "Sugestie: proprietățile-obiect (inventar) folosesc Min=0, Max=1\nși cheia prefixată cu \"item.\" (ex: item.lantern).",
                ForeColor = Color.FromArgb(120, 110, 80),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                AutoSize = true,
                Location = new Point(20, layout.Bottom + 76)
            };

            WireEvents();

            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            panel.Controls.Add(lblHeader);
            panel.Controls.Add(layout);
            panel.Controls.Add(lblHint);
            Controls.Add(panel);
        }

        private void WireEvents()
        {
            _txtKey.Leave += (s, e) =>
            {
                string newKey = _txtKey.Text.Trim();
                if (string.IsNullOrEmpty(newKey) || newKey == _prop.Key) return;

                string oldKey = _prop.Key;
                _prop.Key = newKey;
                RenamePropertyKeyReferences(oldKey, newKey);
                PropertyChanged?.Invoke();
            };
            _txtDisplayName.TextChanged += (s, e) => { _prop.DisplayName = _txtDisplayName.Text; PropertyChanged?.Invoke(); };
            _numMin.ValueChanged += (s, e) => { _prop.Min = (int)_numMin.Value; PropertyChanged?.Invoke(); };
            _numMax.ValueChanged += (s, e) => { _prop.Max = (int)_numMax.Value; PropertyChanged?.Invoke(); };
            _numInitial.ValueChanged += (s, e) => { _prop.Initial = (int)_numInitial.Value; PropertyChanged?.Invoke(); };
            _chkShowInHud.CheckedChanged += (s, e) => { _prop.ShowInHud = _chkShowInHud.Checked; PropertyChanged?.Invoke(); };
            _txtHudIcon.TextChanged += (s, e) => { _prop.HudIcon = string.IsNullOrWhiteSpace(_txtHudIcon.Text) ? null : _txtHudIcon.Text.Trim(); PropertyChanged?.Invoke(); };
            _cmbOnMinBlock.SelectedIndexChanged += (s, e) =>
            {
                string sel = _cmbOnMinBlock.SelectedItem as string;
                _prop.OnMinBlock = (sel == "(niciunul)") ? null : sel;
                PropertyChanged?.Invoke();
            };
            _cmbOnMaxBlock.SelectedIndexChanged += (s, e) =>
            {
                string sel = _cmbOnMaxBlock.SelectedItem as string;
                _prop.OnMaxBlock = (sel == "(niciunul)") ? null : sel;
                PropertyChanged?.Invoke();
            };
        }

        // ── Cascadă la redenumirea unei chei (analog cu redenumirea unui bloc) ──

        /// <summary>
        /// La redenumirea unei proprietăți, toate Effects și Conditions din toate
        /// deciziile tuturor blocurilor care refereau cheia veche trebuie actualizate,
        /// altfel rămân "agățate" de o cheie care nu mai există (Validate le-ar semnala
        /// ca erori, fără ca utilizatorul să înțeleagă de ce).
        /// </summary>
        private void RenamePropertyKeyReferences(string oldKey, string newKey)
        {
            foreach (var block in _blocks)
            {
                foreach (var decision in block.Decisions)
                {
                    foreach (var effect in decision.Effects)
                        if (effect.Property == oldKey) effect.Property = newKey;

                    RenameInCondition(decision.Condition, oldKey, newKey);
                }
            }
        }

        private static void RenameInCondition(ConditionDefinition condition, string oldKey, string newKey)
        {
            if (condition == null) return;

            if (condition.Type == "COMPARISON")
            {
                if (condition.Property == oldKey) condition.Property = newKey;
                return;
            }

            if (condition.Operands == null) return;
            foreach (var operand in condition.Operands)
                RenameInCondition(operand, oldKey, newKey);
        }

        // ── Helpers UI ───────────────────────────────────────────────────

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

        private TextBox NewTextBox(string value)
        {
            return new TextBox
            {
                Text = value ?? "",
                Width = 320,
                BackColor = Color.FromArgb(30, 26, 19),
                ForeColor = Color.FromArgb(220, 205, 165),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
        }

        private NumericUpDown NewNumeric(int value, int min, int max)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = Math.Max(min, Math.Min(max, value)),
                Width = 120,
                BackColor = Color.FromArgb(30, 26, 19),
                ForeColor = Color.FromArgb(220, 205, 165),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
        }

        private CheckBox NewCheckBox(bool value)
        {
            return new CheckBox
            {
                Checked = value,
                ForeColor = Color.FromArgb(220, 205, 165),
                AutoSize = true
            };
        }

        private ComboBox NewBlockCombo(string selectedBlockId)
        {
            var combo = new ComboBox
            {
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 26, 19),
                ForeColor = Color.FromArgb(220, 205, 165),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f)
            };
            combo.Items.Add("(niciunul)");
            foreach (var block in _blocks)
                combo.Items.Add(block.Id);

            combo.SelectedItem = !string.IsNullOrEmpty(selectedBlockId) && combo.Items.Contains(selectedBlockId)
                ? selectedBlockId
                : "(niciunul)";

            return combo;
        }
    }
}
