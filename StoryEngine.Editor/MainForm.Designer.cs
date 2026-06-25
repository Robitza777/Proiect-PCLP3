namespace StoryEngine.Editor
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── Menu ───────────────────────────────────────────────────────────
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem menuFile;
        private System.Windows.Forms.ToolStripMenuItem menuNew;
        private System.Windows.Forms.ToolStripMenuItem menuOpen;
        private System.Windows.Forms.ToolStripMenuItem menuSave;
        private System.Windows.Forms.ToolStripMenuItem menuSaveAs;
        private System.Windows.Forms.ToolStripSeparator menuSep1;
        private System.Windows.Forms.ToolStripMenuItem menuValidate;
        private System.Windows.Forms.ToolStripSeparator menuSep2;
        private System.Windows.Forms.ToolStripMenuItem menuExit;

        // ── Tools menu (hartă expediție etc.) ─────────────────────────────────
        private System.Windows.Forms.ToolStripMenuItem menuTools;
        private System.Windows.Forms.ToolStripMenuItem menuMapEditor;

        // ── Root layout ──────────────────────────────────────────────────────
        private System.Windows.Forms.SplitContainer splitMain;     // left tree | right editor+log
        private System.Windows.Forms.SplitContainer splitRight;    // editor | validation log

        // ── Left: TreeView + toolbar ─────────────────────────────────────────
        private System.Windows.Forms.Panel panelTreeContainer;
        private System.Windows.Forms.ToolStrip toolStripTree;
        private System.Windows.Forms.ToolStripButton btnAddBlock;
        private System.Windows.Forms.ToolStripButton btnAddProperty;
        private System.Windows.Forms.ToolStripButton btnDeleteNode;
        private System.Windows.Forms.ToolStripButton btnMapEditor;
        private System.Windows.Forms.ToolStripTextBox txtSearchBlocks;
        private System.Windows.Forms.TreeView treeStory;

        // ── Center: editor host panel ────────────────────────────────────────
        private System.Windows.Forms.Panel panelEditorHost;
        private System.Windows.Forms.Label lblNoSelection;

        // ── Bottom: validation log ───────────────────────────────────────────
        private System.Windows.Forms.ListBox listValidation;
        private System.Windows.Forms.Label lblValidationHeader;

        // ── Status bar ────────────────────────────────────────────────────────
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ── Theme colors used throughout ────────────────────────────────
            var colBg = System.Drawing.Color.FromArgb(18, 16, 12);
            var colPanelBg = System.Drawing.Color.FromArgb(24, 20, 14);
            var colTreeBg = System.Drawing.Color.FromArgb(22, 19, 14);
            var colText = System.Drawing.Color.FromArgb(210, 195, 160);
            var colAccent = System.Drawing.Color.FromArgb(220, 195, 120);
            var colBorder = System.Drawing.Color.FromArgb(70, 62, 42);

            // ── Menu ─────────────────────────────────────────────────────────
            menuStrip = new System.Windows.Forms.MenuStrip();
            menuFile = new System.Windows.Forms.ToolStripMenuItem("Fișier");
            menuNew = new System.Windows.Forms.ToolStripMenuItem("Poveste nouă") { ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N };
            menuOpen = new System.Windows.Forms.ToolStripMenuItem("Deschide...") { ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O };
            menuSave = new System.Windows.Forms.ToolStripMenuItem("Salvează") { ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S };
            menuSaveAs = new System.Windows.Forms.ToolStripMenuItem("Salvează ca...");
            menuSep1 = new System.Windows.Forms.ToolStripSeparator();
            menuValidate = new System.Windows.Forms.ToolStripMenuItem("Validează povestea") { ShortcutKeys = System.Windows.Forms.Keys.F6 };
            menuSep2 = new System.Windows.Forms.ToolStripSeparator();
            menuExit = new System.Windows.Forms.ToolStripMenuItem("Ieșire");

            menuNew.Click += menuNew_Click;
            menuOpen.Click += menuOpen_Click;
            menuSave.Click += menuSave_Click;
            menuSaveAs.Click += menuSaveAs_Click;
            menuValidate.Click += menuValidate_Click;
            menuExit.Click += (s, e) => Close();

            menuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[]
                { menuNew, menuOpen, menuSave, menuSaveAs, menuSep1, menuValidate, menuSep2, menuExit });
            menuStrip.Items.Add(menuFile);

            // ── Meniu Tools (Hartă expediție) ────────────────────────────────
            menuTools = new System.Windows.Forms.ToolStripMenuItem("Unelte");
            menuMapEditor = new System.Windows.Forms.ToolStripMenuItem("Hartă expediție...") { ShortcutKeys = System.Windows.Forms.Keys.F7 };
            menuMapEditor.Click += (s, e) => ShowMapEditor();
            menuTools.DropDownItems.Add(menuMapEditor);
            menuStrip.Items.Add(menuTools);

            menuStrip.BackColor = colPanelBg;
            menuStrip.ForeColor = colText;
            menuStrip.Renderer = new EditorDarkRenderer();

            // ── Root split: tree | (editor / log) ───────────────────────────
            splitMain = new System.Windows.Forms.SplitContainer
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                Orientation = System.Windows.Forms.Orientation.Vertical,
                SplitterWidth = 4,
                BackColor = colBorder
            };

            splitRight = new System.Windows.Forms.SplitContainer
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                Orientation = System.Windows.Forms.Orientation.Horizontal,
                SplitterWidth = 4,
                BackColor = colBorder
            };

            // ── Left: toolbar + tree ─────────────────────────────────────────
            panelTreeContainer = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = colTreeBg
            };

            toolStripTree = new System.Windows.Forms.ToolStrip
            {
                Dock = System.Windows.Forms.DockStyle.Top,
                BackColor = colPanelBg,
                GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden,
                Renderer = new EditorDarkRenderer()
            };
            btnAddBlock = new System.Windows.Forms.ToolStripButton("+ Bloc") { ForeColor = colText };
            btnAddProperty = new System.Windows.Forms.ToolStripButton("+ Proprietate") { ForeColor = colText };
            btnDeleteNode = new System.Windows.Forms.ToolStripButton("✕ Șterge") { ForeColor = colText };
            btnMapEditor = new System.Windows.Forms.ToolStripButton("🗺 Hartă expediție") { ForeColor = colText };
            btnAddBlock.Click += btnAddBlock_Click;
            btnAddProperty.Click += btnAddProperty_Click;
            btnDeleteNode.Click += btnDeleteNode_Click;
            btnMapEditor.Click += (s, e) => ShowMapEditor();

            var lblSearch = new System.Windows.Forms.ToolStripLabel("  Caută bloc:") { ForeColor = colText };
            txtSearchBlocks = new System.Windows.Forms.ToolStripTextBox { Size = new System.Drawing.Size(140, 25) };
            txtSearchBlocks.TextChanged += treeSearch_TextChanged;

            toolStripTree.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
                { btnAddBlock, btnAddProperty, new System.Windows.Forms.ToolStripSeparator(), btnDeleteNode,
                  new System.Windows.Forms.ToolStripSeparator(), btnMapEditor,
                  new System.Windows.Forms.ToolStripSeparator(), lblSearch, txtSearchBlocks });

            treeStory = new System.Windows.Forms.TreeView
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = colTreeBg,
                ForeColor = colText,
                BorderStyle = System.Windows.Forms.BorderStyle.None,
                Font = new System.Drawing.Font("Segoe UI", 9.5f),
                FullRowSelect = true,
                HideSelection = false,
                ShowLines = true,
                LineColor = colBorder
            };
            treeStory.AfterSelect += treeStory_AfterSelect;

            panelTreeContainer.Controls.Add(treeStory);
            panelTreeContainer.Controls.Add(toolStripTree);

            // ── Center: editor host ──────────────────────────────────────────
            panelEditorHost = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = colBg,
                AutoScroll = true
            };
            lblNoSelection = new System.Windows.Forms.Label
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                Text = "Selectează un bloc sau o proprietate din arborele din stânga\npentru a-l edita, sau adaugă unul nou cu butoanele de mai sus.",
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                ForeColor = System.Drawing.Color.FromArgb(110, 100, 75),
                Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Italic)
            };
            panelEditorHost.Controls.Add(lblNoSelection);

            // ── Bottom: validation log ───────────────────────────────────────
            var panelLog = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = colPanelBg
            };
            lblValidationHeader = new System.Windows.Forms.Label
            {
                Dock = System.Windows.Forms.DockStyle.Top,
                Height = 24,
                Text = "  Validare",
                ForeColor = colAccent,
                BackColor = colPanelBg,
                Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
            listValidation = new System.Windows.Forms.ListBox
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(20, 17, 12),
                ForeColor = colText,
                BorderStyle = System.Windows.Forms.BorderStyle.None,
                Font = new System.Drawing.Font("Consolas", 9f)
            };
            panelLog.Controls.Add(listValidation);
            panelLog.Controls.Add(lblValidationHeader);

            // ── Assemble splits ───────────────────────────────────────────────
            splitRight.Panel1.Controls.Add(panelEditorHost);
            splitRight.Panel2.Controls.Add(panelLog);

            splitMain.Panel1.Controls.Add(panelTreeContainer);
            splitMain.Panel2.Controls.Add(splitRight);

            // ── Status bar ─────────────────────────────────────────────────────
            statusStrip = new System.Windows.Forms.StatusStrip
            {
                BackColor = colPanelBg
            };
            statusLabel = new System.Windows.Forms.ToolStripStatusLabel("Poveste nouă, fără titlu.")
            {
                ForeColor = colText
            };
            statusStrip.Items.Add(statusLabel);

            // ── Assemble form ────────────────────────────────────────────────
            this.SuspendLayout();
            this.Controls.Add(splitMain);
            this.Controls.Add(menuStrip);
            this.Controls.Add(statusStrip);
            this.MainMenuStrip = menuStrip;
            this.Text = "Story Editor";
            this.Size = new System.Drawing.Size(1200, 800);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.BackColor = colBg;
            this.ForeColor = colText;
            this.ResumeLayout(false);

            // SplitterDistance must be set AFTER controls are sized/docked
            this.Shown += (s, e) =>
            {
                splitMain.SplitterDistance = 280;
                splitRight.SplitterDistance = splitRight.Height - 160;
            };
        }
    }

    // ── Dark renderer shared by MenuStrip and ToolStrip ─────────────────────
    internal class EditorDarkRenderer : System.Windows.Forms.ToolStripProfessionalRenderer
    {
        private static readonly System.Drawing.Color BgNormal = System.Drawing.Color.FromArgb(28, 24, 18);
        private static readonly System.Drawing.Color BgHover = System.Drawing.Color.FromArgb(65, 55, 35);
        private static readonly System.Drawing.Color BgDropdown = System.Drawing.Color.FromArgb(32, 28, 20);
        private static readonly System.Drawing.Color TextNormal = System.Drawing.Color.FromArgb(210, 195, 150);
        private static readonly System.Drawing.Color TextDisabled = System.Drawing.Color.FromArgb(100, 90, 65);
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

        protected override void OnRenderButtonBackground(System.Windows.Forms.ToolStripItemRenderEventArgs e)
        {
            var color = e.Item.Selected ? BgHover : BgNormal;
            using var brush = new System.Drawing.SolidBrush(color);
            e.Graphics.FillRectangle(brush, new System.Drawing.Rectangle(System.Drawing.Point.Empty, e.Item.Size));
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