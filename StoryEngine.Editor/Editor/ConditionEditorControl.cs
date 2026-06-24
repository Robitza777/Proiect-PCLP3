using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using StoryEngine.Models;

namespace StoryEngine.Editor
{
    /// <summary>
    /// Editor recursiv pentru ConditionDefinition (AST: COMPARISON / AND / OR).
    /// Pentru COMPARISON arată: Property, Operator, Value.
    /// Pentru AND/OR arată: o listă de sub-condiții, fiecare cu propriul
    /// ConditionEditorControl imbricat, plus butoane Add/Remove operand.
    ///
    /// onReplaceRoot: callback apelat când acest control trebuie să-și înlocuiască
    /// întreaga rădăcină (ex: la schimbarea Type din COMPARISON la AND/OR).
    /// </summary>
    public class ConditionEditorControl : UserControl
    {
        private ConditionDefinition _condition;
        private readonly StoryDefinition _story;
        private readonly Action<ConditionDefinition> _onReplaceRoot;

        private ComboBox _cmbType;
        private Panel _panelBody; // conține fie comparația, fie lista de operanzi

        public ConditionEditorControl(ConditionDefinition condition, StoryDefinition story,
                                       Action<ConditionDefinition> onReplaceRoot)
        {
            _condition = condition;
            _story = story;
            _onReplaceRoot = onReplaceRoot;
            BuildUi();
        }

        private void BuildUi()
        {
            BackColor = Color.FromArgb(26, 22, 16);
            Padding = new Padding(8);
            AutoScroll = true;

            var topBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            var lblType = new Label
            {
                Text = "Tip condiție:",
                ForeColor = Color.FromArgb(190, 175, 140),
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0)
            };

            _cmbType = new ComboBox
            {
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(34, 30, 22),
                ForeColor = Color.FromArgb(220, 205, 165),
                FlatStyle = FlatStyle.Flat
            };
            _cmbType.Items.AddRange(new object[] { "COMPARISON", "AND", "OR" });
            _cmbType.SelectedItem = _condition.Type ?? "COMPARISON";
            _cmbType.SelectedIndexChanged += CmbType_SelectedIndexChanged;

            topBar.Controls.Add(lblType);
            topBar.Controls.Add(_cmbType);

            _panelBody = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12, 4, 0, 0)
            };

            Controls.Add(_panelBody);
            Controls.Add(topBar);

            RebuildBody();
        }

        private void CmbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string newType = _cmbType.SelectedItem as string;
            if (newType == _condition.Type) return;

            // Reconstruim condiția păstrând ce se poate la schimbarea tipului
            if (newType == "COMPARISON")
            {
                _condition = new ConditionDefinition
                {
                    Type = "COMPARISON",
                    Property = _story.Properties.Count > 0 ? _story.Properties[0].Key : "",
                    Operator = ">=",
                    Value = 0
                };
            }
            else // AND / OR
            {
                _condition = new ConditionDefinition
                {
                    Type = newType,
                    Operands = new List<ConditionDefinition>
                    {
                        new ConditionDefinition
                        {
                            Type = "COMPARISON",
                            Property = _story.Properties.Count > 0 ? _story.Properties[0].Key : "",
                            Operator = ">=",
                            Value = 0
                        }
                    }
                };
            }

            _onReplaceRoot(_condition);
            RebuildBody();
        }

        private void RebuildBody()
        {
            _panelBody.Controls.Clear();

            if (_condition.Type == "COMPARISON")
                BuildComparisonBody();
            else
                BuildOperandsBody();
        }

        // ------------------------------------------------------------------ //
        //  COMPARISON: Property / Operator / Value
        // ------------------------------------------------------------------ //

        private void BuildComparisonBody()
        {
            var row = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true
            };

            var cmbProperty = new ComboBox
            {
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(34, 30, 22),
                ForeColor = Color.FromArgb(220, 205, 165),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 6, 0)
            };
            cmbProperty.Items.Add("day");

            foreach (var prop in _story.Properties)
                cmbProperty.Items.Add(prop.Key);
            if (cmbProperty.Items.Contains(_condition.Property))
                cmbProperty.SelectedItem = _condition.Property;

            var cmbOperator = new ComboBox
            {
                Width = 70,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(34, 30, 22),
                ForeColor = Color.FromArgb(220, 205, 165),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 6, 0)
            };
            cmbOperator.Items.AddRange(new object[] { "<", "<=", ">", ">=", "==", "!=" });
            cmbOperator.SelectedItem = string.IsNullOrEmpty(_condition.Operator) ? "==" : _condition.Operator;

            var numValue = new NumericUpDown
            {
                Width = 90,
                Minimum = -100000,
                Maximum = 100000,
                Value = _condition.Value,
                BackColor = Color.FromArgb(34, 30, 22),
                ForeColor = Color.FromArgb(220, 205, 165)
            };

            cmbProperty.SelectedIndexChanged += (s, e) => _condition.Property = cmbProperty.SelectedItem as string;
            cmbOperator.SelectedIndexChanged += (s, e) => _condition.Operator = cmbOperator.SelectedItem as string;
            numValue.ValueChanged += (s, e) => _condition.Value = (int)numValue.Value;

            row.Controls.Add(cmbProperty);
            row.Controls.Add(cmbOperator);
            row.Controls.Add(numValue);

            _panelBody.Controls.Add(row);
        }

        // ------------------------------------------------------------------ //
        //  AND / OR: list of nested operands
        // ------------------------------------------------------------------ //

        private void BuildOperandsBody()
        {
            _condition.Operands ??= new List<ConditionDefinition>();

            var container = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false
            };

            for (int i = 0; i < _condition.Operands.Count; i++)
            {
                int index = i; // captură locală pentru closure
                var operand = _condition.Operands[index];

                var operandRow = new Panel
                {
                    AutoSize = true,
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(0, 0, 0, 6),
                    BackColor = Color.FromArgb(22, 19, 14)
                };

                var nestedEditor = new ConditionEditorControl(operand, _story, (newOperand) =>
                {
                    _condition.Operands[index] = newOperand;
                })
                { Location = new Point(0, 0), AutoSize = true };

                var btnRemove = new Button
                {
                    Text = "✕",
                    Size = new Size(28, 28),
                    Location = new Point(0, 0),
                    Dock = DockStyle.Right,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(60, 30, 25),
                    ForeColor = Color.FromArgb(220, 180, 170),
                    Visible = _condition.Operands.Count > 1 // nu permitem 0 operanzi
                };
                btnRemove.Click += (s, e) =>
                {
                    _condition.Operands.RemoveAt(index);
                    RebuildBody();
                };

                operandRow.Controls.Add(nestedEditor);
                operandRow.Controls.Add(btnRemove);
                container.Controls.Add(operandRow);
            }

            var btnAddOperand = new Button
            {
                Text = $"+ Adaugă condiție în {_condition.Type}",
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 40, 28),
                ForeColor = Color.FromArgb(210, 190, 130),
                Margin = new Padding(0, 4, 0, 0)
            };
            btnAddOperand.Click += (s, e) =>
            {
                _condition.Operands.Add(new ConditionDefinition
                {
                    Type = "COMPARISON",
                    Property = _story.Properties.Count > 0 ? _story.Properties[0].Key : "",
                    Operator = ">=",
                    Value = 0
                });
                RebuildBody();
            };

            container.Controls.Add(btnAddOperand);
            _panelBody.Controls.Add(container);
        }
    }
}
