using System;
using System.Drawing;
using System.Windows.Forms;

namespace StoryEngine.Editor.Forms
{
    /// <summary>Minimal "type a value" dialog — used for new block IDs, renames, etc.</summary>
    public static class Prompt
    {
        public static string? ShowDialog(string title, string label, string defaultValue = "")
        {
            using var form = new Form
            {
                Width = 420,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false
            };

            var lbl = new Label { Left = 12, Top = 14, Width = 380, Text = label };
            var txt = new TextBox { Left = 12, Top = 38, Width = 380, Text = defaultValue };
            var btnOk = new Button { Text = "OK", Left = 222, Top = 75, Width = 80, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Anulează", Left = 308, Top = 75, Width = 84, DialogResult = DialogResult.Cancel };

            form.Controls.Add(lbl);
            form.Controls.Add(txt);
            form.Controls.Add(btnOk);
            form.Controls.Add(btnCancel);
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            txt.SelectAll();

            return form.ShowDialog() == DialogResult.OK ? txt.Text.Trim() : null;
        }
    }
}
