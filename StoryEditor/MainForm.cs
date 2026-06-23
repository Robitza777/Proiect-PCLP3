using System;
using System.Linq;
using System.Windows.Forms;
using StoryEngine.Engine;
using StoryEngine.Models;
using StoryEngine.Repository;

namespace StoryEngine.Editor
{
    public partial class MainForm : Form
    {
        // ------------------------------------------------------------------ //
        //  State
        // ------------------------------------------------------------------ //
        private StoryDefinition _story;
        private readonly StoryRepository _repo = new StoryRepository();
        private readonly StoryValidator _validator = new StoryValidator();
        private readonly ImageWorkspace _images = new ImageWorkspace();
        private string _currentZipPath;
        private string _workspaceDir;
        private bool _isDirty;

        // Nodurile rădăcină ale arborelui (regenerate la fiecare RefreshTree)
        private TreeNode _nodeProperties;
        private TreeNode _nodeBlocks;

        // Controlul de editare curent afișat în panelEditorHost (poate fi null)
        private Control _currentEditor;

        public MainForm()
        {
            InitializeComponent();
            NewStory(); // pornește cu o poveste goală, ca utilizatorul să nu vadă un ecran complet vid
            FormClosing += (s, e) => _images.CleanupWorkspace(_workspaceDir);
        }

        // ------------------------------------------------------------------ //
        //  File menu handlers
        // ------------------------------------------------------------------ //

        private void menuNew_Click(object sender, EventArgs e)
        {
            if (!ConfirmDiscardChanges()) return;
            NewStory();
        }

        private void menuOpen_Click(object sender, EventArgs e)
        {
            if (!ConfirmDiscardChanges()) return;

            using var dlg = new OpenFileDialog { Title = "Deschide poveste", Filter = "Story files (*.zip)|*.zip" };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                _story = _repo.LoadStory(dlg.FileName);

                _images.CleanupWorkspace(_workspaceDir);
                _workspaceDir = _images.ExtractImagesFromZip(dlg.FileName);

                _currentZipPath = dlg.FileName;
                _isDirty = false;
                RefreshTree();
                ShowNoSelection();
                UpdateStatus($"Încărcat: {dlg.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare la încărcare:\n{ex.Message}", "Eroare",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentZipPath))
            {
                menuSaveAs_Click(sender, e);
                return;
            }
            SaveToPath(_currentZipPath);
        }

        private void menuSaveAs_Click(object sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Salvează poveste ca...",
                Filter = "Story files (*.zip)|*.zip",
                FileName = string.IsNullOrEmpty(_story.Title) ? "poveste.zip" : _story.Title + ".zip"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            _currentZipPath = dlg.FileName;
            SaveToPath(_currentZipPath);
        }

        private void SaveToPath(string path)
        {
            var result = _validator.Validate(_story);
            if (!result.IsValid)
            {
                string msg = "Povestea conține erori:\n\n" + string.Join("\n", result.Errors.Select(er => "• " + er)) + "\n\nSalvezi oricum?";
                if (MessageBox.Show(msg, "Erori de validare", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
            }

            try
            {
                _repo.SaveStory(_story, path);
                _images.AppendImagesToZip(_workspaceDir, path); // StoryRepository nu salvează imaginile — completăm noi arhiva
                _isDirty = false;
                UpdateStatus($"Salvat: {path}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare la salvare:\n{ex.Message}", "Eroare",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuValidate_Click(object sender, EventArgs e)
        {
            RunValidation();
        }

        // ------------------------------------------------------------------ //
        //  New story
        // ------------------------------------------------------------------ //

        private void NewStory()
        {
            _images.CleanupWorkspace(_workspaceDir);
            _workspaceDir = _images.CreateNewWorkspace();

            _story = new StoryDefinition
            {
                Title = "Poveste nouă",
                Author = Environment.UserName,
                Description = "",
                StartBlock = null
            };
            _currentZipPath = null;
            _isDirty = false;
            RefreshTree();
            ShowNoSelection();
            UpdateStatus("Poveste nouă, fără titlu.");
        }

        // ------------------------------------------------------------------ //
        //  TreeView management
        // ------------------------------------------------------------------ //

        private void RefreshTree()
        {
            treeStory.BeginUpdate();
            treeStory.Nodes.Clear();

            var rootNode = new TreeNode(string.IsNullOrEmpty(_story.Title) ? "(fără titlu)" : _story.Title)
            {
                Tag = _story,
                ImageKey = "story"
            };

            _nodeProperties = new TreeNode("Proprietăți") { Tag = "PROPERTIES_ROOT" };
            foreach (var prop in _story.Properties)
                _nodeProperties.Nodes.Add(MakePropertyNode(prop));

            _nodeBlocks = new TreeNode("Blocuri") { Tag = "BLOCKS_ROOT" };
            string filter = txtSearchBlocks?.Text?.Trim().ToLowerInvariant() ?? "";
            var blocksToShow = string.IsNullOrEmpty(filter)
                ? _story.Blocks
                : _story.Blocks.Where(b =>
                        (b.Id?.ToLowerInvariant().Contains(filter) ?? false) ||
                        (b.Text?.ToLowerInvariant().Contains(filter) ?? false))
                    .ToList();
            foreach (var block in blocksToShow)
                _nodeBlocks.Nodes.Add(MakeBlockNode(block));

            rootNode.Nodes.Add(_nodeProperties);
            rootNode.Nodes.Add(_nodeBlocks);
            treeStory.Nodes.Add(rootNode);
            rootNode.Expand();
            _nodeProperties.Expand();
            _nodeBlocks.Expand();

            treeStory.EndUpdate();
        }

        private TreeNode MakePropertyNode(StatePropertyDefinition prop)
        {
            return new TreeNode(prop.Key) { Tag = prop };
        }

        private TreeNode MakeBlockNode(StoryBlock block)
        {
            string label = block.Id;
            if (block.Id == _story.StartBlock) label += "  ⭐ START";
            if (block.IsFinal) label += "  🏁";
            return new TreeNode(label) { Tag = block };
        }

        /// <summary>Reconstruiește un singur nod de bloc (după editare), păstrând selecția.</summary>
        private void RefreshBlockNode(StoryBlock block)
        {
            foreach (TreeNode node in _nodeBlocks.Nodes)
            {
                if (ReferenceEquals(node.Tag, block))
                {
                    node.Text = block.Id + (block.Id == _story.StartBlock ? "  ⭐ START" : "") + (block.IsFinal ? "  🏁" : "");
                    return;
                }
            }
        }

        private void RefreshPropertyNode(StatePropertyDefinition prop)
        {
            foreach (TreeNode node in _nodeProperties.Nodes)
            {
                if (ReferenceEquals(node.Tag, prop))
                {
                    node.Text = prop.Key;
                    return;
                }
            }
        }

        private void treeStory_AfterSelect(object sender, TreeViewEventArgs e)
        {
            switch (e.Node.Tag)
            {
                case StoryBlock block:
                    ShowBlockEditor(block);
                    break;
                case StatePropertyDefinition prop:
                    ShowPropertyEditor(prop);
                    break;
                case StoryDefinition story:
                    ShowStoryRootEditor(story);
                    break;
                default:
                    ShowNoSelection();
                    break;
            }
        }

        // ------------------------------------------------------------------ //
        //  Toolbar: Add / Delete
        // ------------------------------------------------------------------ //

        private void btnAddBlock_Click(object sender, EventArgs e)
        {
            string newId = GenerateUniqueBlockId();
            var block = new StoryBlock { Id = newId, Text = "" };
            _story.Blocks.Add(block);

            if (string.IsNullOrEmpty(_story.StartBlock))
                _story.StartBlock = block.Id; // primul bloc creat devine automat StartBlock

            MarkDirty();
            RefreshTree();
            SelectNodeForTag(block);
        }

        private void btnAddProperty_Click(object sender, EventArgs e)
        {
            string newKey = GenerateUniquePropertyKey();
            var prop = new StatePropertyDefinition
            {
                Key = newKey,
                DisplayName = newKey,
                Min = 0,
                Max = 100,
                Initial = 100,
                ShowInHud = true
            };
            _story.Properties.Add(prop);

            MarkDirty();
            RefreshTree();
            SelectNodeForTag(prop);
        }

        private void treeSearch_TextChanged(object sender, EventArgs e)
        {
            RefreshTree();
        }

        private void btnDeleteNode_Click(object sender, EventArgs e)
        {
            var node = treeStory.SelectedNode;
            if (node == null) return;

            switch (node.Tag)
            {
                case StoryBlock block:
                    if (Confirm($"Ștergi blocul '{block.Id}'?\nDeciziile altor blocuri care țintesc spre el vor rămâne, dar vor deveni invalide."))
                    {
                        _story.Blocks.Remove(block);
                        if (_story.StartBlock == block.Id) _story.StartBlock = null;
                        MarkDirty();
                        RefreshTree();
                        ShowNoSelection();
                    }
                    break;

                case StatePropertyDefinition prop:
                    if (Confirm($"Ștergi proprietatea '{prop.Key}'?\nEfectele/condițiile care o folosesc vor deveni invalide."))
                    {
                        _story.Properties.Remove(prop);
                        MarkDirty();
                        RefreshTree();
                        ShowNoSelection();
                    }
                    break;

                default:
                    MessageBox.Show("Selectează un bloc sau o proprietate pentru a-l șterge.",
                        "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
            }
        }

        private void SelectNodeForTag(object tag)
        {
            TreeNode Find(TreeNodeCollection nodes)
            {
                foreach (TreeNode n in nodes)
                {
                    if (ReferenceEquals(n.Tag, tag)) return n;
                    var found = Find(n.Nodes);
                    if (found != null) return found;
                }
                return null;
            }

            var match = Find(treeStory.Nodes);
            if (match != null) treeStory.SelectedNode = match;
        }

        // ------------------------------------------------------------------ //
        //  Editor host: switching the center panel content
        // ------------------------------------------------------------------ //

        private void ShowNoSelection()
        {
            SetEditor(lblNoSelection);
        }

        private void ShowStoryRootEditor(StoryDefinition story)
        {
            var editor = new StoryRootEditorControl(story, _story.Blocks);
            editor.TitleChanged += () =>
            {
                MarkDirty();
                treeStory.Nodes[0].Text = string.IsNullOrEmpty(_story.Title) ? "(fără titlu)" : _story.Title;
            };
            SetEditor(editor);
        }

        private void ShowBlockEditor(StoryBlock block)
        {
            var editor = new BlockEditorControl(block, _story, _images, _workspaceDir);
            editor.BlockChanged += () =>
            {
                MarkDirty();
                RefreshBlockNode(block);
            };
            editor.StartBlockRequested += () =>
            {
                _story.StartBlock = block.Id;
                MarkDirty();
                RefreshTree();
                SelectNodeForTag(block);
            };
            SetEditor(editor);
        }

        private void ShowPropertyEditor(StatePropertyDefinition prop)
        {
            var editor = new PropertyEditorControl(prop, _story.Blocks);
            editor.PropertyChanged += () =>
            {
                MarkDirty();
                RefreshPropertyNode(prop);
            };
            SetEditor(editor);
        }

        private void SetEditor(Control control)
        {
            panelEditorHost.Controls.Clear();
            control.Dock = DockStyle.Fill;
            panelEditorHost.Controls.Add(control);
            _currentEditor = control;
        }

        // ------------------------------------------------------------------ //
        //  Validation
        // ------------------------------------------------------------------ //

        private void RunValidation()
        {
            var result = _validator.Validate(_story);
            listValidation.Items.Clear();

            foreach (var error in result.Errors)
                listValidation.Items.Add("❌ " + error);
            foreach (var warning in result.Warnings)
                listValidation.Items.Add("⚠️ " + warning);

            if (result.IsValid && result.Warnings.Count == 0)
                listValidation.Items.Add("✅ Povestea este validă, fără avertismente.");

            lblValidationHeader.Text = result.IsValid
                ? $"  Validare — OK ({result.Warnings.Count} avertismente)"
                : $"  Validare — {result.Errors.Count} erori, {result.Warnings.Count} avertismente";
        }

        // ------------------------------------------------------------------ //
        //  Helpers
        // ------------------------------------------------------------------ //

        private string GenerateUniqueBlockId()
        {
            int n = 1;
            string candidate;
            do { candidate = $"block{n++}"; }
            while (_story.Blocks.Any(b => b.Id == candidate));
            return candidate;
        }

        private string GenerateUniquePropertyKey()
        {
            int n = 1;
            string candidate;
            do { candidate = $"property{n++}"; }
            while (_story.Properties.Any(p => p.Key == candidate));
            return candidate;
        }

        private void MarkDirty()
        {
            _isDirty = true;
        }

        private void UpdateStatus(string text)
        {
            statusLabel.Text = text;
        }

        private bool Confirm(string message)
        {
            return MessageBox.Show(message, "Confirmare",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        private bool ConfirmDiscardChanges()
        {
            if (!_isDirty) return true;
            var result = MessageBox.Show(
                "Ai modificări nesalvate. Continui fără a salva?",
                "Modificări nesalvate", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            return result == DialogResult.Yes;
        }
    }
}
