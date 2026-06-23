using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using StoryEngine.Models;

namespace StoryEngine.Editor
{
    /// <summary>
    /// Editor pentru metadatele rădăcină ale poveștii: Title, Author, Description, StartBlock.
    /// </summary>
    public class StoryRootEditorControl : UserControl
    {
        public event Action TitleChanged;

        private readonly StoryDefinition _story;
        private readonly List<StoryBlock> _blocks;

        private TextBox _txtTitle;
        private TextBox _txtAuthor;
        private TextBox _txtDescription;
        private ComboBox _cmbStartBlock;

        public StoryRootEditorControl(StoryDefinition story, List<StoryBlock> blocks)
        {
            _story = story;
            _blocks = blocks;
            BuildUi();
        }

        private void BuildUi()
        {
            BackColor = Color.FromArgb(18, 16, 12);
            Padding = new Padding(20);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                Padding = new Padding(4)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblHeader = new Label
            {
                Text = "Setări poveste",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 195, 120),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 16)
            };

            AddRow(layout, "Titlu:", _txtTitle = NewTextBox(_story.Title));
            AddRow(layout, "Autor:", _txtAuthor = NewTextBox(_story.Author));
            AddRow(layout, "Descriere:", _txtDescription = NewTextBox(_story.Description, multiline: true));
            AddRow(layout, "Bloc de start:", _cmbStartBlock = NewStartBlockCombo());

            _txtTitle.TextChanged += (s, e) => { _story.Title = _txtTitle.Text; TitleChanged?.Invoke(); };
            _txtAuthor.TextChanged += (s, e) => _story.Author = _txtAuthor.Text;
            _txtDescription.TextChanged += (s, e) => _story.Description = _txtDescription.Text;
            _cmbStartBlock.SelectedIndexChanged += (s, e) =>
            {
                _story.StartBlock = _cmbStartBlock.SelectedItem as string;
            };

            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            panel.Controls.Add(layout);
            panel.Controls.Add(lblHeader);
            lblHeader.Location = new Point(20, 16);
            layout.Location = new Point(20, 56);

            Controls.Add(panel);
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

        private TextBox NewTextBox(string initialValue, bool multiline = false)
        {
            return new TextBox
            {
                Text = initialValue ?? "",
                Width = 380,
                Multiline = multiline,
                Height = multiline ? 70 : 24,
                BackColor = Color.FromArgb(30, 26, 19),
                ForeColor = Color.FromArgb(220, 205, 165),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
        }

        private ComboBox NewStartBlockCombo()
        {
            var combo = new ComboBox
            {
                Width = 380,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 26, 19),
                ForeColor = Color.FromArgb(220, 205, 165),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f)
            };
            foreach (var block in _blocks)
                combo.Items.Add(block.Id);

            if (!string.IsNullOrEmpty(_story.StartBlock) && combo.Items.Contains(_story.StartBlock))
                combo.SelectedItem = _story.StartBlock;

            return combo;
        }
    }
}
