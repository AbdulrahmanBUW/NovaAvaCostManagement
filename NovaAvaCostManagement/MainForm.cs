using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace NovaAvaCostManagement
{
    public partial class MainForm : Form
    {
        private List<CostElement> elements = new List<CostElement>();
        private List<CostElement> copiedElements = new List<CostElement>();
        private DataGridView dataGridView;
        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private string currentFilePath;

        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "AVA XML Editor";
            this.MinimumSize = new Size(1000, 600);
            this.BackColor = Color.White;

            CreateStatusStrip();
            CreateToolStrip();
            CreateMenuStrip();
            CreateMainGrid();
        }

        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip();

            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add("&Open AVA XML...", null, OpenFile);
            fileMenu.DropDownItems.Add("&Save AVA XML...", null, SaveFile);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("E&xit", null, (s, e) => this.Close());
            menuStrip.Items.Add(fileMenu);

            var editMenu = new ToolStripMenuItem("&Edit");
            var addItem = new ToolStripMenuItem("&Add Element", null, AddElement);
            addItem.ShortcutKeys = Keys.Control | Keys.N;
            editMenu.DropDownItems.Add(addItem);

            var editItem = new ToolStripMenuItem("&Edit Element", null, EditElement);
            editItem.ShortcutKeys = Keys.F2;
            editMenu.DropDownItems.Add(editItem);

            var deleteItem = new ToolStripMenuItem("&Delete Element", null, DeleteElement);
            deleteItem.ShortcutKeys = Keys.Delete;
            editMenu.DropDownItems.Add(deleteItem);

            menuStrip.Items.Add(editMenu);

            var helpMenu = new ToolStripMenuItem("&Help");
            helpMenu.DropDownItems.Add("&About", null, ShowAbout);
            menuStrip.Items.Add(helpMenu);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void CreateToolStrip()
        {
            toolStrip = new ToolStrip();
            toolStrip.ImageScalingSize = new Size(24, 24);

            toolStrip.Items.Add(new ToolStripButton("Open", null, OpenFile));
            toolStrip.Items.Add(new ToolStripButton("Save", null, SaveFile));
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Add", null, AddElement));
            toolStrip.Items.Add(new ToolStripButton("Edit", null, EditElement));
            toolStrip.Items.Add(new ToolStripButton("Delete", null, DeleteElement));
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Copy", null, (EventHandler)CopyElements));
            toolStrip.Items.Add(new ToolStripButton("Paste", null, (EventHandler)PasteElements));

            this.Controls.Add(toolStrip);
        }

        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready")
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            statusStrip.Items.Add(statusLabel);
            this.Controls.Add(statusStrip);
        }

        private void CreateMainGrid()
        {
            int topOffset = menuStrip.Height + toolStrip.Height;

            dataGridView = new DataGridView
            {
                Location = new Point(0, topOffset),
                Size = new Size(this.ClientSize.Width, this.ClientSize.Height - topOffset - statusStrip.Height),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                BackgroundColor = Color.White,
                ColumnHeadersVisible = true,
                ColumnHeadersHeight = 30,
                RowHeadersWidth = 50,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
                EditMode = DataGridViewEditMode.EditOnEnter,
                MultiSelect = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 9F),
                    SelectionBackColor = SystemColors.Highlight,
                    SelectionForeColor = Color.White
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    BackColor = SystemColors.Control
                },
                RowTemplate = { Height = 28 }
            };

            SetupColumns();
            CreateContextMenu();

            dataGridView.KeyDown += DataGridView_KeyDown;
            dataGridView.CellValueChanged += DataGridView_CellValueChanged;
            dataGridView.UserDeletingRow += DataGridView_UserDeletingRow;
            dataGridView.DoubleClick += (s, args) => EditElement(s, args);
            dataGridView.CellMouseDown += DataGridView_CellMouseDown;

            this.Controls.Add(dataGridView);
        }

        private void SetupColumns()
        {
            dataGridView.Columns.Clear();

            AddColumn("colId", "Id", "ID", 70, true, true);
            AddColumn("colName", "Name", "Name", 250, false, false);
            AddColumn("colCatalogName", "CatalogName", "Catalog Name", 180, false, false);
            AddColumn("colCatalogType", "CatalogType", "Catalog Type", 150, false, false);
            AddColumn("colText", "Text", "Text", 200, false, false);
            AddColumn("colQtyResult", "QtyResult", "Qty", 80, false, false);
            AddColumn("colUp", "Up", "Unit Price", 100, false, false);
            AddColumn("colUpResult", "UpResult", "Total Price", 100, false, false);
            AddColumn("colQu", "Qu", "Unit", 60, false, false);
            AddColumn("colChildren", "Children", "Children", 80, false, false);
            AddColumn("colIdent", "Ident", "Ident (GUID)", 280, false, false);

            var colProperties = new DataGridViewTextBoxColumn
            {
                Name = "colProperties",
                DataPropertyName = "Properties",
                HeaderText = "Properties (Preview)",
                Width = 200,
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.False,
                    Font = new Font("Consolas", 8F)
                }
            };
            dataGridView.Columns.Add(colProperties);

            var colLongText = new DataGridViewTextBoxColumn
            {
                Name = "colLongText",
                DataPropertyName = "LongText",
                HeaderText = "Long Text (Preview)",
                Width = 300,
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.False,
                    Font = new Font("Segoe UI", 8F)
                }
            };
            dataGridView.Columns.Add(colLongText);

            dataGridView.CellFormatting += DataGridView_CellFormatting;
        }

        private void AddColumn(string name, string dataPropertyName, string headerText, int width, bool readOnly, bool frozen)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                DataPropertyName = dataPropertyName,
                HeaderText = headerText,
                Width = width,
                ReadOnly = readOnly,
                Frozen = frozen
            };

            if (readOnly)
            {
                column.DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            }

            dataGridView.Columns.Add(column);
        }

        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null) return;

            var columnName = dataGridView.Columns[e.ColumnIndex].Name;

            if (columnName == "colText" || columnName == "colLongText")
            {
                string text = e.Value.ToString();

                if (text.Contains("<") && text.Contains(">"))
                {
                    text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", "");
                    text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

                    if (columnName == "colLongText" && text.Length > 100)
                    {
                        text = text.Substring(0, 100) + "...";
                    }

                    e.Value = text;
                    e.FormattingApplied = true;
                }
            }
            else if (columnName == "colProperties")
            {
                string props = e.Value.ToString();
                if (props.Length > 50)
                {
                    e.Value = props.Substring(0, 50) + "...";
                    e.FormattingApplied = true;
                }
            }
        }

        private void CreateContextMenu()
        {
            var contextMenu = new ContextMenuStrip();

            contextMenu.Items.Add("Copy Elements", null, (EventHandler)CopyElements);
            contextMenu.Items.Add("Paste Elements at End", null, (EventHandler)PasteElements);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Copy Cells (Ctrl+C)", null, (EventHandler)CopyCells);
            contextMenu.Items.Add("Paste Cells (Ctrl+V)", null, (EventHandler)PasteCells);
            contextMenu.Items.Add("Clear Cells (Delete)", null, (EventHandler)ClearSelectedCells);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Edit Element (F2)", null, (EventHandler)EditElement);
            contextMenu.Items.Add("View Full Text", null, (EventHandler)ViewFullText);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Add Element Above", null, (EventHandler)AddElementAbove);
            contextMenu.Items.Add("Add Element Below", null, (EventHandler)AddElementBelow);
            contextMenu.Items.Add("Delete Element", null, (EventHandler)DeleteElement);

            dataGridView.ContextMenuStrip = contextMenu;
        }

        private void DataGridView_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                dataGridView.CurrentCell = dataGridView[e.ColumnIndex, e.RowIndex];
            }
        }

        private void ViewFullText(object sender, EventArgs e)
        {
            if (dataGridView.CurrentCell == null || dataGridView.CurrentCell.RowIndex >= elements.Count)
                return;

            var element = elements[dataGridView.CurrentCell.RowIndex];
            var columnName = dataGridView.CurrentCell.OwningColumn.DataPropertyName;

            string title = "View Full Content";
            string content = "";

            switch (columnName)
            {
                case "Text":
                    title = "Full Text";
                    content = element.Text;
                    break;
                case "LongText":
                    title = "Full Long Text";
                    content = element.LongText;
                    break;
                case "Properties":
                    title = "Full Properties";
                    content = element.Properties;
                    break;
                default:
                    content = dataGridView.CurrentCell.Value?.ToString() ?? "";
                    break;
            }

            using (var form = new Form())
            {
                form.Text = title;
                form.Size = new Size(700, 500);
                form.StartPosition = FormStartPosition.CenterParent;

                var txtContent = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Both,
                    Font = new Font("Consolas", 9F),
                    Text = content,
                    WordWrap = true
                };

                form.Controls.Add(txtContent);
                form.ShowDialog();
            }
        }

        private void AddElementAbove(object sender, EventArgs e)
        {
            if (dataGridView.CurrentCell == null || dataGridView.CurrentCell.RowIndex >= elements.Count)
            {
                AddElement(sender, e);
                return;
            }

            int insertIndex = dataGridView.CurrentCell.RowIndex;
            int nextId = GetNextAvailableId();

            using (var form = new ElementEditForm(null, nextId))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    elements.Insert(insertIndex, form.CostElement);
                    RefreshGrid();
                    SetStatus($"Element inserted at position {insertIndex + 1}");
                }
            }
        }

        private void AddElementBelow(object sender, EventArgs e)
        {
            if (dataGridView.CurrentCell == null || dataGridView.CurrentCell.RowIndex >= elements.Count)
            {
                AddElement(sender, e);
                return;
            }

            int insertIndex = dataGridView.CurrentCell.RowIndex + 1;
            int nextId = GetNextAvailableId();

            using (var form = new ElementEditForm(null, nextId))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    elements.Insert(insertIndex, form.CostElement);
                    RefreshGrid();
                    SetStatus($"Element inserted at position {insertIndex + 1}");
                }
            }
        }

        private void DataGridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                if (dataGridView.SelectedRows.Count > 0)
                {
                    CopyElements(sender, e);
                    e.Handled = true;
                }
                else
                {
                    CopyCells(sender, e);
                    e.Handled = true;
                }
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                PasteElements(sender, e);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete && !dataGridView.IsCurrentCellInEditMode)
            {
                ClearSelectedCells(sender, e);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.N)
            {
                AddElement(sender, e);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F2 && !dataGridView.IsCurrentCellInEditMode)
            {
                EditElement(sender, e);
                e.Handled = true;
            }
        }

        private void CopyElements(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select one or more rows to copy.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                copiedElements.Clear();

                var selectedIndices = dataGridView.SelectedRows
                    .Cast<DataGridViewRow>()
                    .Where(row => row.Index < elements.Count)
                    .Select(row => row.Index)
                    .OrderBy(index => index)
                    .ToList();

                foreach (int index in selectedIndices)
                {
                    copiedElements.Add(elements[index].Clone());
                }

                SetStatus($"Copied {copiedElements.Count} element(s) to clipboard");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Copy error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void PasteElements(object sender, EventArgs e)
        {
            if (copiedElements.Count == 0)
            {
                MessageBox.Show("No elements copied to paste.", "No Elements",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                int startIndex = elements.Count;
                int pastedCount = 0;

                foreach (var copiedElement in copiedElements)
                {
                    var newElement = copiedElement.Clone();
                    newElement.Id = GetNextAvailableId().ToString();
                    newElement.Ident = Guid.NewGuid().ToString();
                    elements.Add(newElement);
                    pastedCount++;
                }

                RefreshGrid();
                SetStatus($"Pasted {pastedCount} element(s) at the end");

                if (elements.Count > 0)
                {
                    dataGridView.FirstDisplayedScrollingRowIndex = startIndex;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Paste error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CopyCells(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count == 0 && dataGridView.GetCellCount(DataGridViewElementStates.Selected) == 0)
                return;

            try
            {
                Clipboard.SetDataObject(dataGridView.GetClipboardContent());
                SetStatus("Copied cells to clipboard");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Copy error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void PasteCells(object sender, EventArgs e)
        {
            // Keep original implementation from your existing code
        }

        private void ClearSelectedCells(object sender, EventArgs e)
        {
            foreach (DataGridViewCell cell in dataGridView.SelectedCells)
            {
                if (!cell.ReadOnly && !dataGridView.Columns[cell.ColumnIndex].ReadOnly)
                {
                    cell.Value = null;
                }
            }
            SetStatus("Cells cleared");
        }

        private void DataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.RowIndex >= elements.Count) return;

            var row = dataGridView.Rows[e.RowIndex];
            var element = elements[e.RowIndex];
            var column = dataGridView.Columns[e.ColumnIndex];

            switch (column.DataPropertyName)
            {
                case "Name":
                    element.Name = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";
                    break;
                case "Children":
                    element.Children = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";
                    break;
                case "CatalogName":
                    element.CatalogName = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";
                    break;
                case "Ident":
                    element.Ident = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";
                    break;
                case "CatalogType":
                    element.CatalogType = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";
                    break;
                case "Text":
                    element.Text = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";
                    break;
                case "LongText":
                    element.LongText = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";
                    break;
                case "QtyResult":
                    if (decimal.TryParse(row.Cells[e.ColumnIndex].Value?.ToString(), out decimal qty))
                        element.QtyResult = qty;
                    break;
                case "Qu":
                    element.Qu = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";
                    break;
                case "Up":
                    if (decimal.TryParse(row.Cells[e.ColumnIndex].Value?.ToString(), out decimal up))
                        element.Up = up;
                    break;
                case "Properties":
                    element.Properties = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";
                    break;
            }
        }

        private void DataGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs args)
        {
            var result = MessageBox.Show("Delete this element?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                args.Cancel = true;
            }
        }

        private void RefreshGrid()
        {
            var bindingList = new BindingList<CostElement>(elements);
            dataGridView.DataSource = bindingList;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            statusLabel.Text = $"Elements: {elements.Count} | File: {(string.IsNullOrEmpty(currentFilePath) ? "None" : Path.GetFileName(currentFilePath))}";
        }

        private int GetNextAvailableId()
        {
            if (elements.Count == 0) return 1;

            int maxId = elements
                .Select(e => int.TryParse(e.Id, out int id) ? id : 0)
                .DefaultIfEmpty(0)
                .Max();

            return maxId + 1;
        }

        private void OpenFile(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.Title = "Open AVA XML File";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        SetStatus("Loading XML...");
                        elements = XmlImporter.ImportFromXml(dialog.FileName);
                        currentFilePath = dialog.FileName;
                        RefreshGrid();
                        SetStatus($"Loaded {elements.Count} elements from {Path.GetFileName(dialog.FileName)}");

                        MessageBox.Show($"Successfully loaded {elements.Count} elements.", "Import Complete",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading file:\n{ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        SetStatus("Load failed");
                    }
                }
            }
        }

        private void SaveFile(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.Title = "Save AVA XML File";
                dialog.FileName = string.IsNullOrEmpty(currentFilePath)
                    ? "export.xml"
                    : Path.GetFileName(currentFilePath);

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        SetStatus("Saving XML...");
                        XmlExporter.ExportToXml(elements, dialog.FileName, false);
                        currentFilePath = dialog.FileName;
                        SetStatus($"Saved to {Path.GetFileName(dialog.FileName)}");

                        MessageBox.Show($"Successfully exported {elements.Count} elements.", "Export Complete",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving file:\n{ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        SetStatus("Save failed");
                    }
                }
            }
        }

        private void AddElement(object sender, EventArgs e)
        {
            int nextId = GetNextAvailableId();

            using (var form = new ElementEditForm(null, nextId))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    elements.Add(form.CostElement);
                    RefreshGrid();
                    SetStatus("Element added");
                }
            }
        }

        private void EditElement(object sender, EventArgs e)
        {
            if (dataGridView.CurrentCell == null || dataGridView.CurrentCell.RowIndex >= elements.Count)
            {
                MessageBox.Show("Please select an element to edit.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int rowIndex = dataGridView.CurrentCell.RowIndex;
            var element = elements[rowIndex];
            int nextId = GetNextAvailableId();

            using (var form = new ElementEditForm(element, nextId))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    elements[rowIndex] = form.CostElement;
                    RefreshGrid();
                    SetStatus("Element updated");
                }
            }
        }

        private void DeleteElement(object sender, EventArgs e)
        {
            if (dataGridView.CurrentCell == null || dataGridView.CurrentCell.RowIndex >= elements.Count)
            {
                MessageBox.Show("Please select an element to delete.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show("Are you sure you want to delete this element?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                int rowIndex = dataGridView.CurrentCell.RowIndex;
                elements.RemoveAt(rowIndex);
                RefreshGrid();
                SetStatus("Element deleted");
            }
        }

        private void ShowAbout(object sender, EventArgs e)
        {
            MessageBox.Show(
                "AVA XML Editor - Simplified Version\n\n" +
                "Features:\n" +
                "- Edit only necessary fields\n" +
                "- Excel-like copy/paste\n" +
                "- Preserves all XML data on export\n" +
                "- Auto-incrementing IDs\n\n" +
                "Version 2.0",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void SetStatus(string message)
        {
            statusLabel.Text = message;
            Application.DoEvents();
        }

        private void MainForm_Load_1(object sender, EventArgs e)
        {
            // Required for designer
        }
    }
}