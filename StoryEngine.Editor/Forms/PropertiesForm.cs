using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using StoryEngine.Models;

namespace StoryEngine.Editor.Forms
{
    /// <summary>Dialog for managing the story's global StatePropertyDefinition list.</summary>
    public class PropertiesForm : Form
    {
        private readonly DataGridView _grid;
        private readonly List<string> _blockIdsWithBlank;

        public List<StatePropertyDefinition> Result { get; private set; } = new();

        public PropertiesForm(List<StatePropertyDefinition> properties, List<string> blockIds)
        {
            _blockIdsWithBlank = new List<string> { "" };
            _blockIdsWithBlank.AddRange(blockIds);

            Text = "Proprietăți de stare (Statistici / Inventar)";
            Width = 1040;
            Height = 520;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;

            _grid = BuildGrid();
            FillGrid(properties);

            var pnlTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(8) };
            var btnAdd = new Button { Text = "Adaugă proprietate", AutoSize = true };
            var btnDel = new Button { Text = "Șterge selectată", AutoSize = true };
            btnAdd.Click += (s, e) => AddRow();
            btnDel.Click += (s, e) => DeleteSelected();
            pnlTop.Controls.Add(btnAdd);
            pnlTop.Controls.Add(btnDel);

            var pnlBottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            var btnOk = new Button { Text = "OK", Width = 90 };
            var btnCancel = new Button { Text = "Anulează", Width = 90 };
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(btnOk);

            Controls.Add(_grid);
            Controls.Add(pnlBottom);
            Controls.Add(pnlTop);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private DataGridView BuildGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersWidth = 24,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                EditMode = DataGridViewEditMode.EditOnEnter
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Key", HeaderText = "Key (unic, ex: item.lantern)", Width = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DisplayName", HeaderText = "Nume HUD", Width = 130 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Min", HeaderText = "Min", Width = 55 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Max", HeaderText = "Max", Width = 55 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Initial", HeaderText = "Inițial", Width = 60 });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "ShowInHud", HeaderText = "În HUD?", Width = 60 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "HudOrder", HeaderText = "Ordine HUD", Width = 75 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "HudIcon", HeaderText = "Iconiță HUD (opțional)", Width = 140 });

            var colMinBlock = new DataGridViewComboBoxColumn { Name = "OnMinBlock", HeaderText = "Bloc la Min (opțional)", Width = 150 };
            colMinBlock.Items.AddRange(_blockIdsWithBlank.Cast<object>().ToArray());
            var colMaxBlock = new DataGridViewComboBoxColumn { Name = "OnMaxBlock", HeaderText = "Bloc la Max (opțional)", Width = 150 };
            colMaxBlock.Items.AddRange(_blockIdsWithBlank.Cast<object>().ToArray());
            grid.Columns.Add(colMinBlock);
            grid.Columns.Add(colMaxBlock);

            grid.DataError += (s, e) => { e.ThrowException = false; };

            return grid;
        }

        private void FillGrid(List<StatePropertyDefinition> properties)
        {
            foreach (var p in properties)
            {
                int idx = _grid.Rows.Add();
                var row = _grid.Rows[idx];
                row.Cells["Key"].Value = p.Key;
                row.Cells["DisplayName"].Value = p.DisplayName;
                row.Cells["Min"].Value = p.Min.ToString();
                row.Cells["Max"].Value = p.Max.ToString();
                row.Cells["Initial"].Value = p.Initial.ToString();
                row.Cells["ShowInHud"].Value = p.ShowInHud;
                row.Cells["HudOrder"].Value = p.HudOrder.ToString();
                row.Cells["HudIcon"].Value = p.HudIcon;
                row.Cells["OnMinBlock"].Value = p.OnMinBlock ?? "";
                row.Cells["OnMaxBlock"].Value = p.OnMaxBlock ?? "";
            }
        }

        private void AddRow()
        {
            int idx = _grid.Rows.Add();
            var row = _grid.Rows[idx];
            row.Cells["Key"].Value = "proprietate.noua";
            row.Cells["DisplayName"].Value = "Proprietate nouă";
            row.Cells["Min"].Value = "0";
            row.Cells["Max"].Value = "100";
            row.Cells["Initial"].Value = "100";
            row.Cells["ShowInHud"].Value = true;
            row.Cells["HudOrder"].Value = (idx + 1).ToString();
            row.Cells["OnMinBlock"].Value = "";
            row.Cells["OnMaxBlock"].Value = "";
        }

        private void DeleteSelected()
        {
            if (_grid.CurrentRow != null && !_grid.CurrentRow.IsNewRow)
                _grid.Rows.Remove(_grid.CurrentRow);
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            var list = new List<StatePropertyDefinition>();
            var seenKeys = new HashSet<string>();

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;
                string key = (row.Cells["Key"].Value as string ?? "").Trim();
                if (string.IsNullOrEmpty(key))
                {
                    MessageBox.Show("Fiecare proprietate trebuie să aibă o cheie (Key).", "Date incomplete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!seenKeys.Add(key))
                {
                    MessageBox.Show($"Cheia '{key}' este duplicată. Cheile trebuie să fie unice.", "Cheie duplicată", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int.TryParse(row.Cells["Min"].Value as string, out int min);
                int.TryParse(row.Cells["Max"].Value as string, out int max);
                int.TryParse(row.Cells["Initial"].Value as string, out int initial);
                int.TryParse(row.Cells["HudOrder"].Value as string, out int hudOrder);

                string? minBlock = row.Cells["OnMinBlock"].Value as string;
                string? maxBlock = row.Cells["OnMaxBlock"].Value as string;

                list.Add(new StatePropertyDefinition
                {
                    Key = key,
                    DisplayName = (row.Cells["DisplayName"].Value as string ?? "").Trim(),
                    Min = min,
                    Max = max,
                    Initial = initial,
                    ShowInHud = row.Cells["ShowInHud"].Value is bool b && b,
                    HudOrder = hudOrder,
                    HudIcon = string.IsNullOrWhiteSpace(row.Cells["HudIcon"].Value as string) ? null : (row.Cells["HudIcon"].Value as string)!.Trim(),
                    OnMinBlock = string.IsNullOrWhiteSpace(minBlock) ? null : minBlock!.Trim(),
                    OnMaxBlock = string.IsNullOrWhiteSpace(maxBlock) ? null : maxBlock!.Trim()
                });
            }

            Result = list;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
