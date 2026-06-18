using System.Drawing;
using System.Windows.Forms;
using StoryEngine.Models;
using StoryEngine.Repository;

namespace StoryEngine.Player
{
    public class InventoryForm : Form
    {
        private readonly StoryDefinition _story;
        private readonly GameState _state;
        private readonly StoryRepository _repo;
        private readonly string _zipPath;

        public InventoryForm(StoryDefinition story, GameState state,
                             StoryRepository repo, string zipPath)
        {
            _story = story;
            _state = state;
            _repo = repo;
            _zipPath = zipPath;
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Inventar";
            Size = new Size(340, 420);
            MinimumSize = new Size(280, 300);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(22, 19, 14);
            ForeColor = Color.FromArgb(210, 195, 160);

            var lblTitle = new Label
            {
                Text = "🎒  Inventar",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 195, 120),
                BackColor = Color.FromArgb(28, 24, 16),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = Color.Transparent
            };

            var btnClose = new Button
            {
                Text = "Închide",
                Dock = DockStyle.Bottom,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 36, 26),
                ForeColor = Color.FromArgb(200, 180, 120),
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(80, 72, 48);
            btnClose.Click += (s, e) => Close();

            Controls.Add(flow);
            Controls.Add(lblTitle);
            Controls.Add(btnClose);

            bool hasItems = false;
            foreach (var prop in _story.Properties)
            {
                if (!prop.Key.StartsWith("item.")) continue;
                if (!_state.Properties.TryGetValue(prop.Key, out int val) || val != 1) continue;
                hasItems = true;
                flow.Controls.Add(BuildItemRow(prop));
            }

            if (!hasItems)
                flow.Controls.Add(new Label
                {
                    Text = "Inventarul este gol.",
                    AutoSize = false,
                    Width = 290,
                    Height = 40,
                    ForeColor = Color.FromArgb(150, 135, 90),
                    Font = new Font("Segoe UI", 10f, FontStyle.Italic),
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleCenter
                });
        }

        private Panel BuildItemRow(StatePropertyDefinition prop)
        {
            var row = new Panel
            {
                Width = 290,
                Height = 52,
                Margin = new Padding(0, 4, 0, 4),
                BackColor = Color.FromArgb(32, 28, 20)
            };

            var pic = new PictureBox
            {
                Size = new Size(36, 36),
                Location = new Point(8, 8),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            // HudIcon explicit, altfel derivat din cheie: "item.lantern" → "lantern.png"
            string iconFile = !string.IsNullOrEmpty(prop.HudIcon)
                ? prop.HudIcon
                : ImageHelper.IconFileFromItemKey(prop.Key);

            var img = ImageHelper.LoadFromZip(_repo, _zipPath, iconFile);
            if (img != null)
            {
                pic.Image = img;
            }
            else
            {
                pic.BackColor = Color.FromArgb(55, 50, 35);
                pic.Controls.Add(new Label
                {
                    Text = "?",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.FromArgb(150, 135, 90),
                    Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                    BackColor = Color.Transparent
                });
            }

            row.Controls.Add(pic);
            row.Controls.Add(new Label
            {
                Text = prop.DisplayName,
                Location = new Point(54, 8),
                Size = new Size(220, 20),
                ForeColor = Color.FromArgb(215, 200, 155),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            });
            row.Controls.Add(new Label
            {
                Text = prop.Key,
                Location = new Point(54, 28),
                Size = new Size(220, 16),
                ForeColor = Color.FromArgb(140, 125, 88),
                Font = new Font("Segoe UI", 7.5f),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            });

            return row;
        }
    }
}
