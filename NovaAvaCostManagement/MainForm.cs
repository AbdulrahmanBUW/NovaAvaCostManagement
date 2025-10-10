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
        private ViewMode currentViewMode = ViewMode.FlatList;
        private UndoRedoManager undoRedoManager = new UndoRedoManager();
        private Dictionary<string, object> cellOldValues = new Dictionary<string, object>();
        private bool isRecordingChanges = true;
        private string currentFilePath;

        private DataGridView dataGridView;
        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripButton btnToggleWbs;
        private ToolStripButton btnUndo;
        private ToolStripButton btnRedo;

        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        #region Initialization

        private void InitializeCustomComponents()
        {
            ConfigureMainForm();
            CreateStatusStrip();
            CreateToolStrip();
            CreateMenuStrip();
            CreateMainGrid();
        }

        private void ConfigureMainForm()
        {
            this.Size = new Size(1600, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "AVA XML Editor - SPEC Edition";
            this.MinimumSize = new Size(1200, 600);
            this.BackColor = Color.White;
        }

        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready")
            {
                Spring = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
            statusStrip.Items.Add(statusLabel);
            this.Controls.Add(statusStrip);
        }

        private void CreateToolStrip()
        {
            toolStrip = new ToolStrip();
            toolStrip.ImageScalingSize = new Size(24, 24);

            toolStrip.Items.Add(new ToolStripButton("Open", null, OpenFile));
            toolStrip.Items.Add(new ToolStripButton("Save", null, SaveFile));
            toolStrip.Items.Add(new ToolStripSeparator());

            btnUndo = new ToolStripButton("Undo", null, (s, e) => PerformUndo()) { Enabled = false };
            toolStrip.Items.Add(btnUndo);

            btnRedo = new ToolStripButton("Redo", null, (s, e) => PerformRedo()) { Enabled = false };
            toolStrip.Items.Add(btnRedo);

            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Add", null, AddElement));
            toolStrip.Items.Add(new ToolStripButton("Edit", null, EditElement));
            toolStrip.Items.Add(new ToolStripButton("Delete", null, DeleteElement));
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Copy", null, CopyElements));
            toolStrip.Items.Add(new ToolStripButton("Paste", null, PasteElements));
            toolStrip.Items.Add(new ToolStripSeparator());

            btnToggleWbs = new ToolStripButton("WBS View", null, ToggleWbsView) { CheckOnClick = true };
            toolStrip.Items.Add(btnToggleWbs);

            this.Controls.Add(toolStrip);
        }

        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip();

            CreateFileMenu();
            CreateEditMenu();
            CreateViewMenu();
            CreateHelpMenu();

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void CreateFileMenu()
        {
            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add("&Open AVA XML...", null, OpenFile);
            fileMenu.DropDownItems.Add("&Save AVA XML...", null, SaveFile);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("E&xit", null, (s, e) => this.Close());
            menuStrip.Items.Add(fileMenu);
        }

        private void CreateEditMenu()
        {
            var editMenu = new ToolStripMenuItem("&Edit");

            AddMenuItem(editMenu, "&Undo", Keys.Control | Keys.Z, (s, e) => PerformUndo());
            AddMenuItem(editMenu, "&Redo", Keys.Control | Keys.Y, (s, e) => PerformRedo());
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            AddMenuItem(editMenu, "&Add Element", Keys.Control | Keys.N, AddElement);
            AddMenuItem(editMenu, "&Edit Element", Keys.F2, EditElement);
            AddMenuItem(editMenu, "&Delete Element", Keys.Delete, DeleteElement);

            menuStrip.Items.Add(editMenu);
        }

        private void CreateViewMenu()
        {
            var viewMenu = new ToolStripMenuItem("&View");
            AddMenuItem(viewMenu, "&WBS Hierarchy", Keys.Control | Keys.W, ToggleWbsView);
            AddMenuItem(viewMenu, "&Flat List", Keys.Control | Keys.L, ToggleWbsView);
            menuStrip.Items.Add(viewMenu);
        }

        private void CreateHelpMenu()
        {
            var helpMenu = new ToolStripMenuItem("&Help");
            helpMenu.DropDownItems.Add("&About", null, ShowAbout);
            menuStrip.Items.Add(helpMenu);
        }

        private void AddMenuItem(ToolStripMenuItem parent, string text, Keys shortcut, EventHandler handler)
        {
            var item = new ToolStripMenuItem(text, null, handler) { ShortcutKeys = shortcut };
            parent.DropDownItems.Add(item);
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
                SelectionMode = DataGridViewSelectionMode.CellSelect,
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

            GridHelper.SetupColumns(dataGridView);
            CreateContextMenu();
            AttachGridEventHandlers();

            this.Controls.Add(dataGridView);
        }

        private void AttachGridEventHandlers()
        {
            dataGridView.KeyDown += DataGridView_KeyDown;
            dataGridView.CellValueChanged += DataGridView_CellValueChanged;
            dataGridView.CellBeginEdit += DataGridView_CellBeginEdit;
            dataGridView.CellDoubleClick += DataGridView_CellDoubleClick;
            dataGridView.CellMouseDown += DataGridView_CellMouseDown;
            dataGridView.CellFormatting += DataGridView_CellFormatting;
        }

        private void CreateContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Opening += ContextMenu_Opening;

            contextMenu.Items.Add("Copy Cells (Ctrl+C)", null, (s, e) => CopyCellsToClipboard());
            contextMenu.Items.Add("Paste Cells (Ctrl+V)", null, (s, e) => PasteCellsFromClipboard());
            contextMenu.Items.Add("Clear Cells (Del)", null, (s, e) => ClearSelectedCells());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Copy Elements", null, CopyElements);
            contextMenu.Items.Add("Paste Elements at End", null, PasteElements);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Edit Element (Ctrl+Double-Click)", null, EditElement);
            contextMenu.Items.Add("View Full Text", null, ViewFullText);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Delete Element", null, DeleteElement);

            dataGridView.ContextMenuStrip = contextMenu;
        }

        #endregion

        #region Event Handlers

        private void ContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var contextMenu = (ContextMenuStrip)sender;
            bool hasCellSelection = dataGridView.SelectedCells.Count > 0;

            contextMenu.Items[0].Enabled = hasCellSelection;
            contextMenu.Items[1].Enabled = hasCellSelection;
            contextMenu.Items[2].Enabled = hasCellSelection;
        }

        private void DataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && Control.ModifierKeys == Keys.Control)
            {
                EditElement(sender, e);
            }
        }

        private void DataGridView_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                if (!dataGridView[e.ColumnIndex, e.RowIndex].Selected)
                {
                    dataGridView.CurrentCell = dataGridView[e.ColumnIndex, e.RowIndex];
                }
            }
        }

        private void DataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var element = GridHelper.GetElementAtRow(dataGridView, e.RowIndex, currentViewMode);
            if (element == null)
                return;

            var column = dataGridView.Columns[e.ColumnIndex];
            if (column.ReadOnly)
                return;

            var propertyName = column.DataPropertyName;
            if (string.IsNullOrEmpty(propertyName))
                return;

            var property = typeof(CostElement).GetProperty(propertyName);
            if (property == null || !property.CanRead)
                return;

            string key = $"{e.RowIndex}_{e.ColumnIndex}";
            cellOldValues[key] = property.GetValue(element);
        }

        private void DataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (!isRecordingChanges)
                return;

            var element = GridHelper.GetElementAtRow(dataGridView, e.RowIndex, currentViewMode);
            if (element == null)
                return;

            var row = dataGridView.Rows[e.RowIndex];
            var column = dataGridView.Columns[e.ColumnIndex];
            var propertyName = column.DataPropertyName;

            if (string.IsNullOrEmpty(propertyName))
                return;

            var property = typeof(CostElement).GetProperty(propertyName);
            if (property == null || !property.CanWrite)
                return;

            string key = $"{e.RowIndex}_{e.ColumnIndex}";
            if (!cellOldValues.ContainsKey(key))
                return;

            object oldValue = cellOldValues[key];
            object newValue = row.Cells[e.ColumnIndex].Value;

            try
            {
                object convertedValue = ClipboardHelper.ConvertValue(newValue?.ToString() ?? "", property.PropertyType);

                if (!Equals(oldValue, convertedValue))
                {
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
                    property.SetValue(element, convertedValue);
                }
            }
            catch (Exception ex)
            {
                row.Cells[e.ColumnIndex].Value = oldValue;
                MessageBox.Show($"Invalid value: {ex.Message}", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                cellOldValues.Remove(key);
            }

            dataGridView.InvalidateRow(e.RowIndex);
        }

        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            var row = dataGridView.Rows[e.RowIndex];
            var columnName = dataGridView.Columns[e.ColumnIndex].Name;
            var boundItem = row.DataBoundItem;

            CostElement element = null;

            if (currentViewMode == ViewMode.WbsHierarchy)
            {
                if (boundItem is WbsDisplayItem wbsItem)
                {
                    element = wbsItem.Element;
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

            if (element == null)
                return;

            FormatCellContent(element, columnName, e);
        }

        private void FormatCellContent(CostElement element, string columnName, DataGridViewCellFormattingEventArgs e)
        {
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
            else if (columnName == "colTotal")
            {
                e.Value = element.UpResult.ToString("F3");
                e.CellStyle.BackColor = Color.FromArgb(230, 255, 230);
                e.CellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold);
                e.FormattingApplied = true;
            }
            else if (columnName == "colText" || columnName == "colLongText")
            {
                FormatTextContent(e, columnName);
            }
        }

        private void FormatTextContent(DataGridViewCellFormattingEventArgs e, string columnName)
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

        private void DataGridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (HandleKeyboardShortcut(e))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private bool HandleKeyboardShortcut(KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z)
            {
                PerformUndo();
                return true;
            }
            else if (e.Control && e.KeyCode == Keys.Y)
            {
                PerformRedo();
                return true;
            }
            else if (e.Control && e.KeyCode == Keys.C && !dataGridView.IsCurrentCellInEditMode)
            {
                if (dataGridView.SelectedRows.Count > 0)
                    CopyElements(null, e);
                else
                    CopyCellsToClipboard();
                return true;
            }
            else if (e.Control && e.KeyCode == Keys.V && !dataGridView.IsCurrentCellInEditMode)
            {
                PasteCellsFromClipboard();
                return true;
            }
            else if (e.KeyCode == Keys.Delete && !dataGridView.IsCurrentCellInEditMode)
            {
                if (dataGridView.SelectedRows.Count > 0)
                    DeleteElement(null, e);
                else
                    ClearSelectedCells();
                return true;
            }
            else if (e.Control && e.KeyCode == Keys.N)
            {
                AddElement(null, e);
                return true;
            }
            else if (e.KeyCode == Keys.F2 && !dataGridView.IsCurrentCellInEditMode)
            {
                EditElement(null, e);
                return true;
            }
            else if (e.Control && e.KeyCode == Keys.W)
            {
                ToggleWbsView(null, e);
                return true;
            }

            return false;
        }

        private void MainForm_Load_1(object sender, EventArgs e)
        {
        }

        #endregion

        #region Undo/Redo Operations

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

        #endregion

        #region Clipboard Operations

        private void CopyCellsToClipboard()
        {
            ClipboardHelper.CopyCellsToClipboard(dataGridView);
            SetStatus($"Copied {dataGridView.SelectedCells.Count} cell(s)");
        }

        private void PasteCellsFromClipboard()
        {
            int pastedCells = ClipboardHelper.PasteCellsFromClipboard(
                dataGridView, currentViewMode, undoRedoManager, ref isRecordingChanges);

            if (pastedCells > 0)
            {
                UpdateUndoRedoButtons();
                SetStatus($"Pasted {pastedCells} cell(s)");
            }
        }

        private void ClearSelectedCells()
        {
            int initialCount = dataGridView.SelectedCells.Count;
            ClipboardHelper.ClearSelectedCells(dataGridView, currentViewMode, undoRedoManager);
            UpdateUndoRedoButtons();
            RefreshGrid();
            SetStatus($"Cleared {initialCount} cell(s)");
        }

        private void CopyElements(object sender, EventArgs e)
        {
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

            copiedElements.Clear();
            foreach (var row in selectedRows)
            {
                var element = GridHelper.GetElementFromRow(row, currentViewMode);
                if (element != null)
                {
                    copiedElements.Add(element.Clone());
                }
            }

            SetStatus($"Copied {copiedElements.Count} element(s) to clipboard");
        }

        private void PasteElements(object sender, EventArgs e)
        {
            if (copiedElements.Count == 0)
            {
                MessageBox.Show("No elements copied to paste.", "No Elements",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int pastedCount = 0;
            int firstNewElementIndex = elements.Count;

            foreach (var copiedElement in copiedElements)
            {
                var newElement = copiedElement.Clone();
                newElement.Id = GetNextAvailableId().ToString();
                newElement.Ident = Guid.NewGuid().ToString().Replace("-", "");
                elements.Add(newElement);
                pastedCount++;
            }

            RefreshGrid();

            if (currentViewMode == ViewMode.FlatList && dataGridView.Rows.Count > firstNewElementIndex)
            {
                dataGridView.ClearSelection();
                dataGridView.FirstDisplayedScrollingRowIndex = Math.Max(0, firstNewElementIndex);
                dataGridView.Rows[firstNewElementIndex].Selected = true;
                dataGridView.CurrentCell = dataGridView.Rows[firstNewElementIndex].Cells[0];
            }

            SetStatus($"Pasted {pastedCount} element(s)");
        }

        #endregion

        #region View Management

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
                var wbsTree = WbsBuilder.BuildWbsTree(elements);
                var displayList = WbsNode.FlattenToDisplayList(wbsTree);
                var bindingList = new BindingList<WbsDisplayItem>(displayList);
                dataGridView.DataSource = bindingList;
            }
            else
            {
                var bindingList = new BindingList<CostElement>(elements);
                dataGridView.DataSource = bindingList;
            }

            UpdateStatus();
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
            return GridHelper.GetElementFromRow(row, currentViewMode);
        }

        #endregion

        #region File Operations

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
                        undoRedoManager.Clear();
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

        #endregion

        #region Element CRUD Operations

        private void AddElement(object sender, EventArgs e)
        {
            int nextId = GetNextAvailableId();

            using (var form = new ElementEditForm(null, nextId))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    elements.Add(form.CostElement);
                    RefreshGrid();

                    if (currentViewMode == ViewMode.FlatList && dataGridView.Rows.Count > 0)
                    {
                        int lastRowIndex = dataGridView.Rows.Count - 1;
                        dataGridView.ClearSelection();
                        dataGridView.FirstDisplayedScrollingRowIndex = Math.Max(0, lastRowIndex);
                        dataGridView.Rows[lastRowIndex].Selected = true;
                        dataGridView.CurrentCell = dataGridView.Rows[lastRowIndex].Cells[0];
                    }

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

        #endregion

        #region Helper Methods

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

        private void UpdateStatus()
        {
            string viewMode = currentViewMode == ViewMode.WbsHierarchy ? "WBS" : "Flat";
            statusLabel.Text = $"Elements: {elements.Count} | View: {viewMode} | File: {(string.IsNullOrEmpty(currentFilePath) ? "None" : Path.GetFileName(currentFilePath))}";
        }

        private void SetStatus(string message)
        {
            statusLabel.Text = message;
            Application.DoEvents();
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

        #endregion
    }
}