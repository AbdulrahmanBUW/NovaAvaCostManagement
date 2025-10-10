using NovaAvaCostManagement;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace NovaAvaCostManagement
{
    public partial class MainForm : Form
    {
        private List<CostElement> elements = new List<CostElement>();
        private List<CostElement> copiedElements = new List<CostElement>();
        private ViewMode currentViewMode = ViewMode.FlatList;
        private UndoRedoManager undoRedoManager = new UndoRedoManager();
        private Dictionary<string, object> cellOldValues = new Dictionary<string, object>();
        private bool isRecordingChanges = true;

        private DataGridView dataGridView;
        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripButton btnToggleWbs;
        private ToolStripButton btnUndo;
        private ToolStripButton btnRedo;
        private string currentFilePath;

        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.Size = new Size(1600, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "AVA XML Editor - SPEC Edition";
            this.MinimumSize = new Size(1200, 600);
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

            var undoItem = new ToolStripMenuItem("&Undo", null, (s, e) => PerformUndo());
            undoItem.ShortcutKeys = Keys.Control | Keys.Z;
            editMenu.DropDownItems.Add(undoItem);

            var redoItem = new ToolStripMenuItem("&Redo", null, (s, e) => PerformRedo());
            redoItem.ShortcutKeys = Keys.Control | Keys.Y;
            editMenu.DropDownItems.Add(redoItem);

            editMenu.DropDownItems.Add(new ToolStripSeparator());

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

            var viewMenu = new ToolStripMenuItem("&View");
            var wbsViewItem = new ToolStripMenuItem("&WBS Hierarchy", null, ToggleWbsView);
            wbsViewItem.ShortcutKeys = Keys.Control | Keys.W;
            viewMenu.DropDownItems.Add(wbsViewItem);

            var flatViewItem = new ToolStripMenuItem("&Flat List", null, ToggleWbsView);
            flatViewItem.ShortcutKeys = Keys.Control | Keys.L;
            viewMenu.DropDownItems.Add(flatViewItem);

            menuStrip.Items.Add(viewMenu);

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

            btnUndo = new ToolStripButton("Undo", null, (s, e) => PerformUndo());
            btnUndo.Enabled = false;
            toolStrip.Items.Add(btnUndo);

            btnRedo = new ToolStripButton("Redo", null, (s, e) => PerformRedo());
            btnRedo.Enabled = false;
            toolStrip.Items.Add(btnRedo);

            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Add", null, AddElement));
            toolStrip.Items.Add(new ToolStripButton("Edit", null, EditElement));
            toolStrip.Items.Add(new ToolStripButton("Delete", null, DeleteElement));
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Copy", null, (EventHandler)CopyElements));
            toolStrip.Items.Add(new ToolStripButton("Paste", null, (EventHandler)PasteElements));
            toolStrip.Items.Add(new ToolStripSeparator());

            btnToggleWbs = new ToolStripButton("WBS View", null, ToggleWbsView);
            btnToggleWbs.CheckOnClick = true;
            toolStrip.Items.Add(btnToggleWbs);

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
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                BackgroundColor = Color.White,
                ColumnHeadersVisible = true,
                ColumnHeadersHeight = 30,
                RowHeadersWidth = 50,
                SelectionMode = DataGridViewSelectionMode.CellSelect,  // Cell selection
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
            dataGridView.CellBeginEdit += DataGridView_CellBeginEdit;
            dataGridView.CellDoubleClick += DataGridView_CellDoubleClick;  // Changed from DoubleClick
            dataGridView.CellMouseDown += DataGridView_CellMouseDown;
            dataGridView.CellFormatting += DataGridView_CellFormatting;

            this.Controls.Add(dataGridView);
        }

        private void DataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Only open edit form on double-click in row header or when Ctrl is held
            if (e.RowIndex >= 0 && Control.ModifierKeys == Keys.Control)
            {
                EditElement(sender, e);
            }
        }


        private void SetupColumns()
        {
            dataGridView.Columns.Clear();

            // New column structure as requested
            AddColumn("colCostId", "Id", "Cost-ID", 80, true, true);
            AddColumn("colName", "Name", "Name", 200, false, false);
            AddColumn("colDescription", "Description", "Description", 150, false, false);
            AddColumn("colSpecFilter", "SpecFilter", "DX.SPEC_filter", 100, false, false);
            AddColumn("colSpecName", "SpecName", "DX.SPEC_Name", 150, false, false);
            AddColumn("colSpecSize", "SpecSize", "DX.SPEC_Size", 100, false, false);
            AddColumn("colSpecType", "SpecType", "DX.SPEC_Type", 120, false, false);
            AddColumn("colSpecManufacturer", "SpecManufacturer", "DX.SPEC_Manufacturer", 150, false, false);
            AddColumn("colSpecMaterial", "SpecMaterial", "DX.SPEC_Material", 120, false, false);
            AddColumn("colLineId", "CalculationId", "Line-ID", 80, true, false);
            AddColumn("colOrder", "Order", "Order", 70, true, false);
            AddColumn("colIdent", "Ident", "GUID-Ident", 280, false, false);

            // Catalog columns - using helper method to display catalog assignments
            AddCatalogColumn("colCatalog1", "Catalog 1", 180);
            AddCatalogColumn("colCatalog2", "Catalog 2", 180);
            AddCatalogColumn("colCatalog3", "Catalog 3", 180);
            AddCatalogColumn("colCatalog4", "Catalog 4", 180);

            AddColumn("colText", "Text", "Text", 200, false, false);

            var colLongText = new DataGridViewTextBoxColumn
            {
                Name = "colLongText",
                DataPropertyName = "LongText",
                HeaderText = "Longtext",
                Width = 300,
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.False,
                    Font = new Font("Segoe UI", 8F)
                }
            };
            dataGridView.Columns.Add(colLongText);

            AddColumn("colQuantity", "Qty", "Quantity", 100, false, false);
            AddColumn("colQtyResult", "QtyResult", "Qty_result", 100, false, false);
            AddColumn("colEinheit", "Qu", "Einheit", 80, false, false);
            AddColumn("colPrice", "Up", "Price", 100, false, false);
            AddColumn("colTotal", "UpResult", "Total", 120, true, false);
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

        private void AddCatalogColumn(string name, string headerText, int width)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = headerText,
                Width = width,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(245, 245, 250)
                }
            };
            dataGridView.Columns.Add(column);
        }

        private void DataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var element = GetElementAtRow(e.RowIndex);
            if (element == null) return;

            var column = dataGridView.Columns[e.ColumnIndex];
            if (column.ReadOnly) return;

            var propertyName = column.DataPropertyName;
            if (string.IsNullOrEmpty(propertyName)) return;

            var property = typeof(CostElement).GetProperty(propertyName);
            if (property == null || !property.CanRead) return;

            // Store old value with unique key
            string key = $"{e.RowIndex}_{e.ColumnIndex}";
            cellOldValues[key] = property.GetValue(element);
        }

        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dataGridView.Rows[e.RowIndex];
            var columnName = dataGridView.Columns[e.ColumnIndex].Name;

            // Get the element
            CostElement element = null;
            var boundItem = row.DataBoundItem;

            if (currentViewMode == ViewMode.WbsHierarchy)
            {
                if (boundItem is WbsDisplayItem wbsItem)
                {
                    element = wbsItem.Element;
                    // Make group rows bold
                    if (wbsItem.IsGroup)
                    {
                        e.CellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold);
                        e.CellStyle.BackColor = Color.FromArgb(230, 240, 255);
                    }
                }
            }
            else
            {
                if (boundItem is CostElement ce)
                    element = ce;
            }

            if (element == null) return;

            // Handle catalog columns
            if (columnName == "colCatalog1")
            {
                e.Value = element.GetCatalog(1);
                e.FormattingApplied = true;
            }
            else if (columnName == "colCatalog2")
            {
                e.Value = element.GetCatalog(2);
                e.FormattingApplied = true;
            }
            else if (columnName == "colCatalog3")
            {
                e.Value = element.GetCatalog(3);
                e.FormattingApplied = true;
            }
            else if (columnName == "colCatalog4")
            {
                e.Value = element.GetCatalog(4);
                e.FormattingApplied = true;
            }
            // Handle Total (auto-calculated)
            else if (columnName == "colTotal")
            {
                e.Value = element.UpResult.ToString("F3");
                e.CellStyle.BackColor = Color.FromArgb(230, 255, 230);
                e.CellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold);
                e.FormattingApplied = true;
            }
            // Text cleanup
            else if (columnName == "colText" || columnName == "colLongText")
            {
                if (e.Value != null)
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
            }
        }

        private void ToggleWbsView(object sender, EventArgs e)
        {
            if (currentViewMode == ViewMode.FlatList)
            {
                currentViewMode = ViewMode.WbsHierarchy;
                btnToggleWbs.Checked = true;
                btnToggleWbs.Text = "Flat View";
                SetStatus("Switched to WBS Hierarchy view");
            }
            else
            {
                currentViewMode = ViewMode.FlatList;
                btnToggleWbs.Checked = false;
                btnToggleWbs.Text = "WBS View";
                SetStatus("Switched to Flat List view");
            }

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            if (currentViewMode == ViewMode.WbsHierarchy)
            {
                // Build WBS tree and flatten to display list
                var wbsTree = WbsBuilder.BuildWbsTree(elements);
                var displayList = WbsNode.FlattenToDisplayList(wbsTree);
                var bindingList = new BindingList<WbsDisplayItem>(displayList);
                dataGridView.DataSource = bindingList;
            }
            else
            {
                // Flat list view
                var bindingList = new BindingList<CostElement>(elements);
                dataGridView.DataSource = bindingList;
            }

            UpdateStatus();
        }

        private void ContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var contextMenu = (ContextMenuStrip)sender;
            bool hasRowSelection = dataGridView.SelectedRows.Count > 0;
            bool hasCellSelection = dataGridView.SelectedCells.Count > 0;

            // Enable cell operations if cells are selected
            contextMenu.Items[0].Enabled = hasCellSelection; // Copy Cells
            contextMenu.Items[1].Enabled = hasCellSelection; // Paste Cells
            contextMenu.Items[2].Enabled = hasCellSelection; // Clear Cells
        }

        private void CreateContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Opening += ContextMenu_Opening;

            contextMenu.Items.Add("Copy Cells (Ctrl+C)", null, (s, e) => CopyCellsToClipboard());
            contextMenu.Items.Add("Paste Cells (Ctrl+V)", null, (s, e) => PasteCellsFromClipboard());
            contextMenu.Items.Add("Clear Cells (Del)", null, (s, e) => ClearSelectedCells());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Copy Elements", null, (EventHandler)CopyElements);
            contextMenu.Items.Add("Paste Elements at End", null, (EventHandler)PasteElements);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Edit Element (Ctrl+Double-Click)", null, (EventHandler)EditElement);
            contextMenu.Items.Add("View Full Text", null, (EventHandler)ViewFullText);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Delete Element", null, (EventHandler)DeleteElement);

            dataGridView.ContextMenuStrip = contextMenu;
        }

        private void DataGridView_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Right-click context menu
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                // Don't change selection if right-clicking on already selected cells
                if (!dataGridView[e.ColumnIndex, e.RowIndex].Selected)
                {
                    dataGridView.CurrentCell = dataGridView[e.ColumnIndex, e.RowIndex];
                }
            }
        }

        private void ViewFullText(object sender, EventArgs e)
        {
            var element = GetElementAtCurrentRow();
            if (element == null) return;

            var columnName = dataGridView.CurrentCell?.OwningColumn.DataPropertyName;

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
                    content = dataGridView.CurrentCell?.Value?.ToString() ?? "";
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

        private CostElement GetElementAtCurrentRow()
        {
            if (dataGridView.CurrentCell == null) return null;

            var row = dataGridView.Rows[dataGridView.CurrentCell.RowIndex];
            var boundItem = row.DataBoundItem;

            if (currentViewMode == ViewMode.WbsHierarchy)
            {
                if (boundItem is WbsDisplayItem wbsItem)
                    return wbsItem.Element;
            }
            else
            {
                if (boundItem is CostElement element)
                    return element;
            }

            return null;
        }

        private void ClearSelectedCells()
        {
            if (dataGridView.SelectedCells.Count == 0) return;

            var changeSet = new ChangeSet("Clear cells");

            foreach (DataGridViewCell cell in dataGridView.SelectedCells)
            {
                if (cell.ReadOnly) continue;

                var column = dataGridView.Columns[cell.ColumnIndex];
                var propertyName = column.DataPropertyName;
                if (string.IsNullOrEmpty(propertyName)) continue;

                var element = GetElementAtRow(cell.RowIndex);
                if (element == null) continue;

                var property = typeof(CostElement).GetProperty(propertyName);
                if (property == null || !property.CanWrite) continue;

                object oldValue = property.GetValue(element);
                object newValue = null;

                // Determine default value based on property type (C# 7.3 compatible)
                if (property.PropertyType == typeof(string))
                {
                    newValue = "";
                }
                else if (property.PropertyType == typeof(decimal))
                {
                    newValue = 0m;
                }
                else if (property.PropertyType == typeof(int))
                {
                    newValue = 0;
                }
                else if (property.PropertyType == typeof(bool))
                {
                    newValue = false;
                }

                changeSet.AddChange(new CellChange(
                    cell.RowIndex,
                    column.Name,
                    propertyName,
                    oldValue,
                    newValue,
                    element
                ));

                property.SetValue(element, newValue);
                cell.Value = newValue;
            }

            if (changeSet.Changes.Count > 0)
            {
                undoRedoManager.RecordChange(changeSet);
                UpdateUndoRedoButtons();
            }

            RefreshGrid();
            SetStatus($"Cleared {changeSet.Changes.Count} cell(s)");
        }

        private void DataGridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z)
            {
                PerformUndo();
                e.Handled = true;
                e.SuppressKeyPress = true;  // Added
            }
            else if (e.Control && e.KeyCode == Keys.Y)
            {
                PerformRedo();
                e.Handled = true;
                e.SuppressKeyPress = true;  // Added
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                // Check if we're in edit mode
                if (dataGridView.IsCurrentCellInEditMode)
                {
                    // Let the default copy behavior work for the text in the cell editor
                    return;
                }

                // Check if we have full rows selected or just cells
                if (dataGridView.SelectedRows.Count > 0)
                {
                    CopyElements(sender, e);
                }
                else
                {
                    CopyCellsToClipboard();
                }
                e.Handled = true;
                e.SuppressKeyPress = true;  // Added
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                // Check if we're in edit mode
                if (dataGridView.IsCurrentCellInEditMode)
                {
                    // Let the default paste behavior work for the text in the cell editor
                    return;
                }

                // Always paste to cells (works for both cell and row selection)
                PasteCellsFromClipboard();
                e.Handled = true;
                e.SuppressKeyPress = true;  // Added
            }
            else if (e.KeyCode == Keys.Delete && !dataGridView.IsCurrentCellInEditMode)
            {
                // If full rows are selected, delete elements
                // If only cells are selected, clear cell values
                if (dataGridView.SelectedRows.Count > 0)
                {
                    DeleteElement(sender, e);
                }
                else
                {
                    ClearSelectedCells();
                }
                e.Handled = true;
                e.SuppressKeyPress = true;  // Added
            }
            else if (e.Control && e.KeyCode == Keys.N)
            {
                AddElement(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true;  // Added
            }
            else if (e.KeyCode == Keys.F2 && !dataGridView.IsCurrentCellInEditMode)
            {
                EditElement(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true;  // Added
            }
            else if (e.Control && e.KeyCode == Keys.W)
            {
                ToggleWbsView(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true;  // Added
            }
        }

        private void PerformUndo()
        {
            if (!undoRedoManager.CanUndo)
            {
                SetStatus("Nothing to undo");
                return;
            }

            isRecordingChanges = false;
            var changeSet = undoRedoManager.Undo();
            RefreshGrid();
            SetStatus($"Undone: {changeSet.Description}");
            UpdateUndoRedoButtons();
            isRecordingChanges = true;
        }

        private void PerformRedo()
        {
            if (!undoRedoManager.CanRedo)
            {
                SetStatus("Nothing to redo");
                return;
            }

            isRecordingChanges = false;
            var changeSet = undoRedoManager.Redo();
            RefreshGrid();
            SetStatus($"Redone: {changeSet.Description}");
            UpdateUndoRedoButtons();
            isRecordingChanges = true;
        }

        private void UpdateUndoRedoButtons()
        {
            btnUndo.Enabled = undoRedoManager.CanUndo;
            btnRedo.Enabled = undoRedoManager.CanRedo;
        }

        private void CopyCellsToClipboard()
        {
            try
            {
                var content = dataGridView.GetClipboardContent();
                if (content != null)
                {
                    Clipboard.SetDataObject(content);
                    SetStatus($"Copied {dataGridView.SelectedCells.Count} cell(s)");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Copy error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void PasteCellsFromClipboard()
        {
            try
            {
                string clipboardText = Clipboard.GetText();
                if (string.IsNullOrEmpty(clipboardText))
                {
                    SetStatus("Clipboard is empty");
                    return;
                }

                // Get starting cell - use top-left of selection
                if (dataGridView.CurrentCell == null)
                {
                    SetStatus("Please select a cell first");
                    return;
                }

                // Find the top-left cell of the selection
                int startRow = dataGridView.CurrentCell.RowIndex;
                int startCol = dataGridView.CurrentCell.ColumnIndex;

                if (dataGridView.SelectedCells.Count > 0)
                {
                    int minRow = int.MaxValue;
                    int minCol = int.MaxValue;

                    foreach (DataGridViewCell cell in dataGridView.SelectedCells)
                    {
                        if (cell.RowIndex < minRow) minRow = cell.RowIndex;
                        if (cell.ColumnIndex < minCol) minCol = cell.ColumnIndex;
                    }

                    startRow = minRow;
                    startCol = minCol;
                }

                // Parse clipboard data (Excel format: tabs separate columns, newlines separate rows)
                string[] rows = clipboardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                var changeSet = new ChangeSet("Paste cells");
                int pastedCells = 0;

                // Temporarily disable change recording to avoid duplicate undo entries
                bool wasRecording = isRecordingChanges;
                isRecordingChanges = false;

                try
                {
                    for (int i = 0; i < rows.Length; i++)
                    {
                        if (string.IsNullOrEmpty(rows[i]) && i == rows.Length - 1) continue; // Skip last empty line

                        int targetRow = startRow + i;
                        if (targetRow >= dataGridView.Rows.Count) break;

                        string[] cells = rows[i].Split('\t');

                        for (int j = 0; j < cells.Length; j++)
                        {
                            int targetCol = startCol + j;
                            if (targetCol >= dataGridView.Columns.Count) break;

                            var column = dataGridView.Columns[targetCol];

                            // Skip read-only columns
                            if (column.ReadOnly) continue;

                            var element = GetElementAtRow(targetRow);
                            if (element == null) continue;

                            string propertyName = column.DataPropertyName;
                            if (string.IsNullOrEmpty(propertyName)) continue;

                            var property = typeof(CostElement).GetProperty(propertyName);
                            if (property == null || !property.CanWrite) continue;

                            // Get old value
                            object oldValue = property.GetValue(element);
                            string newValueStr = cells[j];

                            // Convert and set new value
                            try
                            {
                                object newValue = ConvertValue(newValueStr, property.PropertyType);

                                // Only record if value changed
                                if (!Equals(oldValue, newValue))
                                {
                                    // Record change
                                    changeSet.AddChange(new CellChange(
                                        targetRow,
                                        column.Name,
                                        propertyName,
                                        oldValue,
                                        newValue,
                                        element
                                    ));

                                    property.SetValue(element, newValue);
                                    pastedCells++;
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                }
                finally
                {
                    // Restore recording state
                    isRecordingChanges = wasRecording;
                }

                if (changeSet.Changes.Count > 0)
                {
                    undoRedoManager.RecordChange(changeSet);
                    UpdateUndoRedoButtons();
                }

                // Refresh the grid to show changes
                dataGridView.Refresh();

                SetStatus($"Pasted {pastedCells} cell(s)");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Paste error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (targetType == typeof(string)) return "";
                if (targetType == typeof(decimal)) return 0m;
                if (targetType == typeof(int)) return 0;
                if (targetType == typeof(bool)) return false;
                return null;
            }

            if (targetType == typeof(string))
                return value;
            else if (targetType == typeof(decimal))
                return decimal.Parse(value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
            else if (targetType == typeof(int))
                return int.Parse(value);
            else if (targetType == typeof(bool))
                return bool.Parse(value);

            return Convert.ChangeType(value, targetType);
        }

        private CostElement GetElementAtRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dataGridView.Rows.Count)
                return null;

            var row = dataGridView.Rows[rowIndex];
            var boundItem = row.DataBoundItem;

            if (currentViewMode == ViewMode.WbsHierarchy)
            {
                if (boundItem is WbsDisplayItem wbsItem)
                    return wbsItem.Element;
            }
            else
            {
                if (boundItem is CostElement element)
                    return element;
            }

            return null;
        }

        private void CopyElements(object sender, EventArgs e)
        {
            // Get unique rows from selection
            var selectedRows = new HashSet<DataGridViewRow>();

            if (dataGridView.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dataGridView.SelectedRows)
                    selectedRows.Add(row);
            }
            else if (dataGridView.SelectedCells.Count > 0)
            {
                foreach (DataGridViewCell cell in dataGridView.SelectedCells)
                    selectedRows.Add(dataGridView.Rows[cell.RowIndex]);
            }

            if (selectedRows.Count == 0)
            {
                MessageBox.Show("Please select one or more rows to copy.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                copiedElements.Clear();

                foreach (var row in selectedRows)
                {
                    var element = GetElementFromRow(row);
                    if (element != null)
                    {
                        copiedElements.Add(element.Clone());
                    }
                }

                SetStatus($"Copied {copiedElements.Count} element(s) to clipboard");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Copy error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private CostElement GetElementFromRow(DataGridViewRow row)
        {
            var boundItem = row.DataBoundItem;

            if (currentViewMode == ViewMode.WbsHierarchy)
            {
                if (boundItem is WbsDisplayItem wbsItem)
                    return wbsItem.Element;
            }
            else
            {
                if (boundItem is CostElement element)
                    return element;
            }

            return null;
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
                int pastedCount = 0;

                foreach (var copiedElement in copiedElements)
                {
                    var newElement = copiedElement.Clone();
                    newElement.Id = GetNextAvailableId().ToString();
                    newElement.Ident = Guid.NewGuid().ToString().Replace("-", "");
                    elements.Add(newElement);
                    pastedCount++;
                }

                RefreshGrid();
                SetStatus($"Pasted {pastedCount} element(s)");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Paste error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!isRecordingChanges) return;

            var element = GetElementAtRow(e.RowIndex);
            if (element == null) return;

            var row = dataGridView.Rows[e.RowIndex];
            var column = dataGridView.Columns[e.ColumnIndex];
            var propertyName = column.DataPropertyName;

            if (string.IsNullOrEmpty(propertyName)) return;

            var property = typeof(CostElement).GetProperty(propertyName);
            if (property == null || !property.CanWrite) return;

            // Get old value from tracking dictionary
            string key = $"{e.RowIndex}_{e.ColumnIndex}";

            // If we don't have an old value tracked, skip this change
            // (it's likely a programmatic change, not user edit)
            if (!cellOldValues.ContainsKey(key))
            {
                return;
            }

            object oldValue = cellOldValues[key];

            // Get new value from cell
            object newValue = row.Cells[e.ColumnIndex].Value;

            // Convert and set the new value
            try
            {
                object convertedValue = ConvertValue(newValue?.ToString() ?? "", property.PropertyType);

                // Only record change if value actually changed
                if (!Equals(oldValue, convertedValue))
                {
                    // Create change record for undo/redo
                    var changeSet = new ChangeSet($"Edit {column.HeaderText}");
                    changeSet.AddChange(new CellChange(
                        e.RowIndex,
                        column.Name,
                        propertyName,
                        oldValue,
                        convertedValue,
                        element
                    ));

                    undoRedoManager.RecordChange(changeSet);
                    UpdateUndoRedoButtons();

                    // Apply the value
                    property.SetValue(element, convertedValue);
                }
            }
            catch (Exception ex)
            {
                // Restore old value if conversion fails
                row.Cells[e.ColumnIndex].Value = oldValue;
                MessageBox.Show($"Invalid value: {ex.Message}", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                // Clean up tracking dictionary
                cellOldValues.Remove(key);
            }

            // Refresh row to update calculated fields
            dataGridView.InvalidateRow(e.RowIndex);
        }

        private void UpdateStatus()
        {
            string viewMode = currentViewMode == ViewMode.WbsHierarchy ? "WBS" : "Flat";
            statusLabel.Text = $"Elements: {elements.Count} | View: {viewMode} | File: {(string.IsNullOrEmpty(currentFilePath) ? "None" : Path.GetFileName(currentFilePath))}";
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
                        undoRedoManager.Clear(); // Clear undo/redo when loading new file
                        UpdateUndoRedoButtons();
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
            var element = GetElementAtCurrentRow();
            if (element == null)
            {
                MessageBox.Show("Please select an element to edit.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int nextId = GetNextAvailableId();
            int elementIndex = elements.IndexOf(element);

            using (var form = new ElementEditForm(element, nextId))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    elements[elementIndex] = form.CostElement;
                    RefreshGrid();
                    SetStatus("Element updated");
                }
            }
        }

        private void DeleteElement(object sender, EventArgs e)
        {
            var element = GetElementAtCurrentRow();
            if (element == null)
            {
                MessageBox.Show("Please select an element to delete.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show("Are you sure you want to delete this element?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                elements.Remove(element);
                RefreshGrid();
                SetStatus("Element deleted");
            }
        }

        private void ShowAbout(object sender, EventArgs e)
        {
            MessageBox.Show(
                "AVA XML Editor - SPEC Edition\n\n" +
                "Features:\n" +
                "- SPEC parameter fields for property generation\n" +
                "- Multiple catalog support (up to 4)\n" +
                "- Auto-calculated totals\n" +
                "- WBS Hierarchy view\n" +
                "- Excel-like editing\n" +
                "- Preserves all XML data\n\n" +
                "Keyboard Shortcuts:\n" +
                "Ctrl+W - Toggle WBS View\n" +
                "Ctrl+N - Add Element\n" +
                "F2 - Edit Element\n" +
                "Delete - Delete Element\n\n" +
                "Version 3.0",
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