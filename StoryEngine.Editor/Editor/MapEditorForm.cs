using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using StoryEngine.Models;

namespace StoryEngine.Editor
{
    public class MapEditorForm : Form
    {
        private readonly StoryDefinition _story;
        private readonly ImageWorkspace _images;
        private readonly string _workspaceDir;

        private PictureBox _picPreview;
        private ListBox _listLocations;
        private Label _lblMapBackground;
        private Label _lblDetails;

        public bool HasChanges { get; private set; }

        public MapEditorForm(StoryDefinition story, ImageWorkspace images, string workspaceDir)
        {
            _story = story;
            _images = images;
            _workspaceDir = workspaceDir;

            Text = "Editor hartă expediție";
            Width = 1720;
            Height = 800;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(18, 16, 12);
            ForeColor = Color.FromArgb(220, 205, 165);

            BuildUi();
            RefreshAll();
        }

        private void BuildUi()
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 670,
                BackColor = Color.FromArgb(70, 62, 42)
            };

            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 16, 12),
                Padding = new Padding(12)
            };

            var topTools = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.FromArgb(24, 20, 14)
            };

            var btnChooseMap = NewButton("Alege fundal hartă");
            btnChooseMap.Click += btnChooseMap_Click;

            var btnClearMap = NewButton("Șterge fundal");
            btnClearMap.Click += (s, e) =>
            {
                _story.MapBackground = null;
                HasChanges = true;
                RefreshAll();
            };

            _lblMapBackground = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 195, 120),
                Margin = new Padding(12, 10, 0, 0)
            };

            topTools.Controls.Add(btnChooseMap);
            topTools.Controls.Add(btnClearMap);
            topTools.Controls.Add(_lblMapBackground);

            _picPreview = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(38, 31, 18),
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            leftPanel.Controls.Add(_picPreview);
            leftPanel.Controls.Add(topTools);

            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 20, 14),
                Padding = new Padding(10)
            };

            var listTitle = new Label
            {
                Text = "Locații hartă",
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = Color.FromArgb(220, 195, 120),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            _listLocations = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 260,
                BackColor = Color.FromArgb(20, 17, 12),
                ForeColor = Color.FromArgb(220, 205, 165),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            _listLocations.SelectedIndexChanged += (s, e) => RefreshDetails();

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 86,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            var btnAdd = NewButton("+ Adaugă");
            btnAdd.Click += btnAdd_Click;

            var btnEdit = NewButton("Editează");
            btnEdit.Click += btnEdit_Click;

            var btnDelete = NewButton("Șterge");
            btnDelete.Click += btnDelete_Click;

            buttons.Controls.Add(btnAdd);
            buttons.Controls.Add(btnEdit);
            buttons.Controls.Add(btnDelete);

            _lblDetails = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(190, 175, 140),
                Font = new Font("Consolas", 9.5f),
                Padding = new Padding(4),
                AutoEllipsis = true
            };

            rightPanel.Controls.Add(_lblDetails);
            rightPanel.Controls.Add(buttons);
            rightPanel.Controls.Add(_listLocations);
            rightPanel.Controls.Add(listTitle);

            split.Panel1.Controls.Add(leftPanel);
            split.Panel2.Controls.Add(rightPanel);

            Controls.Add(split);
        }

        private void btnChooseMap_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Alege imagine hartă",
                Filter = ImageWorkspace.ImageFileFilter
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                _story.MapBackground = _images.ImportImage(_workspaceDir, dlg.FileName);
                HasChanges = true;
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nu s-a putut importa harta:\n{ex.Message}", "Eroare",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            _story.MapLocations ??= new System.Collections.Generic.List<MapLocationDefinition>();

            var location = new MapLocationDefinition
            {
                Id = GenerateUniqueLocationId(),
                Name = "Locație nouă",
                TargetBlock = _story.Blocks.FirstOrDefault()?.Id,
                X = 100,
                Y = 100
            };

            using var dlg = new MapLocationEditDialog(location, _story, _images, _workspaceDir);

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            _story.MapLocations.Add(location);
            HasChanges = true;
            RefreshAll();
            SelectLocation(location);
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var location = GetSelectedLocation();

            if (location == null)
            {
                MessageBox.Show("Selectează o locație pentru editare.", "Hartă",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new MapLocationEditDialog(location, _story, _images, _workspaceDir);

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            HasChanges = true;
            RefreshAll();
            SelectLocation(location);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var location = GetSelectedLocation();

            if (location == null)
            {
                MessageBox.Show("Selectează o locație pentru ștergere.", "Hartă",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"Ștergi locația '{location.Name}'?", "Confirmare",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            _story.MapLocations.Remove(location);
            HasChanges = true;
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshMapBackground();
            RefreshLocationList();
            RefreshPreviewMarkers();
            RefreshDetails();

            _lblMapBackground.Text = string.IsNullOrEmpty(_story.MapBackground)
                ? "Fundal: implicit"
                : "Fundal: " + _story.MapBackground;
        }

        private void RefreshMapBackground()
        {
            _picPreview.Controls.Clear();

            Image old = _picPreview.Image;
            _picPreview.Image = null;
            old?.Dispose();

            Image img = LoadWorkspaceImage(_story.MapBackground);

            if (img != null)
                _picPreview.Image = img;
            else
                _picPreview.Image = CreateDefaultMapImage(900, 560);
        }

        private void RefreshLocationList()
        {
            object selected = _listLocations.SelectedItem;

            _listLocations.Items.Clear();

            if (_story.MapLocations == null)
                return;

            foreach (var location in _story.MapLocations)
                _listLocations.Items.Add(location);

            if (selected != null && _listLocations.Items.Contains(selected))
                _listLocations.SelectedItem = selected;
        }

        private void RefreshPreviewMarkers()
        {
            _picPreview.Controls.Clear();

            if (_story.MapLocations == null)
                return;

            foreach (var location in _story.MapLocations)
            {
                var marker = new Button
                {
                    Text = string.IsNullOrEmpty(location.Name) ? location.Id : location.Name,
                    Tag = location,
                    Width = 120,
                    Height = 34,
                    Left = location.X,
                    Top = location.Y,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(105, 75, 30),
                    ForeColor = Color.FromArgb(255, 225, 135),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                marker.FlatAppearance.BorderColor = Color.FromArgb(255, 210, 90);
                marker.FlatAppearance.BorderSize = 2;

                marker.Click += (s, e) =>
                {
                    _listLocations.SelectedItem = marker.Tag;
                };

                _picPreview.Controls.Add(marker);
            }
        }

        private void RefreshDetails()
        {
            var location = GetSelectedLocation();

            if (location == null)
            {
                _lblDetails.Text = "Selectează o locație pentru detalii.";
                return;
            }

            string conditionText = location.Condition == null
                ? "fără condiție"
                : FormatCondition(location.Condition);

            _lblDetails.Text =
                $"Id: {location.Id}\n" +
                $"Nume: {location.Name}\n" +
                $"TargetBlock: {location.TargetBlock}\n" +
                $"X/Y: {location.X}, {location.Y}\n" +
                $"Icon: {location.Icon}\n" +
                $"Condiție: {conditionText}\n\n" +
                $"Descriere:\n{location.Description}";
        }

        private MapLocationDefinition GetSelectedLocation()
        {
            return _listLocations.SelectedItem as MapLocationDefinition;
        }

        private void SelectLocation(MapLocationDefinition location)
        {
            if (location == null)
                return;

            foreach (var item in _listLocations.Items)
            {
                if (ReferenceEquals(item, location))
                {
                    _listLocations.SelectedItem = item;
                    return;
                }
            }
        }

        private Image LoadWorkspaceImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(_workspaceDir))
                return null;

            string path = Path.Combine(_images.GetImagesDir(_workspaceDir), fileName);

            if (!File.Exists(path))
                return null;

            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                return new Bitmap(fs);
            }
            catch
            {
                return null;
            }
        }

        private Image CreateDefaultMapImage(int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bmp))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(205, 178, 112)))
            using (Pen border = new Pen(Color.FromArgb(90, 60, 25), 6))
            using (Pen path = new Pen(Color.FromArgb(130, 75, 35), 3))
            using (Font titleFont = new Font("Georgia", 26, FontStyle.Bold))
            using (Font smallFont = new Font("Segoe UI", 11, FontStyle.Italic))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(80, 50, 25)))
            {
                g.FillRectangle(bg, 0, 0, width, height);
                g.DrawRectangle(border, 12, 12, width - 24, height - 24);

                g.DrawBezier(path, 100, 430, 250, 250, 450, 500, 750, 180);
                g.DrawBezier(path, 180, 170, 300, 80, 520, 210, 690, 90);

                g.DrawString("Expedition Map", titleFont, textBrush, 40, 35);
                g.DrawString("Alege o imagine de hartă sau poziționează locațiile aici.", smallFont, textBrush, 45, 85);
            }

            return bmp;
        }

        private string GenerateUniqueLocationId()
        {
            int n = 1;
            string candidate;

            do
            {
                candidate = "location" + n++;
            }
            while (_story.MapLocations != null && _story.MapLocations.Any(l => l.Id == candidate));

            return candidate;
        }

        private string FormatCondition(ConditionDefinition c)
        {
            if (c == null)
                return "";

            if (c.Type == "COMPARISON")
                return $"{c.Property} {c.Operator} {c.Value}";

            if (c.Operands == null || c.Operands.Count == 0)
                return c.Type;

            return "(" + string.Join($" {c.Type} ", c.Operands.Select(FormatCondition)) + ")";
        }

        private Button NewButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 40, 28),
                ForeColor = Color.FromArgb(220, 205, 165),
                Margin = new Padding(4)
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(90, 80, 55);
            return btn;
        }
    }

    internal static class MapLocationListBoxExtensions
    {
        public static string ToDisplayText(this MapLocationDefinition location)
        {
            return string.IsNullOrEmpty(location?.Name)
                ? location?.Id
                : $"{location.Name}  ->  {location.TargetBlock}";
        }
    }
}