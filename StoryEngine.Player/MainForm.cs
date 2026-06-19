using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using StoryEngine.Engine;
using StoryEngine.Models;
using StoryEngine.Repository;

namespace StoryEngine.Player
{
    public partial class MainForm : Form
    {
        private GameEngine _engine;
        private StoryDefinition _story;
        private readonly StoryRepository _repo = new StoryRepository();
        private string _currentZipPath;

        public MainForm()
        {
            InitializeComponent();
            ApplyTheme();
            SetUiState(UiState.NoStory);
            flowDecisions.SizeChanged += (s, e) => ResizeDecisionButtons();
        }

        private void ResizeDecisionButtons()
        {
            int btnWidth = flowDecisions.ClientSize.Width - 20;
            if (btnWidth <= 0) return;
            foreach (Control c in flowDecisions.Controls)
                if (c is Button btn) btn.Width = btnWidth;
        }

        // ── Menu handlers ─────────────────────────────────────────────────

        private void menuOpen_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Title = "Deschide poveste", Filter = "Story files (*.zip)|*.zip" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try
            {
                _currentZipPath = dlg.FileName;
                _story = _repo.LoadStory(_currentZipPath);
                _engine = new GameEngine(_story);
                _engine.StartNewGame();
                _hudBars.Clear();   // forțează rebuild HUD la noua poveste
                RefreshAll();
                SetUiState(UiState.Playing);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare la încărcare:\n{ex.Message}", "Eroare",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuRestart_Click(object sender, EventArgs e)
        {
            if (_engine == null) return;
            if (MessageBox.Show("Reîncepi jocul? Progresul curent se va pierde.",
                    "Restart", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            _engine.StartNewGame();
            _hudBars.Clear();   // forțează rebuild ca iconițele să fie re-încărcate
            RefreshAll();
            SetUiState(UiState.Playing);
        }

        private void menuSave_Click(object sender, EventArgs e)
        {
            if (_engine == null) return;
            using var dlg = new SaveFileDialog
            { Title = "Salvează jocul", Filter = "Save files (*.sav.json)|*.sav.json", FileName = "save1.sav.json" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try { _repo.SaveGameState(_engine.State, dlg.FileName); StatusMessage("Joc salvat."); }
            catch (Exception ex) { MessageBox.Show($"Eroare la salvare:\n{ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void menuLoad_Click(object sender, EventArgs e)
        {
            if (_story == null)
            {
                MessageBox.Show("Deschide mai întâi un fișier de poveste (.zip).", "Atenție", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using var dlg = new OpenFileDialog { Title = "Încarcă salvare", Filter = "Save files (*.sav.json)|*.sav.json" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try
            {
                _engine.LoadGame(_repo.LoadGameState(dlg.FileName));
                RefreshAll();
                SetUiState(UiState.Playing);
                StatusMessage("Salvare încărcată.");
            }
            catch (Exception ex) { MessageBox.Show($"Eroare la încărcare salvare:\n{ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void menuExit_Click(object sender, EventArgs e) => Close();

        // ── Core refresh ──────────────────────────────────────────────────

        private void RefreshAll()
        {
            var block = _engine.GetCurrentBlock();
            RefreshBackground(block.BackgroundImage);
            RefreshStoryText(block);
            RefreshHud();
            RefreshDecisions(block);
        }

        // ── Background ────────────────────────────────────────────────────

        private void RefreshBackground(string imageFilename)
        {
            picBackground.Image?.Dispose();
            picBackground.Image = null;

            if (string.IsNullOrEmpty(imageFilename) || string.IsNullOrEmpty(_currentZipPath))
            {
                panelTextOverlay.BackColor = Color.FromArgb(20, 17, 11);
                return;
            }

            var img = ImageHelper.LoadFromZip(_repo, _currentZipPath, imageFilename);
            if (img != null)
            {
                picBackground.Image = img;
                panelTextOverlay.BackColor = Color.FromArgb(185, 14, 12, 8);
            }
            else
            {
                panelTextOverlay.BackColor = Color.FromArgb(20, 17, 11);
            }
        }

        // ── Story text ────────────────────────────────────────────────────

        private void RefreshStoryText(StoryBlock block)
        {
            lblDayCounter.Text = $"Ziua {_engine.State.Day}";

            if (!string.IsNullOrEmpty(_engine.State.LastResultText))
                txtStoryText.Text = _engine.State.LastResultText + "\n\n" + block.Text;
            else
                txtStoryText.Text = block.Text;
        }

        // ── HUD ───────────────────────────────────────────────────────────

        private readonly List<HudBar> _hudBars = new List<HudBar>();

        private void RefreshHud()
        {
            if (_hudBars.Count == 0)
                BuildHudBars();

            foreach (var bar in _hudBars)
                bar.Update(_engine.State.Properties[bar.PropertyKey]);
        }

        private void BuildHudBars()
        {
            panelHud.Controls.Clear();
            _hudBars.Clear();

            foreach (var prop in _story.Properties)
            {
                if (!prop.ShowInHud) continue;
                var bar = new HudBar(prop, _repo, _currentZipPath);
                _hudBars.Add(bar);
                panelHud.Controls.Add(bar.Panel);
            }
        }

        // ── Decision buttons ──────────────────────────────────────────────

        private void RefreshDecisions(StoryBlock block)
        {
            // Dispose imagini vechi de pe butoane înainte de clear
            foreach (Control c in flowDecisions.Controls)
                if (c is Button b)
                {
                    Image oldImg = b.Image;
                    b.Image = null;
                    oldImg?.Dispose();
                }
            flowDecisions.Controls.Clear();

            if (block.IsFinal) { ShowGameOver(block); return; }

            if (block.Decisions.Count == 0)
            {
                flowDecisions.Controls.Add(new Label
                {
                    Text = "Nu există acțiuni disponibile.",
                    ForeColor = Color.FromArgb(180, 180, 160),
                    AutoSize = true,
                    Padding = new Padding(8)
                });
                return;
            }

            foreach (var decision in block.Decisions)
            {
                bool available = decision.Condition == null
                    || _engine.EvaluateCondition(decision.Condition);
                flowDecisions.Controls.Add(CreateDecisionButton(decision, available));
            }
        }

        private Button CreateDecisionButton(DecisionDefinition decision, bool enabled)
        {
            int btnWidth = flowDecisions.ClientSize.Width > 20
                ? flowDecisions.ClientSize.Width - 20
                : this.ClientSize.Width - 36;

            var btn = new Button
            {
                Text = decision.Text,
                Tag = decision,
                AutoSize = false,
                Width = btnWidth,
                Height = 48,
                Margin = new Padding(4, 4, 4, 4),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };

            if (enabled)
            {
                btn.BackColor = Color.FromArgb(45, 40, 30);
                btn.ForeColor = Color.FromArgb(220, 200, 150);
                btn.Cursor = Cursors.Hand;
                btn.FlatAppearance.BorderColor = Color.FromArgb(100, 90, 60);
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 58, 42);
                btn.Click += DecisionButton_Click;
            }
            else
            {
                btn.BackColor = Color.FromArgb(22, 20, 15);
                btn.ForeColor = Color.FromArgb(110, 100, 75);
                btn.Cursor = Cursors.Default;
                btn.Enter += (s, e) => { flowDecisions.Focus(); };
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 20, 15);
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(22, 20, 15);
                btn.FlatAppearance.BorderColor = Color.FromArgb(32, 29, 21);
            }
            btn.FlatAppearance.BorderSize = 1;

            // Iconița item (LoadImageFromZip — fără file lock)
            var icon = TryGetDecisionItemIcon(decision);
            if (icon != null)
            {
                    Image displayIcon = enabled ? icon : ImageEffects.ToGrayscale(icon);

                    btn.Image = new Bitmap(displayIcon, new Size(24, 24));
                    btn.ImageAlign = ContentAlignment.MiddleLeft;
                    btn.TextImageRelation = TextImageRelation.ImageBeforeText;
                    btn.Padding = new Padding(4, 0, 0, 0);
                
                icon.Dispose();  // original eliberat; btn.Image are propria copie
            }

            return btn;
        }

        private void DecisionButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is DecisionDefinition decision)
            {
                _engine.ChooseDecision(decision);
                RefreshAll();
            }
        }

        // ── Item icon helper ──────────────────────────────────────────────

        private Image TryGetDecisionItemIcon(DecisionDefinition decision)
        {
            if (decision.Condition?.Type != "COMPARISON") return null;
            if (!decision.Condition.Property.StartsWith("item.")) return null;

            var propDef = _story.Properties.Find(p => p.Key == decision.Condition.Property);

            // HudIcon explicit, altfel derivat: "item.lantern" → "lantern.png"
            string iconFile = !string.IsNullOrEmpty(propDef?.HudIcon)
                ? propDef.HudIcon
                : decision.Condition.Property.Substring("item.".Length) + ".png";

            return ImageHelper.LoadFromZip(_repo, _currentZipPath, iconFile);
        }

        // ── Game over ─────────────────────────────────────────────────────

        private void ShowGameOver(StoryBlock finalBlock)
        {
            int btnWidth = flowDecisions.ClientSize.Width > 20
                ? flowDecisions.ClientSize.Width - 20
                : this.ClientSize.Width - 36;
            SetUiState(UiState.GameOver);
            var btn = new Button
            {
                Text = "▶  Joacă din nou",
                AutoSize = false,
                Width = btnWidth,
                Height = 48,
                Margin = new Padding(4),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(80, 60, 20),
                ForeColor = Color.FromArgb(255, 220, 100),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(180, 140, 40);
            btn.Click += (s, e) => menuRestart_Click(s, e);
            flowDecisions.Controls.Add(btn);
        }

        // ── UI state ──────────────────────────────────────────────────────

        private enum UiState { NoStory, Playing, GameOver }

        private void SetUiState(UiState state)
        {
            menuRestart.Enabled = state != UiState.NoStory;
            menuSave.Enabled = state == UiState.Playing;
            menuLoad.Enabled = state != UiState.NoStory;
            btnInventory.Enabled = state != UiState.NoStory;

            switch (state)
            {
                case UiState.NoStory:
                    lblTitle.Text = "Story Engine Player";
                    lblDayCounter.Text = "";
                    txtStoryText.Text = "Deschide un fișier .zip din meniu pentru a începe.";
                    panelHud.Controls.Clear();
                    flowDecisions.Controls.Clear();
                    break;
                case UiState.Playing:
                    lblTitle.Text = _story?.Title ?? "";
                    break;
                case UiState.GameOver:
                    lblDayCounter.Text = $"Ziua {_engine.State.Day} — FINAL";
                    break;
            }
        }

        // ── Inventory popup ───────────────────────────────────────────────

        private void btnInventory_Click(object sender, EventArgs e)
        {
            using var dlg = new InventoryForm(_story, _engine.State, _repo, _currentZipPath);
            dlg.ShowDialog(this);
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void StatusMessage(string msg)
        {
            Text = $"Story Player — {msg}";
            var t = new System.Windows.Forms.Timer { Interval = 2000 };
            t.Tick += (s, e) => { Text = "Story Player"; t.Dispose(); };
            t.Start();
        }

        private void ApplyTheme()
        {
            BackColor = Color.FromArgb(18, 16, 12);
            ForeColor = Color.FromArgb(210, 195, 160);
        }
    }
}
