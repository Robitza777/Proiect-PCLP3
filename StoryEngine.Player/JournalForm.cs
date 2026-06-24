using System;
using System.Drawing;
using System.Windows.Forms;
using StoryEngine.Models;

namespace StoryEngine.Player
{
    public class JournalForm : Form
    {
        public JournalForm(GameState state)
        {
            Text = "Jurnal de aventură";
            Width = 700;
            Height = 500;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(18, 16, 12);
            ForeColor = Color.FromArgb(220, 200, 150);

            var txtJournal = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(20, 17, 11),
                ForeColor = Color.FromArgb(220, 200, 150),
                Font = new Font("Segoe UI", 11f)
            };

            if (state.Journal == null || state.Journal.Count == 0)
            {
                txtJournal.Text = "Jurnalul este gol momentan.";
            }
            else
            {
                txtJournal.Text = string.Join(Environment.NewLine + Environment.NewLine, state.Journal);
            }

            Controls.Add(txtJournal);
        }
    }
}