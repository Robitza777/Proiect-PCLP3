using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using StoryEngine.Models;

namespace StoryEngine.Editor.Forms
{
    /// <summary>
    /// Pedagogic tree editor for ConditionDefinition (as recommended in the spec, §14.3):
    /// the author picks a node type (COMPARISON / AND / OR) instead of typing free text.
    /// </summary>
    public class ConditionEditorForm : Form
    {
        private readonly TreeView _tree;
        private readonly ComboBox _cmbNewType;
        private readonly GroupBox _grpLeaf;
        private readonly ComboBox _cmbProperty;
        private readonly ComboBox _cmbOperator;
        private readonly TextBox _txtValue;

        private readonly List<string> _propertyKeys;
        private ConditionDefinition? _root;
        private readonly Dictionary<ConditionDefinition, ConditionDefinition?> _parentOf = new();
        private readonly Dictionary<TreeNode, ConditionDefinition> _nodeOf = new();

        /// <summary>Null means "no condition" (the decision is always available).</summary>
        public ConditionDefinition? Result { get; private set; }

        public ConditionEditorForm(ConditionDefinition? existing, List<string> propertyKeys)
        {
            _propertyKeys = propertyKeys;
            _root = CloneOrNull(existing);

            Text = "Editor condiție";
            Width = 720;
            Height = 520;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;

            // ── top toolbar: add / delete node ──────────────────────────
            var pnlTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8, 8, 8, 4) };
            _cmbNewType = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbNewType.Items.AddRange(new object[] { "COMPARISON", "AND", "OR" });
            _cmbNewType.SelectedIndex = 0;
            var btnAdd = new Button { Text = "Adaugă nod", AutoSize = true };
            var btnDelete = new Button { Text = "Șterge nod selectat", AutoSize = true };
            btnAdd.Click += (s, e) => AddNode();
            btnDelete.Click += (s, e) => DeleteSelectedNode();
            pnlTop.Controls.Add(new Label { Text = "Tip nod nou:", AutoSize = true, Padding = new Padding(0, 6, 4, 0) });
            pnlTop.Controls.Add(_cmbNewType);
            pnlTop.Controls.Add(btnAdd);
            pnlTop.Controls.Add(btnDelete);

            // ── bottom: OK / Cancel ──────────────────────────────────────
            var pnlBottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            var btnOk = new Button { Text = "OK", Width = 90 };
            var btnCancel = new Button { Text = "Anulează", Width = 90 };
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(btnOk);

            // ── center: tree (left) + leaf editor (right) ────────────────
            var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 380 };

            _tree = new TreeView { Dock = DockStyle.Fill, HideSelection = false };
            _tree.AfterSelect += (s, e) => RefreshLeafPanel();
            split.Panel1.Controls.Add(_tree);

            _grpLeaf = new GroupBox { Dock = DockStyle.Top, Height = 220, Text = "Comparație (nod COMPARISON)", Padding = new Padding(8) };
            var lblProp = new Label { Left = 10, Top = 28, Width = 100, Text = "Proprietate:" };
            _cmbProperty = new ComboBox { Left = 10, Top = 50, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbProperty.Items.AddRange(_propertyKeys.Cast<object>().ToArray());

            var lblOp = new Label { Left = 10, Top = 84, Width = 100, Text = "Operator:" };
            _cmbOperator = new ComboBox { Left = 10, Top = 106, Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbOperator.Items.AddRange(new object[] { "<", "<=", ">", ">=", "==", "!=" });

            var lblVal = new Label { Left = 10, Top = 140, Width = 100, Text = "Valoare:" };
            _txtValue = new TextBox { Left = 10, Top = 162, Width = 100 };

            var btnApply = new Button { Left = 10, Top = 195, Width = 120, Text = "Aplică pe nod" };
            btnApply.Click += (s, e) => ApplyLeafEdit();

            _grpLeaf.Controls.Add(lblProp);
            _grpLeaf.Controls.Add(_cmbProperty);
            _grpLeaf.Controls.Add(lblOp);
            _grpLeaf.Controls.Add(_cmbOperator);
            _grpLeaf.Controls.Add(lblVal);
            _grpLeaf.Controls.Add(_txtValue);
            _grpLeaf.Controls.Add(btnApply);

            var lblHint = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(8),
                Text = "Selectează un nod AND/OR și apasă „Adaugă nod” pentru a-i adăuga sub-condiții.\n" +
                       "Selectează un nod COMPARISON pentru a-i edita proprietatea/operatorul/valoarea în panoul de sus."
            };

            var rightPanel = new Panel { Dock = DockStyle.Fill };
            rightPanel.Controls.Add(lblHint);
            rightPanel.Controls.Add(_grpLeaf);
            split.Panel2.Controls.Add(rightPanel);

            Controls.Add(split);
            Controls.Add(pnlBottom);
            Controls.Add(pnlTop);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            RebuildTree();
            RefreshLeafPanel();
        }

        private static ConditionDefinition? CloneOrNull(ConditionDefinition? src)
        {
            if (src == null) return null;
            var clone = new ConditionDefinition { Type = src.Type, Property = src.Property, Operator = src.Operator, Value = src.Value };
            if (src.Operands != null)
                clone.Operands = src.Operands.Select(CloneOrNull).Where(c => c != null).Select(c => c!).ToList();
            return clone;
        }

        // ── Tree building ─────────────────────────────────────────────────

        private void RebuildTree(ConditionDefinition? selectAfter = null)
        {
            _tree.Nodes.Clear();
            _nodeOf.Clear();
            _parentOf.Clear();

            if (_root == null)
            {
                _tree.Nodes.Add(new TreeNode("(nicio condiție — decizia este mereu disponibilă)"));
                return;
            }

            ComputeParents(_root, null);
            var rootNode = BuildTreeNode(_root);
            _tree.Nodes.Add(rootNode);
            _tree.ExpandAll();

            if (selectAfter != null)
            {
                var match = _nodeOf.FirstOrDefault(kv => kv.Value == selectAfter).Key;
                if (match != null) _tree.SelectedNode = match;
            }
            else
            {
                _tree.SelectedNode = rootNode;
            }
        }

        private void ComputeParents(ConditionDefinition node, ConditionDefinition? parent)
        {
            _parentOf[node] = parent;
            if (node.Operands != null)
                foreach (var child in node.Operands)
                    ComputeParents(child, node);
        }

        private TreeNode BuildTreeNode(ConditionDefinition cond)
        {
            var node = new TreeNode(Summarize(cond)) { Tag = cond };
            _nodeOf[node] = cond;

            if (cond.Operands != null)
                foreach (var child in cond.Operands)
                    node.Nodes.Add(BuildTreeNode(child));

            return node;
        }

        private static string Summarize(ConditionDefinition cond)
        {
            if (cond.Type == "COMPARISON")
            {
                string prop = string.IsNullOrEmpty(cond.Property) ? "<proprietate?>" : cond.Property;
                string op = string.IsNullOrEmpty(cond.Operator) ? "?" : cond.Operator;
                return $"{prop} {op} {cond.Value}";
            }
            int childCount = cond.Operands?.Count ?? 0;
            return $"{cond.Type}  ({childCount} sub-condiții)";
        }

        // ── Add / delete ─────────────────────────────────────────────────

        private void AddNode()
        {
            string newType = (string)_cmbNewType.SelectedItem!;
            var newNode = new ConditionDefinition
            {
                Type = newType,
                Operands = newType == "COMPARISON" ? null : new List<ConditionDefinition>(),
                Property = _propertyKeys.FirstOrDefault() ?? "",
                Operator = ">=",
                Value = 0
            };

            if (_root == null)
            {
                _root = newNode;
                RebuildTree(newNode);
                return;
            }

            var selected = _tree.SelectedNode?.Tag as ConditionDefinition;
            if (selected == null)
            {
                MessageBox.Show("Selectează mai întâi un nod AND/OR în care să adaugi noua condiție.",
                    "Niciun nod selectat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (selected.Type == "COMPARISON")
            {
                MessageBox.Show("Un nod COMPARISON este o frunză și nu poate avea sub-condiții. Selectează un nod AND/OR.",
                    "Nod invalid", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            selected.Operands ??= new List<ConditionDefinition>();
            selected.Operands.Add(newNode);
            RebuildTree(newNode);
        }

        private void DeleteSelectedNode()
        {
            var selected = _tree.SelectedNode?.Tag as ConditionDefinition;
            if (selected == null) return;

            if (selected == _root)
            {
                if (MessageBox.Show("Ștergi întreaga condiție?", "Confirmare", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                _root = null;
            }
            else
            {
                var parent = _parentOf.TryGetValue(selected, out var p) ? p : null;
                parent?.Operands?.Remove(selected);
            }
            RebuildTree();
        }

        // ── Leaf editing ─────────────────────────────────────────────────

        private void RefreshLeafPanel()
        {
            var selected = _tree.SelectedNode?.Tag as ConditionDefinition;
            if (selected == null || selected.Type != "COMPARISON")
            {
                _grpLeaf.Visible = false;
                return;
            }

            _grpLeaf.Visible = true;
            _cmbProperty.Text = selected.Property ?? "";
            _cmbOperator.Text = string.IsNullOrEmpty(selected.Operator) ? ">=" : selected.Operator;
            _txtValue.Text = selected.Value.ToString();
        }

        private void ApplyLeafEdit()
        {
            var selected = _tree.SelectedNode?.Tag as ConditionDefinition;
            if (selected == null || selected.Type != "COMPARISON") return;

            if (string.IsNullOrWhiteSpace(_cmbProperty.Text))
            {
                MessageBox.Show("Alege o proprietate.", "Date incomplete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(_txtValue.Text.Trim(), out int value))
            {
                MessageBox.Show("Valoarea trebuie să fie un număr întreg.", "Valoare invalidă", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selected.Property = _cmbProperty.Text.Trim();
            selected.Operator = string.IsNullOrEmpty(_cmbOperator.Text) ? ">=" : _cmbOperator.Text;
            selected.Value = value;

            RebuildTree(selected);
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            Result = _root;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
