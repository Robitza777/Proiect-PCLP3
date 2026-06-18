using System;
using System.Drawing;
using System.Windows.Forms;
using StoryEngine.Models;
using StoryEngine.Repository;

namespace StoryEngine.Player
{
    public class HudBar
    {
        public string PropertyKey { get; }
        public Panel Panel { get; }

        private readonly StatePropertyDefinition _def;
        private readonly Panel _barTrack;
        private readonly Panel _barFill;
        private readonly Label _lblValue;
        private readonly int _iconWidth;

        private static readonly Color ColorHigh = Color.FromArgb(80, 160, 80);
        private static readonly Color ColorMid = Color.FromArgb(200, 160, 40);
        private static readonly Color ColorLow = Color.FromArgb(190, 60, 50);

        public HudBar(StatePropertyDefinition def, StoryRepository repo = null, string zipPath = null)
        {
            _def = def;
            PropertyKey = def.Key;

            Panel = new Panel
            {
                Width = 140,
                Height = 52,
                Margin = new Padding(4, 0, 4, 0),
                BackColor = Color.Transparent
            };

            int xOffset = 0;

            // Iconița (opțional) — via ImageHelper, fără file lock
            if (repo != null && !string.IsNullOrEmpty(zipPath) && !string.IsNullOrEmpty(def.HudIcon))
            {
                var img = ImageHelper.LoadFromZip(repo, zipPath, def.HudIcon);
                if (img != null)
                {
                    var pic = new PictureBox
                    {
                        Image = new Bitmap(img, new Size(18, 18)),
                        Size = new Size(18, 18),
                        Location = new Point(0, 17),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        BackColor = Color.Transparent
                    };
                    img.Dispose();
                    Panel.Controls.Add(pic);
                    xOffset = 22;
                    _iconWidth = 22;
                }
            }

            Panel.Controls.Add(new Label
            {
                Text = def.DisplayName.ToUpper(),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 135, 90),
                BackColor = Color.Transparent,
                AutoSize = false,
                Width = Panel.Width,
                Height = 16,
                Location = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleLeft
            });

            int barW = Panel.Width - xOffset;
            _barTrack = new Panel
            {
                Width = barW,
                Height = 12,
                Location = new Point(xOffset, 20),
                BackColor = Color.FromArgb(40, 36, 26)
            };
            _barFill = new Panel { Height = 12, Location = new Point(0, 0), BackColor = ColorHigh };
            _barTrack.Controls.Add(_barFill);

            _lblValue = new Label
            {
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(200, 185, 130),
                BackColor = Color.Transparent,
                AutoSize = false,
                Width = Panel.Width,
                Height = 14,
                Location = new Point(0, 35),
                TextAlign = ContentAlignment.MiddleRight
            };

            Panel.Controls.Add(_barTrack);
            Panel.Controls.Add(_lblValue);
        }

        public void Update(int value)
        {
            int range = _def.Max - _def.Min;
            float pct = range > 0 ? (float)(value - _def.Min) / range : 0f;
            pct = Math.Max(0f, Math.Min(1f, pct));

            _barFill.Width = (int)((_barTrack.Width) * pct);
            _barFill.BackColor = pct > 0.55f ? ColorHigh : pct > 0.25f ? ColorMid : ColorLow;
            _lblValue.Text = $"{value} / {_def.Max}";
        }
    }
}
