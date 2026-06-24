using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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

            var menuJournal = new ToolStripMenuItem("Jurnal aventură");
            menuJournal.Click += (s, e) => ShowJournal();
            menuItemStory.DropDownItems.Insert(5, menuJournal);

            var menuMap = new ToolStripMenuItem("Hartă expediție");
            menuMap.Click += (s, e) => ShowMap();
            menuItemStory.DropDownItems.Insert(6, menuMap);
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

                if (!TryLoadSavedProgress())
                    _engine.StartNewGame();

                _hudBars.Clear();
                RefreshAll();
                SetUiState(_engine.State.IsGameOver ? UiState.GameOver : UiState.Playing);
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

            if (MessageBox.Show("Reîncepi jocul? Progresul salvat se va pierde.",
                    "Restart", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            DeleteSavedProgress();

            _engine.StartNewGame();
            _hudBars.Clear();
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
            lblDayCounter.Text = GetLocationName(block);

            string prefix = "";

            if (_showEffectsActive)
            {
                if (!string.IsNullOrEmpty(_engine.State.LastEffectsSummary))
                    prefix = _engine.State.LastEffectsSummary;
                else if (!string.IsNullOrEmpty(_engine.State.LastResultText))
                    prefix = _engine.State.LastResultText;
            }
            else
            {
                if (!string.IsNullOrEmpty(_engine.State.LastResultText))
                    prefix = _engine.State.LastResultText;
            }

            txtStoryText.Text = string.IsNullOrEmpty(prefix)
                ? block.Text
                : prefix + "\n\n" + block.Text;
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
    foreach (Control c in flowDecisions.Controls)
        if (c is Button b)
        {
            Image oldImg = b.Image;
            b.Image = null;
            oldImg?.Dispose();
        }

    flowDecisions.Controls.Clear();

    if (block.IsFinal)
    {
        ShowGameOver(block);
        return;
    }

    var visibleDecisions = _engine.GetVisibleDecisionsForCurrentBlock();

    if (visibleDecisions.Count == 0)
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

    foreach (var decision in visibleDecisions)
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

            var icon = TryGetDecisionItemIcon(decision);
            if (icon != null)
            {
                Image displayIcon = enabled ? icon : ImageEffects.ToGrayscale(icon);

                btn.Image = new Bitmap(displayIcon, new Size(24, 24));
                btn.ImageAlign = ContentAlignment.MiddleLeft;
                btn.TextImageRelation = TextImageRelation.ImageBeforeText;
                btn.Padding = new Padding(4, 0, 0, 0);

                icon.Dispose();
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
            btnShowEffects.Enabled = state != UiState.NoStory;

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
                    lblDayCounter.Text = GetLocationName(_engine.GetCurrentBlock()) + " — FINAL";
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
        private string GetLocationName(StoryBlock block)
        {
            if (block == null || string.IsNullOrWhiteSpace(block.Id))
                return "Loc necunoscut";

            string id = block.Id;
            string category = block.EventCategory ?? "";

            if (id == "hub.main")
                return "Buncărul familiei";

            if (id == "hub.workshop")
                return "Atelierul improvizat";

            if (id == "hub.infirmary")
                return "Infirmeria";

            if (id == "hub.radio")
                return "Camera radio";

            if (id == "hub.trader")
                return "Ușa de schimb";

            if (id == "random.deck")
                return "Pachetul roșu";

            if (category == "RandomDeck" || id.StartsWith("random.card"))
                return "Card roșu";

            if (category == "scout.bula" || id.StartsWith("scout.bula"))
                return "Expediția lui Bulă";

            if (category == "scout.mara" || id.StartsWith("scout.mara"))
                return "Expediția Marei";

            if (category == "scout.vlad" || id.StartsWith("scout.vlad"))
                return "Expediția lui Vlad";

            if (category == "scout.irina" || id.StartsWith("scout.irina"))
                return "Expediția Irinei";

            if (category == "exp.school" || id.StartsWith("exp.school"))
                return "Școala prăbușită";

            if (category == "exp.market" || id.StartsWith("exp.market"))
                return "Piața ruinată";

            if (category == "exp.forest" || id.StartsWith("exp.forest"))
                return "Pădurea mutantă";

            if (category == "exp.radio" || id.StartsWith("exp.radio"))
                return "Turn radio";

            if (category == "exp.military" || id.StartsWith("exp.military"))
                return "Zona militară";

            if (category == "exp.tunnel" || id.StartsWith("exp.tunnel"))
                return "Tunelul vechi";

            if (category == "exp.pharmacy" || id.StartsWith("exp.pharmacy"))
                return "Farmacia contaminată";

            if (category == "exp.waterplant" || id.StartsWith("exp.waterplant"))
                return "Stația de apă";

            if (category == "exp.lab" || id.StartsWith("exp.lab"))
                return "Laboratorul subteran";

            if (category == "exp.power" || id.StartsWith("exp.power"))
                return "Substația solară";

            if (category == "exp.greenhouse" || id.StartsWith("exp.greenhouse"))
                return "Sera municipală";

            if (category == "exp.evac" || id.StartsWith("exp.evac"))
                return "Platforma de evacuare";

            if (category == "shelter.horror" || id.StartsWith("shelter.horror"))
                return "Buncărul — ceva nu e în regulă";

            if (category == "shelter.cache" || id.StartsWith("shelter.cache"))
                return "Depozitul ascuns";

            if (category == "radio.reply" || id.StartsWith("radio.reply"))
                return "Frecvența necunoscută";

            if (category == "drone.feed" || id.StartsWith("drone.feed"))
                return "Fluxul dronei";

            if (category == "Crisis" || id.StartsWith("crisis."))
                return "Criză în buncăr";

            if (id.StartsWith("ending."))
                return "Final";

            return MakeReadableLocation(id);
        }

        private string MakeReadableLocation(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return "Loc necunoscut";

            string text = id.Replace(".", " ").Replace("_", " ").Replace("-", " ");
            string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length == 0)
                    continue;

                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }

            return string.Join(" ", words);
        }
        private string GetSavedProgressPath()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "StoryEnginePlayer",
                "Saves"
            );

            Directory.CreateDirectory(folder);

            string normalizedPath = _currentZipPath.ToLowerInvariant();
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath));
            string hash = Convert.ToHexString(hashBytes);

            return Path.Combine(folder, hash + ".progress.json");
        }

        private void SaveCurrentProgress()
        {
            if (_engine == null || _story == null || string.IsNullOrEmpty(_currentZipPath))
                return;

            string savePath = GetSavedProgressPath();
            _repo.SaveGameState(_engine.State, savePath);
        }

        private bool TryLoadSavedProgress()
        {
            string savePath = GetSavedProgressPath();

            if (!File.Exists(savePath))
                return false;

            DialogResult result = MessageBox.Show(
                "Am găsit un progres salvat pentru această poveste. Vrei să continui de unde ai rămas?",
                "Continuă jocul",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result != DialogResult.Yes)
                return false;

            try
            {
                GameState savedState = _repo.LoadGameState(savePath);
                _engine.LoadGame(savedState);
                return true;
            }
            catch
            {
                MessageBox.Show(
                    "Salvarea nu a putut fi încărcată. Jocul va începe de la început.",
                    "Salvare invalidă",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return false;
            }
        }

        private void DeleteSavedProgress()
        {
            if (string.IsNullOrEmpty(_currentZipPath))
                return;

            string savePath = GetSavedProgressPath();

            if (File.Exists(savePath))
                File.Delete(savePath);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_engine != null && _story != null && !string.IsNullOrEmpty(_currentZipPath))
            {
                DialogResult result = MessageBox.Show(
                    "Vrei să salvezi progresul înainte să ieși?",
                    "Salvare progres",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        SaveCurrentProgress();
                    }
                    catch
                    {
                        MessageBox.Show(
                            "Progresul nu a putut fi salvat.",
                            "Eroare salvare",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        e.Cancel = true;
                        return;
                    }
                }

                if (result == DialogResult.No)
                {
                    try
                    {
                        DeleteSavedProgress();
                    }
                    catch
                    {
                    }
                }
            }

            base.OnFormClosing(e);
        }
        private void ShowJournal()
        {
            if (_engine == null)
            {
                MessageBox.Show("Deschide mai întâi o poveste.", "Jurnal",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new JournalForm(_engine.State);
            dlg.ShowDialog(this);
        }
       private void ShowMap()
        {
            if (_engine == null || _story == null)
            {
                MessageBox.Show("Deschide mai întâi o poveste.", "Hartă",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new MapForm(_story, _engine, _repo, _currentZipPath);

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                _engine.GoToMapLocation(dlg.SelectedLocation);
                RefreshAll();
                SetUiState(_engine.State.IsGameOver ? UiState.GameOver : UiState.Playing);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nu s-a putut porni expediția:\n{ex.Message}", "Eroare",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ApplyTheme()
        {
            BackColor = Color.FromArgb(18, 16, 12);
            ForeColor = Color.FromArgb(210, 195, 160);
        }
        
    }
}