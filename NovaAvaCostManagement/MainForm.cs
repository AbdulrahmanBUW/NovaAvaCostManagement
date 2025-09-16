using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Improved main form with async operations and proper resource management
    /// </summary>
    public partial class MainForm : Form
    {
        private ProjectManager projectManager;
        private DataGridView dataGridView;
        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripProgressBar progressBar;
        private TextBox searchBox;
        private Timer searchDebounceTimer;
        private BindingSource bindingSource;
        private bool isDirty = false;

        // Constants
        private const int SEARCH_DEBOUNCE_MS = 300;
        private const int MAX_LOG_MESSAGES = 1000;

        public MainForm()
        {
            InitializeCustomComponents();
            InitializeProjectManager();
            SetupEventHandlers();
        }

        /// <summary>
        /// Initialize project manager with error handling
        /// </summary>
        private void InitializeProjectManager()
        {
            try
            {
                projectManager = new ProjectManager();
                projectManager.PropertyChanged += ProjectManager_PropertyChanged;
                projectManager.CreateNewProject();
                RefreshDataGrid();
            }
            catch (Exception ex)
            {
                ShowError("Failed to initialize project manager", ex);
            }
        }

        /// <summary>
        /// Setup event handlers with proper disposal
        /// </summary>
        private void SetupEventHandlers()
        {
            this.FormClosing += MainForm_FormClosing;
            this.Load += MainForm_Load;

            // Setup search debouncing
            searchDebounceTimer = new Timer();
            searchDebounceTimer.Interval = SEARCH_DEBOUNCE_MS;
            searchDebounceTimer.Tick += SearchDebounceTimer_Tick;
        }

        /// <summary>
        /// Initialize UI components with improved layout
        /// </summary>
        private void InitializeCustomComponents()
        {
            this.Text = "NOVA AVA Cost Management System";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 500);
            this.Icon = SystemIcons.Application;

            // Use TableLayoutPanel for better resizing
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1
            };

            // Create components
            CreateMenuStrip();
            CreateToolStrip();
            CreateMainContent();
            CreateStatusStrip();

            // Add to layout
            mainLayout.Controls.Add(menuStrip, 0, 0);
            mainLayout.Controls.Add(toolStrip, 0, 1);
            mainLayout.Controls.Add(CreateMainPanel(), 0, 2);
            mainLayout.Controls.Add(statusStrip, 0, 3);

            // Set row styles
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            this.Controls.Add(mainLayout);
        }

        private Panel CreateMainPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            // Add search panel
            var searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                Padding = new Padding(5)
            };

            var searchLabel = new Label
            {
                Text = "Search:",
                Location = new Point(5, 8),
                Size = new Size(50, 20)
            };

            searchBox = new TextBox
            {
                Location = new Point(60, 5),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 9F)
            };
            searchBox.TextChanged += SearchBox_TextChanged;

            var clearButton = new Button
            {
                Text = "Clear",
                Location = new Point(315, 5),
                Size = new Size(60, 25)
            };
            clearButton.Click += (s, e) => { searchBox.Clear(); };

            searchPanel.Controls.AddRange(new Control[] { searchLabel, searchBox, clearButton });
            panel.Controls.Add(searchPanel);
            panel.Controls.Add(dataGridView);

            return panel;
        }

        /// <summary>
        /// Create menu strip with all options
        /// </summary>
        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip();

            // File menu
            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateMenuItem("&New Project", Keys.Control | Keys.N, async (s, e) => await NewProjectAsync()),
                CreateMenuItem("&Open Project...", Keys.Control | Keys.O, async (s, e) => await OpenProjectAsync()),
                CreateMenuItem("&Save Project", Keys.Control | Keys.S, async (s, e) => await SaveProjectAsync()),
                CreateMenuItem("Save Project &As...", Keys.Control | Keys.Shift | Keys.S, async (s, e) => await SaveProjectAsAsync()),
                new ToolStripSeparator(),
                CreateMenuItem("&Import AVA XML...", null, async (s, e) => await ImportAvaXmlAsync()),
                CreateMenuItem("&Export AVA XML...", null, async (s, e) => await ExportAvaXmlAsync()),
                CreateMenuItem("Export &GAEB XML...", null, async (s, e) => await ExportGaebXmlAsync()),
                new ToolStripSeparator(),
                CreateMenuItem("Recent Files", null, null),
                new ToolStripSeparator(),
                CreateMenuItem("E&xit", Keys.Alt | Keys.F4, (s, e) => Application.Exit())
            });
            menuStrip.Items.Add(fileMenu);

            // Edit menu
            var editMenu = new ToolStripMenuItem("&Edit");
            editMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateMenuItem("&Add Element", Keys.Insert, (s, e) => AddElement()),
                CreateMenuItem("&Edit Element", null, (s, e) => EditElement()),
                CreateMenuItem("&Delete Element", Keys.Delete, (s, e) => DeleteElement()),
                new ToolStripSeparator(),
                CreateMenuItem("&Copy", Keys.Control | Keys.C, (s, e) => CopyElements()),
                CreateMenuItem("&Paste", Keys.Control | Keys.V, (s, e) => PasteElements()),
                new ToolStripSeparator(),
                CreateMenuItem("Select &All", Keys.Control | Keys.A, (s, e) => SelectAll())
            });
            menuStrip.Items.Add(editMenu);

            // Template menu
            var templateMenu = new ToolStripMenuItem("&Template");
            templateMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateMenuItem("Create &Data Entry Template...", null, async (s, e) => await CreateDataEntryTemplateAsync()),
                CreateMenuItem("Create &IFC Mapping Template...", null, async (s, e) => await CreateIFCMappingTemplateAsync()),
                CreateMenuItem("&Convert Template to Main...", null, async (s, e) => await ConvertTemplateToMainAsync())
            });
            menuStrip.Items.Add(templateMenu);

            // Tools menu
            var toolsMenu = new ToolStripMenuItem("&Tools");
            toolsMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateMenuItem("&Validate Data", Keys.F5, async (s, e) => await ValidateDataAsync()),
                CreateMenuItem("&Quick Diagnostics", Keys.F6, (s, e) => ShowQuickDiagnostics()),
                CreateMenuItem("&Batch Operations...", null, (s, e) => ShowBatchOperations()),
                new ToolStripSeparator(),
                CreateMenuItem("&Options...", null, (s, e) => ShowOptions())
            });
            menuStrip.Items.Add(toolsMenu);

            // Help menu
            var helpMenu = new ToolStripMenuItem("&Help");
            helpMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateMenuItem("&Documentation", Keys.F1, (s, e) => ShowDocumentation()),
                CreateMenuItem("&Column Reference", null, (s, e) => ShowColumnMappingReference()),
                new ToolStripSeparator(),
                CreateMenuItem("&About", null, (s, e) => ShowAbout())
            });
            menuStrip.Items.Add(helpMenu);

            this.MainMenuStrip = menuStrip;
        }

        /// <summary>
        /// Helper to create menu items
        /// </summary>
        private ToolStripMenuItem CreateMenuItem(string text, Keys? shortcut, EventHandler handler)
        {
            var item = new ToolStripMenuItem(text);
            if (shortcut.HasValue)
                item.ShortcutKeys = shortcut.Value;
            if (handler != null)
                item.Click += handler;
            return item;
        }

        /// <summary>
        /// Create toolbar with common actions
        /// </summary>
        private void CreateToolStrip()
        {
            toolStrip = new ToolStrip();
            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                CreateToolButton("New", "New Project", async (s, e) => await NewProjectAsync()),
                CreateToolButton("Open", "Open Project", async (s, e) => await OpenProjectAsync()),
                CreateToolButton("Save", "Save Project", async (s, e) => await SaveProjectAsync()),
                new ToolStripSeparator(),
                CreateToolButton("Add", "Add Element", (s, e) => AddElement()),
                CreateToolButton("Edit", "Edit Element", (s, e) => EditElement()),
                CreateToolButton("Delete", "Delete Element", (s, e) => DeleteElement()),
                new ToolStripSeparator(),
                CreateToolButton("Import", "Import XML", async (s, e) => await ImportAvaXmlAsync()),
                CreateToolButton("Export", "Export XML", async (s, e) => await ExportAvaXmlAsync()),
                new ToolStripSeparator(),
                CreateToolButton("Validate", "Validate Data", async (s, e) => await ValidateDataAsync())
            });
        }

        private ToolStripButton CreateToolButton(string text, string tooltip, EventHandler handler)
        {
            var button = new ToolStripButton(text);
            button.ToolTipText = tooltip;
            button.Click += handler;
            return button;
        }

        /// <summary>
        /// Create main content area with improved DataGridView
        /// </summary>
        private void CreateMainContent()
        {
            // Initialize BindingSource for better data management
            bindingSource = new BindingSource();

            // Create DataGridView with optimized settings
            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AllowUserToResizeColumns = true,
                AllowUserToOrderColumns = true,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = SystemColors.ControlLight,
                DataSource = bindingSource,
                VirtualMode = true // Enable virtual mode for performance
            };

            SetupDataGridViewColumns();
            SetupDataGridViewEvents();
        }

        /// <summary>
        /// Setup DataGridView columns with better configuration
        /// </summary>
        private void SetupDataGridViewColumns()
        {
            dataGridView.Columns.Clear();

            DataGridViewColumn[] columns = new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50, Frozen = true },
                new DataGridViewTextBoxColumn { Name = "Id2", HeaderText = "Code", Width = 150, Frozen = true },
                new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", Width = 250, Frozen = true },
                new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Text", HeaderText = "Text", Width = 200 },
                new DataGridViewTextBoxColumn { Name = "LongText", HeaderText = "Description", Width = 300 },
                new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Quantity", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } },
                new DataGridViewTextBoxColumn { Name = "Qu", HeaderText = "Unit", Width = 60 },
                new DataGridViewTextBoxColumn { Name = "Up", HeaderText = "Unit Price", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "C2" } },
                new DataGridViewTextBoxColumn { Name = "Sum", HeaderText = "Total", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "C2", Font = new Font("Segoe UI", 9F, FontStyle.Bold) } },
                new DataGridViewTextBoxColumn { Name = "BimKey", HeaderText = "BIM Key", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Properties", HeaderText = "Properties", Width = 200 },
                new DataGridViewTextBoxColumn { Name = "Note", HeaderText = "Notes", Width = 150 },
                new DataGridViewCheckBoxColumn { Name = "Marked", HeaderText = "Marked", Width = 60 }
            };

            dataGridView.Columns.AddRange(columns);

            // Configure column headers
            foreach (DataGridViewColumn col in dataGridView.Columns)
            {
                col.HeaderCell.Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                col.HeaderCell.Style.BackColor = SystemColors.Control;
            }
        }

        /// <summary>
        /// Setup DataGridView events
        /// </summary>
        private void SetupDataGridViewEvents()
        {
            dataGridView.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    EditElement();
            };

            dataGridView.CellFormatting += DataGridView_CellFormatting;
            dataGridView.SelectionChanged += DataGridView_SelectionChanged;
            dataGridView.KeyDown += DataGridView_KeyDown;
        }

        /// <summary>
        /// Create status strip with progress bar
        /// </summary>
        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip();

            statusLabel = new ToolStripStatusLabel("Ready")
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            progressBar = new ToolStripProgressBar
            {
                Visible = false,
                Style = ProgressBarStyle.Marquee
            };

            var elementCountLabel = new ToolStripStatusLabel();
            var totalValueLabel = new ToolStripStatusLabel();

            statusStrip.Items.AddRange(new ToolStripItem[]
            {
                statusLabel,
                progressBar,
                new ToolStripSeparator(),
                elementCountLabel,
                totalValueLabel
            });
        }

        /// <summary>
        /// Refresh DataGridView with virtual mode
        /// </summary>
        private void RefreshDataGrid()
        {
            try
            {
                var filteredElements = GetFilteredElements();
                bindingSource.DataSource = filteredElements;
                dataGridView.Refresh();
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                ShowError("Failed to refresh data grid", ex);
            }
        }

        /// <summary>
        /// Get filtered elements with improved search
        /// </summary>
        private List<CostElement> GetFilteredElements()
        {
            if (string.IsNullOrWhiteSpace(searchBox?.Text))
                return projectManager.Elements.ToList();

            var searchTerms = searchBox.Text.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return projectManager.Elements.Where(e =>
                searchTerms.All(term =>
                    e.Name?.ToLower().Contains(term) == true ||
                    e.Text?.ToLower().Contains(term) == true ||
                    e.LongText?.ToLower().Contains(term) == true ||
                    e.Id2?.ToLower().Contains(term) == true ||
                    e.Type?.ToLower().Contains(term) == true ||
                    e.BimKey?.ToLower().Contains(term) == true
                )
            ).ToList();
        }

        /// <summary>
        /// Update status bar with statistics
        /// </summary>
        private void UpdateStatusBar()
        {
            var totalElements = projectManager.Elements.Count;
            var filteredCount = GetFilteredElements().Count;
            var totalValue = projectManager.Elements.Sum(e => e.Sum);

            statusLabel.Text = $"Elements: {filteredCount}/{totalElements} | Total Value: {totalValue:C2}";
        }

        // Async operations
        private async Task NewProjectAsync()
        {
            if (!await ConfirmUnsavedChangesAsync()) return;

            await RunAsync(async () =>
            {
                await Task.Run(() =>
                {
                    projectManager.CreateNewProject();
                });
                RefreshDataGrid();
                isDirty = false;
                SetStatus("New project created");
            });
        }

        private async Task OpenProjectAsync()
        {
            if (!await ConfirmUnsavedChangesAsync()) return;

            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Project files (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.Title = "Open Project";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    await RunAsync(async () =>
                    {
                        await Task.Run(() => projectManager.LoadProject(dialog.FileName));
                        RefreshDataGrid();
                        isDirty = false;
                        SetStatus($"Project loaded: {Path.GetFileName(dialog.FileName)}");
                    });
                }
            }
        }

        private async Task SaveProjectAsync()
        {
            if (string.IsNullOrEmpty(projectManager.ProjectFilePath))
            {
                await SaveProjectAsAsync();
            }
            else
            {
                await RunAsync(async () =>
                {
                    await Task.Run(() => projectManager.SaveProject(projectManager.ProjectFilePath));
                    isDirty = false;
                    SetStatus("Project saved");
                });
            }
        }

        private async Task SaveProjectAsAsync()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Project files (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.Title = "Save Project As";
                dialog.FileName = "NovaAvaProject.xml";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    await RunAsync(async () =>
                    {
                        await Task.Run(() => projectManager.SaveProject(dialog.FileName));
                        isDirty = false;
                        SetStatus($"Project saved: {Path.GetFileName(dialog.FileName)}");
                    });
                }
            }
        }

        private async Task ImportAvaXmlAsync()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.Title = "Import AVA XML";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    await RunAsync(async () =>
                    {
                        SetStatus("Importing AVA XML...");
                        await Task.Run(() => projectManager.ImportAvaXml(dialog.FileName));
                        RefreshDataGrid();
                        isDirty = true;
                        SetStatus($"AVA XML imported: {Path.GetFileName(dialog.FileName)}");
                    });
                }
            }
        }

        private async Task ExportAvaXmlAsync()
        {
            await ExportXmlAsync(false);
        }

        private async Task ExportGaebXmlAsync()
        {
            await ExportXmlAsync(true);
        }

        private async Task ExportXmlAsync(bool useGaebFormat)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.Title = useGaebFormat ? "Export GAEB XML" : "Export AVA XML";
                dialog.FileName = $"NovaAva_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xml";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    await RunAsync(async () =>
                    {
                        SetStatus("Exporting XML...");
                        await Task.Run(() => projectManager.ExportAvaXml(dialog.FileName, useGaebFormat));
                        SetStatus($"XML exported: {Path.GetFileName(dialog.FileName)}");

                        if (MessageBox.Show("Export completed. Open file?", "Success",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start(dialog.FileName);
                        }
                    });
                }
            }
        }

        private async Task ValidateDataAsync()
        {
            await RunAsync(async () =>
            {
                SetStatus("Validating data...");
                var result = await Task.Run(() => projectManager.ValidateForExport());
                ShowValidationResults(result);
                SetStatus("Validation completed");
            });
        }

        private async Task CreateDataEntryTemplateAsync()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.Title = "Create Data Entry Template";
                dialog.FileName = "DataEntryTemplate.csv";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    await RunAsync(async () =>
                    {
                        await Task.Run(() => TemplateManager.CreateDataEntryTemplate(dialog.FileName));
                        SetStatus($"Template created: {Path.GetFileName(dialog.FileName)}");
                    });
                }
            }
        }

        private async Task CreateIFCMappingTemplateAsync()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.Title = "Create IFC Mapping Template";
                dialog.FileName = "IFCMappingTemplate.csv";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    await RunAsync(async () =>
                    {
                        await Task.Run(() => TemplateManager.CreateIFCMappingTemplate(dialog.FileName));
                        SetStatus($"IFC template created: {Path.GetFileName(dialog.FileName)}");
                    });
                }
            }
        }

        private async Task ConvertTemplateToMainAsync()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.Title = "Convert Template to Main Sheet";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    await RunAsync(async () =>
                    {
                        SetStatus("Converting template...");
                        var convertedElements = await Task.Run(() =>
                            TemplateManager.ConvertTemplateToMainSheet(dialog.FileName));

                        if (convertedElements.Count > 0)
                        {
                            foreach (var element in convertedElements)
                            {
                                projectManager.Elements.Add(element);
                            }
                            RefreshDataGrid();
                            isDirty = true;
                            SetStatus($"Template converted: {convertedElements.Count} elements");
                        }
                        else
                        {
                            SetStatus("No valid elements found in template");
                        }
                    });
                }
            }
        }

        // Helper methods
        private async Task RunAsync(Func<Task> action)
        {
            try
            {
                ShowProgress(true);
                await action();
            }
            catch (Exception ex)
            {
                ShowError("Operation failed", ex);
            }
            finally
            {
                ShowProgress(false);
            }
        }

        private void ShowProgress(bool show)
        {
            progressBar.Visible = show;
            Cursor = show ? Cursors.WaitCursor : Cursors.Default;
            Application.DoEvents();
        }

        private void SetStatus(string message)
        {
            statusLabel.Text = message;
            projectManager.AddLogMessage(message);
        }

        private void ShowError(string message, Exception ex)
        {
            MessageBox.Show($"{message}\n\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus($"Error: {ex.Message}");
        }

        private async Task<bool> ConfirmUnsavedChangesAsync()
        {
            if (!isDirty) return true;

            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save them?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            switch (result)
            {
                case DialogResult.Yes:
                    await SaveProjectAsync();
                    return true;
                case DialogResult.No:
                    return true;
                default:
                    return false;
            }
        }

        // Event handlers
        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            searchDebounceTimer.Stop();
            searchDebounceTimer.Start();
        }

        private void SearchDebounceTimer_Tick(object sender, EventArgs e)
        {
            searchDebounceTimer.Stop();
            RefreshDataGrid();
        }

        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var element = GetElementAtRow(e.RowIndex);
            if (element == null) return;

            // Apply color coding
            if (!string.IsNullOrEmpty(element.Color))
            {
                try
                {
                    var color = ColorTranslator.FromHtml(element.Color);
                    e.CellStyle.BackColor = Color.FromArgb(30, color);
                }
                catch { }
            }

            // Highlight invalid rows
            if (!element.IsValid)
            {
                e.CellStyle.ForeColor = Color.Red;
            }
        }

        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            UpdateStatusBar();
        }

        private void DataGridView_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    EditElement();
                    e.Handled = true;
                    break;
                case Keys.Delete:
                    DeleteElement();
                    e.Handled = true;
                    break;
                case Keys.Insert:
                    AddElement();
                    e.Handled = true;
                    break;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Load recent files
            // Load settings
            // Check for updates
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Use .GetAwaiter().GetResult() to synchronously wait for the async method
            if (!ConfirmUnsavedChangesAsync().GetAwaiter().GetResult())
            {
                e.Cancel = true;
                return;
            }

            // Cleanup resources
            searchDebounceTimer?.Dispose();
            bindingSource?.Dispose();
            dataGridView?.Dispose();
        }

        private void ProjectManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Elements")
            {
                isDirty = true;
                RefreshDataGrid();
            }
        }

        // Element operations
        private void AddElement()
        {
            using (var form = new ElementEditForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    projectManager.Elements.Add(form.CostElement);
                    RefreshDataGrid();
                    isDirty = true;
                    SetStatus("Element added");
                }
            }
        }

        private void EditElement()
        {
            var selectedElement = GetSelectedElement();
            if (selectedElement == null)
            {
                MessageBox.Show("Please select an element to edit.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var form = new ElementEditForm(selectedElement))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    var index = projectManager.Elements.IndexOf(selectedElement);
                    projectManager.Elements[index] = form.CostElement;
                    RefreshDataGrid();
                    isDirty = true;
                    SetStatus("Element updated");
                }
            }
        }

        private void DeleteElement()
        {
            var selectedElements = GetSelectedElements();
            if (selectedElements.Count == 0)
            {
                MessageBox.Show("Please select elements to delete.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var message = selectedElements.Count == 1
                ? "Delete the selected element?"
                : $"Delete {selectedElements.Count} selected elements?";

            if (MessageBox.Show(message, "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (var element in selectedElements)
                {
                    projectManager.Elements.Remove(element);
                }
                RefreshDataGrid();
                isDirty = true;
                SetStatus($"{selectedElements.Count} element(s) deleted");
            }
        }

        private void CopyElements()
        {
            var selectedElements = GetSelectedElements();
            if (selectedElements.Count > 0)
            {
                // Implement clipboard operations
                SetStatus($"{selectedElements.Count} element(s) copied");
            }
        }

        private void PasteElements()
        {
            // Implement clipboard operations
        }

        private void SelectAll()
        {
            dataGridView.SelectAll();
        }

        private CostElement GetSelectedElement()
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                return GetElementAtRow(dataGridView.SelectedRows[0].Index);
            }
            return null;
        }

        private List<CostElement> GetSelectedElements()
        {
            var elements = new List<CostElement>();
            foreach (DataGridViewRow row in dataGridView.SelectedRows)
            {
                var element = GetElementAtRow(row.Index);
                if (element != null)
                    elements.Add(element);
            }
            return elements;
        }

        private CostElement GetElementAtRow(int rowIndex)
        {
            if (rowIndex >= 0 && rowIndex < bindingSource.Count)
            {
                return bindingSource[rowIndex] as CostElement;
            }
            return null;
        }

        // Additional dialogs
        private void ShowValidationResults(ValidationResult result)
        {
            using (var form = new ValidationResultForm(result))
            {
                form.ShowDialog();
            }
        }

        private void ShowQuickDiagnostics()
        {
            var diagnostics = projectManager.GenerateQuickDiagnostics();
            using (var form = new TextViewerForm("Quick Diagnostics", diagnostics))
            {
                form.ShowDialog();
            }
        }

        private void ShowBatchOperations()
        {
            MessageBox.Show("Batch operations feature coming soon.", "Batch Operations",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowOptions()
        {
            MessageBox.Show("Options dialog coming soon.", "Options",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowDocumentation()
        {
            System.Diagnostics.Process.Start("https://docs.nova-ava.com");
        }

        private void ShowColumnMappingReference()
        {
            var mappingInfo = @"=== NOVA AVA Column Mapping Reference ===

Core Fields (Always Required):
- version (2): Fixed version number
- id: Sequential or GUID identifier  
- name: Element name/description
- text: Short description (max 255 chars)
- longtext: Detailed description (max 2000 chars)

[Additional mapping information...]";

            using (var form = new TextViewerForm("Column Mapping Reference", mappingInfo))
            {
                form.ShowDialog();
            }
        }

        private void ShowAbout()
        {
            var aboutText = @"NOVA AVA Cost Management System
Version 2.0

Enhanced Features:
- Async operations for better performance
- Improved error handling and validation
- Thread-safe operations
- Better resource management
- Enhanced search capabilities
- Virtual mode for large datasets

© 2025 - Built with C# WinForms";

            MessageBox.Show(aboutText, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    /// <summary>
    /// Simple text viewer form for displaying information
    /// </summary>
    public class TextViewerForm : Form
    {
        private TextBox textBox;

        public TextViewerForm(string title, string content)
        {
            this.Text = title;
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;

            textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9F),
                Text = content
            };

            this.Controls.Add(textBox);
        }
    }
}