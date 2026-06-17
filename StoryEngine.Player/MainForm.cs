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
        // ------------------------------------------------------------------ //
        //  State
        // ------------------------------------------------------------------ //
        private GameEngine _engine;
        private StoryDefinition _story;
        private readonly StoryRepository _repo = new StoryRepository();
        private string _currentZipPath;

        // ------------------------------------------------------------------ //
        //  Constructor
        // ------------------------------------------------------------------ //
        public MainForm()
        {
            InitializeComponent();
            ApplyTheme();
            SetUiState(UiState.NoStory);

            // Resize buttons when window is resized
            flowDecisions.SizeChanged += (s, e) => ResizeDecisionButtons();
        }

        private void ResizeDecisionButtons()
        {
            int btnWidth = flowDecisions.ClientSize.Width - 20;
            if (btnWidth <= 0) return;
            foreach (Control c in flowDecisions.Controls)
                if (c is Button btn) btn.Width = btnWidth;
        }

        // ------------------------------------------------------------------ //
        //  Menu handlers
        // ------------------------------------------------------------------ //

        private void menuOpen_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Deschide poveste",
                Filter = "Story files (*.zip)|*.zip"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                _currentZipPath = dlg.FileName;
                _story = _repo.LoadStory(_currentZipPath);
                _engine = new GameEngine(_story);
                _engine.StartNewGame();
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
            RefreshAll();
            SetUiState(UiState.Playing);
        }

        private void menuSave_Click(object sender, EventArgs e)
        {
            if (_engine == null) return;
            using var dlg = new SaveFileDialog
            {
                Title = "Salvează jocul",
                Filter = "Save files (*.sav.json)|*.sav.json",
                FileName = "save1.sav.json"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                _repo.SaveGameState(_engine.State, dlg.FileName);
                StatusMessage("Joc salvat.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare la salvare:\n{ex.Message}", "Eroare",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuLoad_Click(object sender, EventArgs e)
        {
            if (_story == null)
            {
                MessageBox.Show("Deschide mai întâi un fișier de poveste (.zip).",
                    "Atenție", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new OpenFileDialog
            {
                Title = "Încarcă salvare",
                Filter = "Save files (*.sav.json)|*.sav.json"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                var state = _repo.LoadGameState(dlg.FileName);
                _engine.LoadGame(state);
                RefreshAll();
                SetUiState(UiState.Playing);
                StatusMessage("Salvare încărcată.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare la încărcare salvare:\n{ex.Message}", "Eroare",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuExit_Click(object sender, EventArgs e) => Close();

        // ------------------------------------------------------------------ //
        //  Core UI refresh
        // ------------------------------------------------------------------ //

        /// <summary>Refreshes everything: background, text, HUD, decisions.</summary>
        private void RefreshAll()
        {
            var block = _engine.GetCurrentBlock();

            RefreshBackground(block.BackgroundImage);
            RefreshStoryText(block);
            RefreshHud();
            RefreshDecisions(block);
        }

        // ------------------------------------------------------------------ //
        //  Background image
        // ------------------------------------------------------------------ //

        private void RefreshBackground(string imageFilename)
        {
            picBackground.Image?.Dispose();
            picBackground.Image = null;

            if (string.IsNullOrEmpty(imageFilename) || string.IsNullOrEmpty(_currentZipPath))
            {
                // No image — overlay fills everything, opaque dark background
                panelTextOverlay.BackColor = Color.FromArgb(20, 17, 11);
                return;
            }

            try
            {
                string tempPath = _repo.ExtractImage(_currentZipPath, imageFilename);
                if (tempPath != null && File.Exists(tempPath))
                {
                    picBackground.Image = Image.FromFile(tempPath);
                    // Image present — semi-transparent overlay so image shows through
                    panelTextOverlay.BackColor = Color.FromArgb(185, 14, 12, 8);
                }
                else
                {
                    panelTextOverlay.BackColor = Color.FromArgb(20, 17, 11);
                }
            }
            catch
            {
                panelTextOverlay.BackColor = Color.FromArgb(20, 17, 11);
            }
        }

        // ------------------------------------------------------------------ //
        //  Story text
        // ------------------------------------------------------------------ //

        private void RefreshStoryText(StoryBlock block)
        {
            lblDayCounter.Text = $"Ziua {_engine.State.Day}";
            txtStoryText.Text = block.Text;
        }

        // ------------------------------------------------------------------ //
        //  HUD
        // ------------------------------------------------------------------ //

        private readonly List<HudBar> _hudBars = new List<HudBar>();

        private void RefreshHud()
        {
            // First call: build the HUD bars dynamically from story properties
            if (_hudBars.Count == 0)
                BuildHudBars();

            foreach (var bar in _hudBars)
            {
                int value = _engine.State.Properties[bar.PropertyKey];
                bar.Update(value);
            }
        }

        private void BuildHudBars()
        {
            panelHud.Controls.Clear();
            _hudBars.Clear();

            foreach (var prop in _story.Properties)
            {
                if (!prop.ShowInHud) continue;

                var bar = new HudBar(prop);
                _hudBars.Add(bar);
                panelHud.Controls.Add(bar.Panel);
            }
        }

        // ------------------------------------------------------------------ //
        //  Decision buttons
        // ------------------------------------------------------------------ //

        private void RefreshDecisions(StoryBlock block)
        {
            flowDecisions.Controls.Clear();

            if (block.IsFinal)
            {
                ShowGameOver(block);
                return;
            }

            var decisions = _engine.GetAvailableDecisions();

            if (decisions.Count == 0)
            {
                // Edge case: block is not final but has no valid decisions
                var lbl = new Label
                {
                    Text = "Nu există acțiuni disponibile.",
                    ForeColor = Color.FromArgb(180, 180, 160),
                    AutoSize = true,
                    Padding = new Padding(8)
                };
                flowDecisions.Controls.Add(lbl);
                return;
            }

            foreach (var decision in decisions)
            {
                var btn = CreateDecisionButton(decision);
                flowDecisions.Controls.Add(btn);
            }
        }

        private Button CreateDecisionButton(DecisionDefinition decision)
        {
            // ClientSize.Width can be 0 on first render — fall back to form width minus padding
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
                BackColor = Color.FromArgb(45, 40, 30),
                ForeColor = Color.FromArgb(220, 200, 150),
                Font = new Font("Segoe UI", 10f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(100, 90, 60);
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 58, 42);

            btn.Click += DecisionButton_Click;
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

        // ------------------------------------------------------------------ //
        //  Game over screen
        // ------------------------------------------------------------------ //

        private void ShowGameOver(StoryBlock finalBlock)
        {
            SetUiState(UiState.GameOver);

            // Add a "Joacă din nou" button
            var btn = new Button
            {
                Text = "▶  Joacă din nou",
                AutoSize = false,
                Width = 200,
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

        // ------------------------------------------------------------------ //
        //  UI state management
        // ------------------------------------------------------------------ //

        private enum UiState { NoStory, Playing, GameOver }

        private void SetUiState(UiState state)
        {
            menuRestart.Enabled = state != UiState.NoStory;
            menuSave.Enabled = state == UiState.Playing;
            menuLoad.Enabled = state != UiState.NoStory;

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
                    // Text already set by RefreshAll; just update label
                    lblDayCounter.Text = $"Ziua {_engine.State.Day} — FINAL";
                    break;
            }
        }

        private void StatusMessage(string msg)
        {
            // Optional: add a StatusStrip to MainForm and update it here
            // For now just use the title bar temporarily
            Text = $"Story Player — {msg}";
            var t = new System.Windows.Forms.Timer { Interval = 2000 };
            t.Tick += (s, e) => { Text = "Story Player"; t.Dispose(); };
            t.Start();
        }

        // ------------------------------------------------------------------ //
        //  Theme
        // ------------------------------------------------------------------ //

        private void ApplyTheme()
        {
            BackColor = Color.FromArgb(18, 16, 12);
            ForeColor = Color.FromArgb(210, 195, 160);
        }
    }
}