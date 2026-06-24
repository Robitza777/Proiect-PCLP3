using System;
using System.Drawing;
using System.Windows.Forms;
using StoryEngine.Engine;
using StoryEngine.Models;
using StoryEngine.Repository;

namespace StoryEngine.Player
{
    public class MapForm : Form
    {
        private readonly StoryDefinition _story;
        private readonly GameEngine _engine;
        private readonly StoryRepository _repo;
        private readonly string _zipPath;

        private readonly PictureBox _picMap = new PictureBox();
        private readonly Label _lblInfo = new Label();

        public MapLocationDefinition SelectedLocation { get; private set; }

        public MapForm(StoryDefinition story, GameEngine engine, StoryRepository repo, string zipPath)
        {
            _story = story;
            _engine = engine;
            _repo = repo;
            _zipPath = zipPath;

            Text = "Hartă expediție";
            Width = 1370;
            Height = 800;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(18, 16, 12);
            ForeColor = Color.FromArgb(220, 200, 150);

            BuildLayout();
            LoadMapBackground();
            BuildLocationButtons();
        }

        private void BuildLayout()
        {
            _lblInfo.Dock = DockStyle.Bottom;
            _lblInfo.Height = 70;
            _lblInfo.TextAlign = ContentAlignment.MiddleLeft;
            _lblInfo.Padding = new Padding(12);
            _lblInfo.BackColor = Color.FromArgb(30, 25, 15);
            _lblInfo.ForeColor = Color.FromArgb(230, 210, 160);
            _lblInfo.Font = new Font("Segoe UI", 10f);
            _lblInfo.Text = "Alege o locație de pe hartă pentru expediție.";

            _picMap.Dock = DockStyle.Fill;
            _picMap.SizeMode = PictureBoxSizeMode.StretchImage;
            _picMap.BackColor = Color.FromArgb(38, 31, 18);

            Controls.Add(_picMap);
            Controls.Add(_lblInfo);
        }

        private void LoadMapBackground()
        {
            if (!string.IsNullOrEmpty(_story.MapBackground))
            {
                Image img = ImageHelper.LoadFromZip(_repo, _zipPath, _story.MapBackground);

                if (img != null)
                {
                    _picMap.Image = img;
                    return;
                }
            }

            _picMap.Image = CreateDefaultMapImage(900, 560);
        }

        private Image CreateDefaultMapImage(int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bmp))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(205, 178, 112)))
            using (Pen border = new Pen(Color.FromArgb(90, 60, 25), 6))
            using (Pen path = new Pen(Color.FromArgb(130, 75, 35), 3))
            using (Font titleFont = new Font("Georgia", 28, FontStyle.Bold))
            using (Font smallFont = new Font("Segoe UI", 11, FontStyle.Italic))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(80, 50, 25)))
            {
                g.FillRectangle(bg, 0, 0, width, height);
                g.DrawRectangle(border, 12, 12, width - 24, height - 24);

                g.DrawBezier(path, 100, 430, 250, 250, 450, 500, 750, 180);
                g.DrawBezier(path, 180, 170, 300, 80, 520, 210, 690, 90);

                g.DrawString("Expedition Map", titleFont, textBrush, 40, 35);
                g.DrawString("Click on a marked location to start an expedition.", smallFont, textBrush, 45, 90);

                DrawMapSymbol(g, "☠", 130, 260, 34, textBrush);
                DrawMapSymbol(g, "?", 520, 380, 34, textBrush);
                DrawMapSymbol(g, "X", 760, 160, 34, textBrush);
            }

            return bmp;
        }

        private void DrawMapSymbol(Graphics g, string text, int x, int y, int size, Brush brush)
        {
            using Font f = new Font("Segoe UI", size, FontStyle.Bold);
            g.DrawString(text, f, brush, x, y);
        }

        private void BuildLocationButtons()
        {
            if (_story.MapLocations == null || _story.MapLocations.Count == 0)
            {
                _lblInfo.Text = "Această poveste nu are locații definite pentru hartă.";
                return;
            }

            foreach (var location in _story.MapLocations)
            {
                bool unlocked = IsLocationUnlocked(location);

                if (!unlocked && location.HideWhenLocked)
                    continue;

                Button btn = CreateLocationButton(location, unlocked);
                _picMap.Controls.Add(btn);
            }
        }

        private bool IsLocationUnlocked(MapLocationDefinition location)
        {
            if (location.Condition != null && !_engine.EvaluateCondition(location.Condition))
                return false;

            if (location.OneTimeOnly
                && _engine.State.VisitedMapLocations != null
                && _engine.State.VisitedMapLocations.Contains(location.Id))
                return false;

            return true;
        }

        private Button CreateLocationButton(MapLocationDefinition location, bool unlocked)
        {
            Button btn = new Button
            {
                Text = unlocked ? location.Name : "🔒 " + location.Name,
                Tag = location,
                Width = 150,
                Height = 42,
                Left = location.X,
                Top = location.Y,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = unlocked ? Cursors.Hand : Cursors.Default
            };

            if (unlocked)
            {
                btn.BackColor = Color.FromArgb(105, 75, 30);
                btn.ForeColor = Color.FromArgb(255, 225, 135);
                btn.FlatAppearance.BorderColor = Color.FromArgb(255, 210, 90);
                btn.Click += LocationButton_Click;
            }
            else
            {
                btn.BackColor = Color.FromArgb(45, 42, 35);
                btn.ForeColor = Color.FromArgb(140, 130, 105);
                btn.FlatAppearance.BorderColor = Color.FromArgb(80, 75, 65);
                btn.Enabled = false;
            }

            btn.FlatAppearance.BorderSize = 2;

            string description = string.IsNullOrWhiteSpace(location.Description)
                ? location.Name
                : location.Description;

            btn.MouseEnter += (s, e) =>
            {
                _lblInfo.Text = unlocked
                    ? BuildUnlockedInfo(location, description)
                    : $"{location.Name}: locație blocată momentan.";
            };

            btn.MouseLeave += (s, e) =>
            {
                _lblInfo.Text = "Alege o locație de pe hartă pentru expediție.";
            };

            return btn;
        }

        private string BuildUnlockedInfo(MapLocationDefinition location, string description)
        {
            string info = $"{location.Name}: {description}";

            if (location.TravelEffects != null && location.TravelEffects.Count > 0)
            {
                info += " | Cost expediție: ";

                for (int i = 0; i < location.TravelEffects.Count; i++)
                {
                    var effect = location.TravelEffects[i];

                    if (i > 0)
                        info += ", ";

                    string sign = effect.Value > 0 ? "+" : "";
                    info += $"{effect.Property} {sign}{effect.Value}";
                }
            }

            return info;
        }

        private void LocationButton_Click(object sender, EventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not MapLocationDefinition location)
                return;

            DialogResult result = MessageBox.Show(
                $"Vrei să pleci în expediție la {location.Name}?",
                "Pornește expediția",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result != DialogResult.Yes)
                return;

            SelectedLocation = location;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}