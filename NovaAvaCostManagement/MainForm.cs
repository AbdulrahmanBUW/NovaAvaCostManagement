using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Main form for NOVA AVA Cost Management application
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

        public MainForm()
        {

            InitializeCustomComponents();
            projectManager = new ProjectManager();
            projectManager.CreateNewProject();
            RefreshDataGrid();
        }

        /// <summary>
        /// Initialize custom form components
        /// </summary>
        private void InitializeCustomComponents()
        {
            this.Size = new Size(600, 650);  // Reduce height
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;  // Make resizable
            this.MaximizeBox = true;  // Allow maximize
            this.MinimizeBox = true;  // Allow minimize
            this.MinimumSize = new Size(500, 400);  // Set minimum size

            // Add scroll support
            this.AutoScroll = true;
            this.AutoScrollMinSize = new Size(580, 650);

            // Create menu strip
            CreateMenuStrip();

            // Create toolbar
            CreateToolStrip();

            // Create main content area
            CreateMainContent();

            // Create status strip
            CreateStatusStrip();
        }

        /// <summary>
        /// Create menu strip
        /// </summary>
        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip();

            // File menu
            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add("&New Project", null, (s, e) => NewProject());
            fileMenu.DropDownItems.Add("&Open Project...", null, (s, e) => OpenProject());
            fileMenu.DropDownItems.Add("&Save Project", null, (s, e) => SaveProject());
            fileMenu.DropDownItems.Add("Save Project &As...", null, (s, e) => SaveProjectAs());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("&Import AVA XML...", null, (s, e) => ImportAvaXml());
            fileMenu.DropDownItems.Add("&Export AVA XML...", null, (s, e) => ExportAvaXml());
            fileMenu.DropDownItems.Add("Export &GAEB XML...", null, (s, e) => ExportGaebXml());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("E&xit", null, (s, e) => this.Close());
            menuStrip.Items.Add(fileMenu);

            // Template menu
            var templateMenu = new ToolStripMenuItem("&Template");
            templateMenu.DropDownItems.Add("Create &Data Entry Template...", null, (s, e) => CreateDataEntryTemplate());
            templateMenu.DropDownItems.Add("Create &IFC Mapping Template...", null, (s, e) => CreateIFCMappingTemplate());
            templateMenu.DropDownItems.Add("&Convert Template to Main...", null, (s, e) => ConvertTemplateToMain());
            menuStrip.Items.Add(templateMenu);

            // Tools menu
            var toolsMenu = new ToolStripMenuItem("&Tools");
            toolsMenu.DropDownItems.Add("&Validate Data", null, (s, e) => ValidateData());
            toolsMenu.DropDownItems.Add("&Quick Diagnostics", null, (s, e) => ShowQuickDiagnostics());
            toolsMenu.DropDownItems.Add("&Column Mapping Reference", null, (s, e) => ShowColumnMappingReference());
            toolsMenu.DropDownItems.Add("View &Log Messages", null, (s, e) => ShowLogMessages());
            menuStrip.Items.Add(toolsMenu);

            // Help menu
            var helpMenu = new ToolStripMenuItem("&Help");
            helpMenu.DropDownItems.Add("&About", null, (s, e) => ShowAbout());
            menuStrip.Items.Add(helpMenu);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        /// <summary>
        /// Create toolbar
        /// </summary>
        private void CreateToolStrip()
        {
            toolStrip = new ToolStrip();
            toolStrip.Items.Add("New", null, (s, e) => NewProject());
            toolStrip.Items.Add("Open", null, (s, e) => OpenProject());
            toolStrip.Items.Add("Save", null, (s, e) => SaveProject());
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add("Add Element", null, (s, e) => AddElement());
            toolStrip.Items.Add("Edit Element", null, (s, e) => EditElement());
            toolStrip.Items.Add("Delete Element", null, (s, e) => DeleteElement());
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add("Import AVA", null, (s, e) => ImportAvaXml());
            toolStrip.Items.Add("Export AVA", null, (s, e) => ExportAvaXml());
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add("Validate", null, (s, e) => ValidateData());

            this.Controls.Add(toolStrip);
        }

        /// <summary>
        /// Create main content area
        /// </summary>
        private void CreateMainContent()
        {
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            // Search panel
            var searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30
            };

            var searchLabel = new Label
            {
                Text = "Search:",
                Location = new Point(0, 6),
                Size = new Size(50, 20)
            };
            searchPanel.Controls.Add(searchLabel);

            searchBox = new TextBox
            {
                Location = new Point(55, 3),
                Size = new Size(200, 20)
            };
            searchBox.TextChanged += SearchBox_TextChanged;
            searchPanel.Controls.Add(searchBox);

            mainPanel.Controls.Add(searchPanel);

            // DataGridView
            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AllowUserToResizeColumns = true,
                AllowUserToOrderColumns = true
            };

            SetupDataGridViewColumns();
            dataGridView.DoubleClick += (s, e) => EditElement();
            mainPanel.Controls.Add(dataGridView);

            this.Controls.Add(mainPanel);
        }

        /// <summary>
        /// Create status strip
        /// </summary>
        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip();

            statusLabel = new ToolStripStatusLabel("Ready")
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            statusStrip.Items.Add(statusLabel);

            progressBar = new ToolStripProgressBar
            {
                Visible = false
            };
            statusStrip.Items.Add(progressBar);

            this.Controls.Add(statusStrip);
        }

        /// <summary>
        /// Setup DataGridView columns
        /// </summary>
        private void SetupDataGridViewColumns()
        {
            dataGridView.Columns.Clear();

            // Enable scrolling
            dataGridView.ScrollBars = ScrollBars.Both;
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Core columns that should always be visible
            dataGridView.Columns.Add("Version", "Ver");
            dataGridView.Columns.Add("Id", "ID");
            dataGridView.Columns.Add("Id2", "Code");
            dataGridView.Columns.Add("Name", "Name");
            dataGridView.Columns.Add("Type", "Type");
            dataGridView.Columns.Add("Text", "Text");
            dataGridView.Columns.Add("LongText", "Long Text");
            dataGridView.Columns.Add("Qty", "Qty");
            dataGridView.Columns.Add("Qu", "Unit");
            dataGridView.Columns.Add("Up", "Unit Price");
            dataGridView.Columns.Add("Sum", "Total");
            dataGridView.Columns.Add("Properties", "Properties");
            dataGridView.Columns.Add("BimKey", "BIM Key");
            dataGridView.Columns.Add("Note", "Note");
            dataGridView.Columns.Add("Color", "Color");

            // Set column widths
            dataGridView.Columns["Version"].Width = 40;
            dataGridView.Columns["Id"].Width = 50;
            dataGridView.Columns["Id2"].Width = 120;
            dataGridView.Columns["Name"].Width = 200;
            dataGridView.Columns["Type"].Width = 80;
            dataGridView.Columns["Text"].Width = 150;
            dataGridView.Columns["LongText"].Width = 200;
            dataGridView.Columns["Qty"].Width = 80;
            dataGridView.Columns["Qu"].Width = 60;
            dataGridView.Columns["Up"].Width = 80;
            dataGridView.Columns["Sum"].Width = 80;
            dataGridView.Columns["Properties"].Width = 150;
            dataGridView.Columns["BimKey"].Width = 100;
            dataGridView.Columns["Note"].Width = 150;
            dataGridView.Columns["Color"].Width = 80;

            // Freeze important columns
            dataGridView.Columns["Version"].Frozen = true;
            dataGridView.Columns["Id"].Frozen = true;
            dataGridView.Columns["Id2"].Frozen = true;
            dataGridView.Columns["Name"].Frozen = true;
        }

        /// <summary>
        /// Refresh DataGridView with current data
        /// </summary>
        private void RefreshDataGrid()
        {
            dataGridView.Rows.Clear();

            var filteredElements = GetFilteredElements();

            // Debug
            MessageBox.Show($"RefreshDataGrid: {filteredElements.Count} elements to display");

            foreach (var element in filteredElements)
            {
                var rowIndex = dataGridView.Rows.Add();
                var row = dataGridView.Rows[rowIndex];

                row.Cells["Version"].Value = element.Version;
                row.Cells["Id"].Value = element.Id;
                row.Cells["Id2"].Value = element.Id2;
                row.Cells["Name"].Value = element.Name;
                row.Cells["Type"].Value = element.Type;
                row.Cells["Text"].Value = element.Text;
                row.Cells["LongText"].Value = element.LongText;
                row.Cells["Qty"].Value = element.Qty.ToString("F2");
                row.Cells["Qu"].Value = element.Qu;
                row.Cells["Up"].Value = element.Up.ToString("F2");
                row.Cells["Sum"].Value = element.Sum.ToString("F2");
                row.Cells["Properties"].Value = element.Properties;
                row.Cells["BimKey"].Value = element.BimKey;
                row.Cells["Note"].Value = element.Note;
                row.Cells["Color"].Value = element.Color;

                // Apply color coding
                if (!string.IsNullOrEmpty(element.Color))
                {
                    try
                    {
                        var color = ColorTranslator.FromHtml(element.Color);
                        row.DefaultCellStyle.BackColor = Color.FromArgb(50, color);
                    }
                    catch
                    {
                        // Ignore invalid color formats
                    }
                }

                row.Tag = element; // Store reference to the element
            }

            UpdateStatusBar();
        }

        /// <summary>
        /// Get filtered elements based on search
        /// </summary>
        private List<CostElement> GetFilteredElements()
        {
            if (string.IsNullOrWhiteSpace(searchBox?.Text))
                return projectManager.Elements;

            var searchTerm = searchBox.Text.ToLower();
            return projectManager.Elements.Where(e =>
                e.Name.ToLower().Contains(searchTerm) ||
                e.Text.ToLower().Contains(searchTerm) ||
                e.Id2.ToLower().Contains(searchTerm) ||
                e.Type.ToLower().Contains(searchTerm)
            ).ToList();
        }

        /// <summary>
        /// Update status bar
        /// </summary>
        private void UpdateStatusBar()
        {
            var totalElements = projectManager.Elements.Count;
            var filteredCount = GetFilteredElements().Count;
            var totalValue = projectManager.Elements.Sum(e => e.Sum);

            statusLabel.Text = $"Elements: {filteredCount}/{totalElements} | Total Value: {totalValue:F2}";
        }

        // Menu and toolbar event handlers
        private void NewProject()
        {
            if (ConfirmUnsavedChanges())
            {
                projectManager.CreateNewProject();
                RefreshDataGrid();
                SetStatus("New project created");
            }
        }

        private void OpenProject()
        {
            if (!ConfirmUnsavedChanges()) return;

            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Project files (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.Title = "Open Project";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        projectManager.LoadProject(dialog.FileName);
                        RefreshDataGrid();
                        SetStatus($"Project loaded: {Path.GetFileName(dialog.FileName)}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading project: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SaveProject()
        {
            if (string.IsNullOrEmpty(projectManager.ProjectFilePath))
            {
                SaveProjectAs();
            }
            else
            {
                try
                {
                    projectManager.SaveProject(projectManager.ProjectFilePath);
                    SetStatus("Project saved");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving project: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveProjectAs()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Project files (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.Title = "Save Project As";
                dialog.FileName = "NovaAvaProject.xml";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        projectManager.SaveProject(dialog.FileName);
                        SetStatus($"Project saved: {Path.GetFileName(dialog.FileName)}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving project: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ImportAvaXml()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.Title = "Import AVA XML";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        SetStatus("Importing AVA XML...");
                        ShowProgress(true);

                        // Add debugging
                        MessageBox.Show($"Before import: {projectManager.Elements.Count} elements");

                        projectManager.ImportAvaXml(dialog.FileName);

                        // Add more debugging
                        MessageBox.Show($"After import: {projectManager.Elements.Count} elements");

                        RefreshDataGrid();
                        SetStatus($"AVA XML imported: {Path.GetFileName(dialog.FileName)}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error importing AVA XML: {ex.Message}\n\nStack trace: {ex.StackTrace}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        SetStatus("Import failed");
                    }
                    finally
                    {
                        ShowProgress(false);
                    }
                }
            }
        }

        private void ExportAvaXml()
        {
            ExportXml(false);
        }

        private void ExportGaebXml()
        {
            ExportXml(true);
        }

        private void ExportXml(bool useGaebFormat)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.Title = useGaebFormat ? "Export GAEB XML" : "Export AVA XML";
                dialog.FileName = $"NovaAva_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xml";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        SetStatus("Exporting XML...");
                        ShowProgress(true);

                        projectManager.ExportAvaXml(dialog.FileName, useGaebFormat);
                        SetStatus($"XML exported: {Path.GetFileName(dialog.FileName)}");

                        // Show export preview
                        ShowExportPreview(dialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting XML: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        SetStatus("Export failed");
                    }
                    finally
                    {
                        ShowProgress(false);
                    }
                }
            }
        }

        private void CreateDataEntryTemplate()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.Title = "Create Data Entry Template";
                dialog.FileName = "DataEntryTemplate.csv";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        TemplateManager.CreateDataEntryTemplate(dialog.FileName);
                        SetStatus($"Template created: {Path.GetFileName(dialog.FileName)}");

                        var result = MessageBox.Show("Template created successfully. Would you like to open it?",
                            "Template Created", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                        if (result == DialogResult.Yes)
                        {
                            try
                            {
                                System.Diagnostics.Process.Start(dialog.FileName);
                            }
                            catch
                            {
                                MessageBox.Show($"Could not open file: {dialog.FileName}", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error creating template: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void CreateIFCMappingTemplate()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.Title = "Create IFC Mapping Template";
                dialog.FileName = "IFCMappingTemplate.csv";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        TemplateManager.CreateIFCMappingTemplate(dialog.FileName);
                        SetStatus($"IFC mapping template created: {Path.GetFileName(dialog.FileName)}");

                        var result = MessageBox.Show("IFC mapping template created successfully. Would you like to open it?",
                            "Template Created", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                        if (result == DialogResult.Yes)
                        {
                            try
                            {
                                System.Diagnostics.Process.Start(dialog.FileName);
                            }
                            catch
                            {
                                MessageBox.Show($"Could not open file: {dialog.FileName}", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error creating IFC mapping template: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ConvertTemplateToMain()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.Title = "Convert Template to Main Sheet";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        SetStatus("Converting template...");
                        ShowProgress(true);

                        var convertedElements = TemplateManager.ConvertTemplateToMainSheet(dialog.FileName);

                        if (convertedElements.Count > 0)
                        {
                            var result = MessageBox.Show(
                                $"Converted {convertedElements.Count} elements from template.\n\nChoose action:\nYes = Append to existing\nNo = Replace existing\nCancel = Cancel",
                                "Template Conversion",
                                MessageBoxButtons.YesNoCancel,
                                MessageBoxIcon.Question);

                            if (result == DialogResult.Yes) // Append
                            {
                                projectManager.Elements.AddRange(convertedElements);
                                RefreshDataGrid();
                                SetStatus($"Template converted and appended: {convertedElements.Count} elements");
                            }
                            else if (result == DialogResult.No) // Replace
                            {
                                projectManager.Elements.Clear();
                                projectManager.Elements.AddRange(convertedElements);
                                RefreshDataGrid();
                                SetStatus($"Template converted and replaced: {convertedElements.Count} elements");
                            }
                        }
                        else
                        {
                            MessageBox.Show("No valid elements found in template.", "Template Conversion",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error converting template: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        SetStatus("Template conversion failed");
                    }
                    finally
                    {
                        ShowProgress(false);
                    }
                }
            }
        }

        private void ValidateData()
        {
            try
            {
                SetStatus("Validating data...");
                ShowProgress(true);

                var result = projectManager.ValidateForExport();
                ShowValidationResults(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during validation: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ShowProgress(false);
                SetStatus("Validation completed");
            }
        }

        private void ShowQuickDiagnostics()
        {
            var diagnostics = projectManager.GenerateQuickDiagnostics();

            using (var form = new Form())
            {
                form.Text = "Quick Diagnostics";
                form.Size = new Size(600, 500);  // Smaller initial size
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.Sizable;  // Make resizable
                form.MinimumSize = new Size(400, 300);  // Set minimum size

                var textBox = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Both,  // Both horizontal and vertical
                    Font = new Font("Consolas", 9),
                    Text = diagnostics,
                    WordWrap = false  // Allow horizontal scrolling
                };

                form.Controls.Add(textBox);
                form.ShowDialog();
            }
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
- qty: Quantity value
- qu: Unit of measurement
- up: Unit price
- sum: Total value (qty * up)
- properties: PHP-serialized properties string

Auto-Generated Fields:
- id2, id5, id6, ident: GUID identifiers
- created, created3: Timestamps
- criteria: Default criteria string

IFC-Specific Fields:
- ifc_type: IFC element type (IFCPIPESEGMENT, IFCWALL, etc.)
- material: Material specification
- dimension: Dimensional information
- segment_type: Type/category specification

Optional Fields:
- bimkey: BIM reference key
- color: Color coding (hex format)
- note: Additional notes
- type: Element type/category
- label: Display label

All 69+ fields are supported for full NOVA AVA compatibility.
See CostElement class for complete field listing.";

            using (var form = new Form())
            {
                form.Text = "Column Mapping Reference";
                form.Size = new Size(600, 500);
                form.StartPosition = FormStartPosition.CenterParent;

                var textBox = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Font = new Font("Consolas", 9),
                    Text = mappingInfo
                };

                form.Controls.Add(textBox);
                form.ShowDialog();
            }
        }

        private void ShowLogMessages()
        {
            using (var form = new Form())
            {
                form.Text = "Log Messages";
                form.Size = new Size(700, 400);
                form.StartPosition = FormStartPosition.CenterParent;

                var textBox = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Font = new Font("Consolas", 9),
                    Text = string.Join(Environment.NewLine, projectManager.LogMessages)
                };

                form.Controls.Add(textBox);
                form.ShowDialog();
            }
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "NOVA AVA Cost Management System\n\n" +
                "Version 1.0\n" +
                "Complete cost element management with AVA/NOVA XML compatibility\n\n" +
                "Features:\n" +
                "- Full 69+ column support\n" +
                "- PHP-serialized properties generation\n" +
                "- Template-based workflows\n" +
                "- Import/Export AVA and GAEB XML\n" +
                "- Data validation and diagnostics\n\n" +
                "Built with C# WinForms",
                "About NOVA AVA Cost Management",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // Element management
        private void AddElement()
        {
            using (var form = new ElementEditForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    projectManager.Elements.Add(form.CostElement);
                    RefreshDataGrid();
                    SetStatus("Element added");
                }
            }
        }

        private void EditElement()
        {
            if (dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an element to edit.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedElement = (CostElement)dataGridView.SelectedRows[0].Tag;
            using (var form = new ElementEditForm(selectedElement))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    var index = projectManager.Elements.IndexOf(selectedElement);
                    projectManager.Elements[index] = form.CostElement;
                    RefreshDataGrid();
                    SetStatus("Element updated");
                }
            }
        }

        private void DeleteElement()
        {
            if (dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an element to delete.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show("Are you sure you want to delete the selected element?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                var selectedElement = (CostElement)dataGridView.SelectedRows[0].Tag;
                projectManager.Elements.Remove(selectedElement);
                RefreshDataGrid();
                SetStatus("Element deleted");
            }
        }

        // Helper methods
        private bool ConfirmUnsavedChanges()
        {
            // For now, just return true. In a full implementation, 
            // you'd track dirty state and prompt user
            return true;
        }

        private void SetStatus(string message)
        {
            statusLabel.Text = message;
            projectManager.AddLogMessage(message);
        }

        private void ShowProgress(bool show)
        {
            progressBar.Visible = show;
        }

        private void ShowValidationResults(ValidationResult result)
        {
            using (var form = new ValidationResultForm(result))
            {
                form.ShowDialog();
            }
        }

        private void ShowExportPreview(string filePath)
        {
            var result = MessageBox.Show(
                $"Export completed successfully!\n\nFile: {Path.GetFileName(filePath)}\nElements: {projectManager.Elements.Count}\n\nWould you like to open the exported file?",
                "Export Complete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                try
                {
                    System.Diagnostics.Process.Start(filePath);
                }
                catch
                {
                    MessageBox.Show($"Could not open file: {filePath}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}