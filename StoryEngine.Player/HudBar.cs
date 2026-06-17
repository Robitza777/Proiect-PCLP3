using System;
using System.Drawing;
using System.Windows.Forms;
using StoryEngine.Models;

namespace StoryEngine.Player
{
    /// <summary>
    /// A single HUD resource bar (e.g. Food ██████░░ 60).
    /// Created dynamically from StatePropertyDefinition at runtime.
    /// </summary>
    public class HudBar
    {
        public string PropertyKey { get; }

        public Panel Panel { get; }  // add this to panelHud.Controls

        private readonly StatePropertyDefinition _def;
        private readonly Label _lblName;
        private readonly Panel _barTrack;
        private readonly Panel _barFill;
        private readonly Label _lblValue;

        // Color thresholds (as % of max)
        private static readonly Color ColorHigh = Color.FromArgb(80, 160, 80);
        private static readonly Color ColorMid = Color.FromArgb(200, 160, 40);
        private static readonly Color ColorLow = Color.FromArgb(190, 60, 50);
        private static readonly Color ColorBorder = Color.FromArgb(80, 75, 55);

        public HudBar(StatePropertyDefinition def)
        {
            _def = def;
            PropertyKey = def.Key;

            // Outer container: vertical stack (name + bar + value)
            Panel = new Panel
            {
                Width = 130,
                Height = 48,
                Margin = new Padding(4, 0, 4, 0),
                BackColor = Color.Transparent
            };
            Panel.Anchor = AnchorStyles.Left | AnchorStyles.Top;

            // Property name label
            _lblName = new Label
            {
                Text = def.DisplayName.ToUpper(),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 135, 90),
                BackColor = Color.Transparent,
                AutoSize = false,
                Width = 130,
                Height = 16,
                Location = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Progress bar track
            _barTrack = new Panel
            {
                Width = 130,
                Height = 12,
                Location = new Point(0, 18),
                BackColor = Color.FromArgb(40, 36, 26),
                BorderStyle = BorderStyle.None
            };
            using var pen = new System.Drawing.Pen(ColorBorder);
            // Border drawn via Paint if needed; skip for simplicity

            // Fill bar (child of track)
            _barFill = new Panel
            {
                Height = 12,
                Location = new Point(0, 0),
                BackColor = ColorHigh
            };
            _barTrack.Controls.Add(_barFill);

            // Numeric value label
            _lblValue = new Label
            {
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(200, 185, 130),
                BackColor = Color.Transparent,
                AutoSize = false,
                Width = 130,
                Height = 14,
                Location = new Point(0, 33),
                TextAlign = ContentAlignment.MiddleRight
            };

            Panel.Controls.Add(_lblName);
            Panel.Controls.Add(_barTrack);
            Panel.Controls.Add(_lblValue);
        }

        /// <summary>Call this after every state change to redraw the bar.</summary>
        public void Update(int value)
        {
            int range = _def.Max - _def.Min;
            float pct = range > 0 ? (float)(value - _def.Min) / range : 0f;
            pct = Math.Max(0f, Math.Min(1f, pct));

            int fillWidth = (int)(_barTrack.Width * pct);
            _barFill.Width = fillWidth;
            _barFill.BackColor = pct > 0.55f ? ColorHigh : pct > 0.25f ? ColorMid : ColorLow;

            _lblValue.Text = $"{value} / {_def.Max}";
        }
    }
}
