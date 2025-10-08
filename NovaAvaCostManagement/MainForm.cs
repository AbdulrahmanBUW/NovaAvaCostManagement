using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Simplified main form - Excel-like editing of AVA XML files
    /// </summary>
    public partial class MainForm : Form
    {
        private List<CostElement> elements = new List<CostElement>();
        private List<string> availableIfcTypes = new List<string>();
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
            this.Text = "AVA XML Editor - Simplified";
            this.MinimumSize = new Size(1000, 600);

            CreateStatusStrip();
            CreateToolStrip();
            CreateMenuStrip();
            CreateMainGrid();
        }

        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip();

            // File Menu
            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add("&Open AVA XML...", null, OpenFile);
            fileMenu.DropDownItems.Add("&Save AVA XML...", null, SaveFile);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("E&xit", null, (s, e) => this.Close());
            menuStrip.Items.Add(fileMenu);

            // Edit Menu
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

            // Help Menu
            var helpMenu = new ToolStripMenuItem("&Help");
            helpMenu.DropDownItems.Add("&About", null, ShowAbout);
            menuStrip.Items.Add(helpMenu);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void CreateToolStrip()
        {
            toolStrip = new ToolStrip();

            toolStrip.Items.Add(new ToolStripButton("Open", null, OpenFile) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            toolStrip.Items.Add(new ToolStripButton("Save", null, SaveFile) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Add Element", null, AddElement) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            toolStrip.Items.Add(new ToolStripButton("Edit Element", null, EditElement) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            toolStrip.Items.Add(new ToolStripButton("Delete Element", null, DeleteElement) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });

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
                AllowUserToAddRows = true,  // Enable Excel-like empty rows
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                BackgroundColor = Color.White,
                ColumnHeadersVisible = true,
                ColumnHeadersHeight = 30,
                RowHeadersWidth = 50,
                SelectionMode = DataGridViewSelectionMode.RowHeaderSelect,  // Enable row selection
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
                EditMode = DataGridViewEditMode.EditOnEnter,
                MultiSelect = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 9F),
                    SelectionBackColor = Color.FromArgb(51, 153, 255),
                    SelectionForeColor = Color.White
                }
            };

            SetupColumns();
            CreateContextMenu();

            // Excel-like keyboard shortcuts
            dataGridView.KeyDown += DataGridView_KeyDown;
            dataGridView.CellValueChanged += DataGridView_CellValueChanged;
            dataGridView.UserDeletingRow += DataGridView_UserDeletingRow;
            dataGridView.DoubleClick += (s, e) => EditElement(s, e);
            dataGridView.CellMouseDown += DataGridView_CellMouseDown;

            this.Controls.Add(dataGridView);
        }

        private void CreateContextMenu()
        {
            var contextMenu = new ContextMenuStrip();

            contextMenu.Items.Add("Copy (Ctrl+C)", null, (s, e) => CopyCells());
            contextMenu.Items.Add("Paste (Ctrl+V)", null, (s, e) => PasteCells());
            contextMenu.Items.Add("Clear (Delete)", null, (s, e) => ClearSelectedCells());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Edit Element (F2)", null, EditElement);
            contextMenu.Items.Add("View Full Text", null, ViewFullText);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Add Element Above", null, AddElementAbove);
            contextMenu.Items.Add("Add Element Below", null, AddElementBelow);
            contextMenu.Items.Add("Delete Element", null, DeleteElement);

            dataGridView.ContextMenuStrip = contextMenu;
        }

        private void DataGridView_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Select the cell on right-click for context menu
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

        private void SetupColumns()
        {
            dataGridView.Columns.Clear();

            // ID - read-only, frozen for easy reference
            AddColumn("colId", "Id", "ID", 60, true, true);

            // Editable fields - arranged in logical order
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

            // Properties column
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

            // Long Text - make wider and use custom formatting
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

            // Format cells to strip HTML for display and show property previews
            dataGridView.CellFormatting += DataGridView_CellFormatting;
        }

        private void AddColumn(string name, string dataPropertyName, string headerText,
            int width, bool readOnly, bool frozen)
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

            // Strip HTML from Text and LongText columns for display
            if (columnName == "colText" || columnName == "colLongText")
            {
                string text = e.Value.ToString();

                // Remove HTML tags for display
                if (text.Contains("<") && text.Contains(">"))
                {
                    // Simple HTML strip - remove tags but keep content
                    text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", "");
                    // Clean up extra whitespace
                    text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

                    // For LongText, show preview only
                    if (columnName == "colLongText" && text.Length > 100)
                    {
                        text = text.Substring(0, 100) + "...";
                    }

                    e.Value = text;
                    e.FormattingApplied = true;
                }
            }
            // Show preview of Properties
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

        private void AddColumn(string name, string dataPropertyName, string headerText, int width, bool readOnly)
        {
            AddColumn(name, dataPropertyName, headerText, width, readOnly, false);
        }

        private void DataGridView_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+C - Copy
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopyCells();
                e.Handled = true;
            }
            // Ctrl+V - Paste
            else if (e.Control && e.KeyCode == Keys.V)
            {
                PasteCells();
                e.Handled = true;
            }
            // Delete - Clear cells
            else if (e.KeyCode == Keys.Delete && !dataGridView.IsCurrentCellInEditMode)
            {
                ClearSelectedCells();
                e.Handled = true;
            }
            // Ctrl+N - Add new element
            else if (e.Control && e.KeyCode == Keys.N)
            {
                AddElement(sender, e);
                e.Handled = true;
            }
            // F2 - Edit element
            else if (e.KeyCode == Keys.F2 && !dataGridView.IsCurrentCellInEditMode)
            {
                EditElement(sender, e);
                e.Handled = true;
            }
        }

        private void CopyCells()
        {
            if (dataGridView.SelectedRows.Count == 0 && dataGridView.GetCellCount(DataGridViewElementStates.Selected) == 0)
                return;

            try
            {
                // Check if entire rows are selected
                if (dataGridView.SelectedRows.Count > 0)
                {
                    // Copy entire rows
                    var selectedElements = new List<CostElement>();
                    foreach (DataGridViewRow row in dataGridView.SelectedRows)
                    {
                        if (row.Index < elements.Count)
                        {
                            selectedElements.Add(elements[row.Index]);
                        }
                    }

                    // Serialize to clipboard in tab-delimited format
                    var clipboardText = new System.Text.StringBuilder();

                    foreach (var element in selectedElements.OrderBy(e => elements.IndexOf(e)))
                    {
                        clipboardText.AppendLine(string.Join("\t",
                            element.Id,
                            element.Name,
                            element.CatalogName,
                            element.CatalogType,
                            element.Text,
                            element.QtyResult,
                            element.Up,
                            element.UpResult,
                            element.Qu,
                            element.Children,
                            element.Ident,
                            element.Properties,
                            element.LongText
                        ));
                    }

                    Clipboard.SetText(clipboardText.ToString());
                    SetStatus($"Copied {selectedElements.Count} row(s)");
                }
                else
                {
                    // Copy selected cells only
                    Clipboard.SetDataObject(dataGridView.GetClipboardContent());
                    SetStatus("Copied cells to clipboard");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Copy error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void PasteCells()
        {
            if (!Clipboard.ContainsText())
                return;

            try
            {
                string clipboardText = Clipboard.GetText();
                string[] lines = clipboardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length == 0) return;

                // Determine starting position
                int startRow = dataGridView.CurrentCell?.RowIndex ?? elements.Count;

                // Check if we're pasting full rows (13 columns) or partial data
                string[] firstLine = lines[0].Split('\t');
                bool isPastingFullRows = firstLine.Length == 13; // All editable columns

                if (isPastingFullRows)
                {
                    // Paste entire rows
                    int pastedCount = 0;
                    foreach (var line in lines)
                    {
                        string[] cells = line.Split('\t');
                        if (cells.Length < 13) continue;

                        var newElement = new CostElement
                        {
                            Id = cells[0],
                            Name = cells[1],
                            CatalogName = cells[2],
                            CatalogType = cells[3],
                            Text = cells[4],
                            QtyResult = decimal.TryParse(cells[5], out decimal qty) ? qty : 0,
                            Up = decimal.TryParse(cells[6], out decimal up) ? up : 0,
                            UpResult = decimal.TryParse(cells[7], out decimal upResult) ? upResult : 0,
                            Qu = cells[8],
                            Children = cells[9],
                            Ident = cells[10],
                            Properties = cells[11],
                            LongText = cells[12]
                        };

                        if (startRow + pastedCount < elements.Count)
                        {
                            elements[startRow + pastedCount] = newElement;
                        }
                        else
                        {
                            elements.Add(newElement);
                        }
                        pastedCount++;
                    }

                    RefreshGrid();
                    SetStatus($"Pasted {pastedCount} row(s)");
                }
                else
                {
                    // Paste cells (existing behavior)
                    int startCol = dataGridView.CurrentCell?.ColumnIndex ?? 0;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (string.IsNullOrEmpty(lines[i])) continue;

                        string[] cells = lines[i].Split('\t');
                        int targetRow = startRow + i;

                        // Add new rows if needed
                        while (targetRow >= elements.Count)
                        {
                            AddEmptyElement();
                        }

                        for (int j = 0; j < cells.Length; j++)
                        {
                            int targetCol = startCol + j;
                            if (targetCol >= dataGridView.Columns.Count) continue;

                            var column = dataGridView.Columns[targetCol];
                            if (column.ReadOnly) continue;

                            // Update element directly
                            var element = elements[targetRow];
                            UpdateElementField(element, column.DataPropertyName, cells[j]);
                        }
                    }

                    RefreshGrid();
                    SetStatus("Pasted cells");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Paste error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UpdateElementField(CostElement element, string fieldName, string value)
        {
            switch (fieldName)
            {
                case "Name":
                    element.Name = value;
                    break;
                case "CatalogName":
                    element.CatalogName = value;
                    break;
                case "CatalogType":
                    element.CatalogType = value;
                    break;
                case "Text":
                    element.Text = value;
                    break;
                case "QtyResult":
                    element.QtyResult = decimal.TryParse(value, out decimal qty) ? qty : 0;
                    break;
                case "Up":
                    element.Up = decimal.TryParse(value, out decimal up) ? up : 0;
                    break;
                case "UpResult":
                    element.UpResult = decimal.TryParse(value, out decimal upResult) ? upResult : 0;
                    break;
                case "Qu":
                    element.Qu = value;
                    break;
                case "Children":
                    element.Children = value;
                    break;
                case "Ident":
                    element.Ident = value;
                    break;
                case "Properties":
                    element.Properties = value;
                    break;
                case "LongText":
                    element.LongText = value;
                    break;
            }
        }

        private void ClearSelectedCells()
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

            // Update element based on which column changed
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

        private void DataGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            // Prevent accidental deletion
            var result = MessageBox.Show("Delete this element?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
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

        private void AddEmptyElement()
        {
            var newElement = new CostElement
            {
                Id = GetNextAvailableId().ToString(),
                Ident = Guid.NewGuid().ToString()
            };
            elements.Add(newElement);
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