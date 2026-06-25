namespace StoryEngine.Player
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── Controls ───────────────────────────────────────────────────────
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem menuItemStory;
        private System.Windows.Forms.ToolStripMenuItem menuOpen;
        private System.Windows.Forms.ToolStripMenuItem menuRestart;
        private System.Windows.Forms.ToolStripSeparator menuSep1;
        private System.Windows.Forms.ToolStripMenuItem menuSave;
        private System.Windows.Forms.ToolStripMenuItem menuLoad;
        private System.Windows.Forms.ToolStripSeparator menuSep2;
        private System.Windows.Forms.ToolStripMenuItem menuExit;

        private System.Windows.Forms.TableLayoutPanel layoutRoot;

        // Row 0 – title bar
        private System.Windows.Forms.Panel panelTitle;
        private System.Windows.Forms.Label lblTitle;

        // Row 1 – HUD strip
        private System.Windows.Forms.Panel panelHud;

        // Row 2 – main content
        private System.Windows.Forms.Panel panelContent;
        private System.Windows.Forms.TableLayoutPanel layoutContent;
        private System.Windows.Forms.Panel panelTextOverlay;
        private System.Windows.Forms.PictureBox picBackground;
        private System.Windows.Forms.Label lblDayCounter;
        private System.Windows.Forms.RichTextBox txtStoryText;

        // Row 3 – decision buttons
        private System.Windows.Forms.FlowLayoutPanel flowDecisions;

        // Inventory button
        private System.Windows.Forms.Button btnInventory;

        // Toggle button
        private System.Windows.Forms.Button btnShowEffects;
        private bool _showEffectsActive = false;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ── Menu ───────────────────────────────────────────────────────
            menuStrip = new System.Windows.Forms.MenuStrip();
            menuItemStory = new System.Windows.Forms.ToolStripMenuItem("Poveste");
            menuOpen = new System.Windows.Forms.ToolStripMenuItem("Deschide...") { ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O };
            menuRestart = new System.Windows.Forms.ToolStripMenuItem("Restart") { ShortcutKeys = System.Windows.Forms.Keys.F5 };
            menuSep1 = new System.Windows.Forms.ToolStripSeparator();
            menuSave = new System.Windows.Forms.ToolStripMenuItem("Salvează jocul") { ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S };
            menuLoad = new System.Windows.Forms.ToolStripMenuItem("Încarcă salvare");
            menuSep2 = new System.Windows.Forms.ToolStripSeparator();
            menuExit = new System.Windows.Forms.ToolStripMenuItem("Ieșire") { ShortcutKeys = System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4 };

            menuOpen.Click += menuOpen_Click;
            menuRestart.Click += menuRestart_Click;
            menuSave.Click += menuSave_Click;
            menuLoad.Click += menuLoad_Click;
            menuExit.Click += menuExit_Click;

            menuItemStory.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                menuOpen,
                menuRestart,
                menuSep1,
                menuSave,
                menuLoad,
                menuSep2,
                menuExit
            });

            menuStrip.Items.Add(menuItemStory);
            menuStrip.BackColor = System.Drawing.Color.FromArgb(28, 24, 18);
            menuStrip.ForeColor = System.Drawing.Color.FromArgb(200, 185, 140);
            menuStrip.Renderer = new DarkMenuRenderer();

            // ── Root layout ────────────────────────────────────────────────
            layoutRoot = new System.Windows.Forms.TableLayoutPanel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new System.Windows.Forms.Padding(0),
                BackColor = System.Drawing.Color.FromArgb(18, 16, 12)
            };

            layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100));

            layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52));    // title
            layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 130));   // HUD pe doua randuri
            layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100));    // content
            layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 180));   // decisions

            // ── Row 0: Title ───────────────────────────────────────────────
            panelTitle = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(24, 20, 14)
            };

            lblTitle = new System.Windows.Forms.Label
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                Text = "Story Engine Player",
                Font = new System.Drawing.Font("Palatino Linotype", 18f, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(220, 195, 120),
                BackColor = System.Drawing.Color.Transparent,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Padding = new System.Windows.Forms.Padding(0, 4, 0, 0)
            };

            btnInventory = new System.Windows.Forms.Button
            {
                Text = "🎒 Inventar",
                Size = new System.Drawing.Size(110, 32),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(45, 40, 28),
                ForeColor = System.Drawing.Color.FromArgb(210, 190, 130),
                Font = new System.Drawing.Font("Segoe UI", 9f),
                Cursor = System.Windows.Forms.Cursors.Hand,
                Enabled = false
            };
            btnInventory.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(90, 80, 50);
            btnInventory.Click += btnInventory_Click;

            btnShowEffects = new System.Windows.Forms.Button
            {
                Text = "☐ Arată efecte",
                Size = new System.Drawing.Size(110, 32),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(45, 40, 28),
                ForeColor = System.Drawing.Color.FromArgb(210, 190, 130),
                Font = new System.Drawing.Font("Segoe UI", 9f),
                Cursor = System.Windows.Forms.Cursors.Hand,
                Enabled = false
            };
            btnShowEffects.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(90, 80, 50);
            btnShowEffects.Click += (s, e) =>
            {
                _showEffectsActive = !_showEffectsActive;
                btnShowEffects.Text = _showEffectsActive ? "☑ Arată efecte" : "☐ Arată efecte";

                if (_engine != null)
                    RefreshStoryText(_engine.GetCurrentBlock());
            };

            panelTitle.Controls.Add(lblTitle);
            panelTitle.Controls.Add(btnInventory);
            panelTitle.Controls.Add(btnShowEffects);

            panelTitle.SizeChanged += (s, e) =>
            {
                btnInventory.Location = new System.Drawing.Point(
                    panelTitle.ClientSize.Width - btnInventory.Width - 10,
                    (panelTitle.ClientSize.Height - btnInventory.Height) / 2);

                btnInventory.BringToFront();

                btnShowEffects.Location = new System.Drawing.Point(
                    btnInventory.Left - btnShowEffects.Width - 8,
                    (panelTitle.ClientSize.Height - btnShowEffects.Height) / 2);

                btnShowEffects.BringToFront();
            };

            // ── Row 1: HUD ─────────────────────────────────────────────────
            panelHud = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(28, 24, 18),
                Padding = new System.Windows.Forms.Padding(12, 6, 12, 6)
            };

            // HUD bars are added dynamically in MainForm.cs / BuildHudBars()

            // ── Row 2: Content ─────────────────────────────────────────────
            panelContent = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(18, 16, 12),
                Padding = new System.Windows.Forms.Padding(10, 6, 10, 6)
            };

            layoutContent = new System.Windows.Forms.TableLayoutPanel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = System.Drawing.Color.FromArgb(18, 16, 12),
                Margin = new System.Windows.Forms.Padding(0),
                Padding = new System.Windows.Forms.Padding(0)
            };

            layoutContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38));
            layoutContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 62));
            layoutContent.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100));

            panelTextOverlay = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(24, 20, 14),
                Padding = new System.Windows.Forms.Padding(14, 8, 14, 8),
                Margin = new System.Windows.Forms.Padding(0, 0, 8, 0)
            };

            lblDayCounter = new System.Windows.Forms.Label
            {
                Dock = System.Windows.Forms.DockStyle.Top,
                Height = 28,
                Text = "",
                Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(190, 165, 90),
                BackColor = System.Drawing.Color.Transparent,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            txtStoryText = new System.Windows.Forms.RichTextBox
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                ReadOnly = true,
                BackColor = System.Drawing.Color.FromArgb(24, 20, 14),
                ForeColor = System.Drawing.Color.FromArgb(230, 215, 175),
                Font = new System.Drawing.Font("Palatino Linotype", 11f),
                BorderStyle = System.Windows.Forms.BorderStyle.None,
                ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical,
                Text = "Deschide un fișier .zip din meniu pentru a începe."
            };

            picBackground = new System.Windows.Forms.PictureBox
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom,
                BackColor = System.Drawing.Color.FromArgb(12, 10, 7),
                Margin = new System.Windows.Forms.Padding(8, 0, 0, 0)
            };

            panelTextOverlay.Controls.Add(txtStoryText);
            panelTextOverlay.Controls.Add(lblDayCounter);

            layoutContent.Controls.Add(panelTextOverlay, 0, 0);
            layoutContent.Controls.Add(picBackground, 1, 0);

            panelContent.Controls.Add(layoutContent);

            // ── Row 3: Decisions ───────────────────────────────────────────
            flowDecisions = new System.Windows.Forms.FlowLayoutPanel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                FlowDirection = System.Windows.Forms.FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = System.Drawing.Color.FromArgb(22, 19, 14),
                Padding = new System.Windows.Forms.Padding(8, 6, 8, 6)
            };

            // ── Assemble ───────────────────────────────────────────────────
            layoutRoot.Controls.Add(panelTitle, 0, 0);
            layoutRoot.Controls.Add(panelHud, 0, 1);
            layoutRoot.Controls.Add(panelContent, 0, 2);
            layoutRoot.Controls.Add(flowDecisions, 0, 3);

            this.SuspendLayout();

            this.Controls.Add(layoutRoot);
            this.Controls.Add(menuStrip);

            this.MainMenuStrip = menuStrip;
            this.Text = "Story Player";
            this.Size = new System.Drawing.Size(900, 790);
            this.MinimumSize = new System.Drawing.Size(640, 650);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.BackColor = System.Drawing.Color.FromArgb(18, 16, 12);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }

    // ── Dark menu renderer ─────────────────────────────────────────────────
    internal class DarkMenuRenderer : System.Windows.Forms.ToolStripProfessionalRenderer
    {
        private static readonly System.Drawing.Color BgNormal = System.Drawing.Color.FromArgb(28, 24, 18);
        private static readonly System.Drawing.Color BgHover = System.Drawing.Color.FromArgb(65, 55, 35);
        private static readonly System.Drawing.Color BgDropdown = System.Drawing.Color.FromArgb(32, 28, 20);
        private static readonly System.Drawing.Color TextNormal = System.Drawing.Color.FromArgb(210, 195, 150);
        private static readonly System.Drawing.Color TextDisabled = System.Drawing.Color.FromArgb(100, 90, 67);
        private static readonly System.Drawing.Color SeparatorColor = System.Drawing.Color.FromArgb(70, 62, 42);

        protected override void OnRenderToolStripBackground(System.Windows.Forms.ToolStripRenderEventArgs e)
        {
            using var brush = new System.Drawing.SolidBrush(BgNormal);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderImageMargin(System.Windows.Forms.ToolStripRenderEventArgs e)
        {
            using var brush = new System.Drawing.SolidBrush(BgDropdown);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderMenuItemBackground(System.Windows.Forms.ToolStripItemRenderEventArgs e)
        {
            var color = e.Item.Selected ? BgHover : BgDropdown;
            using var brush = new System.Drawing.SolidBrush(color);
            e.Graphics.FillRectangle(brush, new System.Drawing.Rectangle(System.Drawing.Point.Empty, e.Item.Size));
        }

        protected override void OnRenderItemText(System.Windows.Forms.ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? TextNormal : TextDisabled;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(System.Windows.Forms.ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using var pen = new System.Drawing.Pen(SeparatorColor);
            e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
        }

        protected override void OnRenderToolStripBorder(System.Windows.Forms.ToolStripRenderEventArgs e)
        {
            using var pen = new System.Drawing.Pen(SeparatorColor);
            var rect = new System.Drawing.Rectangle(0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
            e.Graphics.DrawRectangle(pen, rect);
        }
    }
}