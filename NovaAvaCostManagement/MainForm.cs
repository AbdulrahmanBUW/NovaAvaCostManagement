using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static NovaAvaCostManagement.ProjectManager;

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
            this.Size = new Size(600, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.MinimumSize = new Size(500, 400);

            this.AutoScroll = true;
            this.AutoScrollMinSize = new Size(580, 650);

            CreateMenuStrip();
            CreateToolStrip();
            CreateMainContent();
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
            toolsMenu.DropDownItems.Add("&Compare with Original XML", null, (s, e) => ShowComparisonWithOriginal());
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
            dataGridView.Rows.Clear();

            // Critical settings for header visibility
            dataGridView.ColumnHeadersVisible = true;
            dataGridView.RowHeadersVisible = true;
            dataGridView.AutoGenerateColumns = false;
            dataGridView.AllowUserToAddRows = false;
            dataGridView.ReadOnly = true;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.MultiSelect = false;

            // Scrolling
            dataGridView.ScrollBars = ScrollBars.Both;
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Style the header
            dataGridView.EnableHeadersVisualStyles = false;
            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48);
            dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView.ColumnHeadersDefaultCellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold);
            dataGridView.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView.ColumnHeadersHeight = 30;
            dataGridView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            // FIXED: Add all columns first, then freeze them at the end
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Version",
                HeaderText = "Ver",
                Width = 40,
                ReadOnly = true
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "ID",
                Width = 50,
                ReadOnly = true
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id2",
                HeaderText = "Code",
                Width = 120,
                ReadOnly = true
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Name",
                Width = 200,
                ReadOnly = true
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Type",
                HeaderText = "Type",
                Width = 80,
                ReadOnly = true
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Text",
                HeaderText = "Text",
                Width = 150,
                ReadOnly = true
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "LongText",
                HeaderText = "Long Text",
                Width = 200,
                ReadOnly = true
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Qty",
                HeaderText = "Qty",
                Width = 80,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Qu",
                HeaderText = "Unit",
                Width = 60,
                ReadOnly = true
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Up",
                HeaderText = "Unit Price",
                Width = 80,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Sum",
                HeaderText = "Total",
                Width = 80,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font(dataGridView.Font, FontStyle.Bold)
                }
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Properties",
                HeaderText = "Properties",
                Width = 150,
                ReadOnly = true
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BimKey",
                HeaderText = "BIM Key",
                Width = 100,
                ReadOnly = true
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Note",
                HeaderText = "Note",
                Width = 150,
                ReadOnly = true
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Color",
                HeaderText = "Color",
                Width = 80,
                ReadOnly = true
            });

            // CRITICAL: Freeze columns AFTER all columns are added
            // Freeze columns from left to right only
            dataGridView.Columns["Version"].Frozen = true;
            dataGridView.Columns["Id"].Frozen = true;
            dataGridView.Columns["Id2"].Frozen = true;
            dataGridView.Columns["Name"].Frozen = true;

            // Style the grid
            dataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dataGridView.RowsDefaultCellStyle.BackColor = Color.White;
            dataGridView.GridColor = Color.FromArgb(200, 200, 200);
            dataGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(51, 153, 255);
            dataGridView.DefaultCellStyle.SelectionForeColor = Color.White;
        }

        /// <summary>
        /// Refresh DataGridView with current data
        /// </summary>
        private void RefreshDataGrid()
        {
            dataGridView.Rows.Clear();

            var filteredElements = GetFilteredElements();

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

                row.Tag = element;
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
                        SetStatus("Importing AVA XML with schema tracking...");
                        ShowProgress(true);

                        projectManager.ImportAvaXmlWithSchemaTracking(dialog.FileName);

                        RefreshDataGrid();
                        SetStatus($"AVA XML imported: {Path.GetFileName(dialog.FileName)}");

                        MessageBox.Show(
                            $"Import successful!\n\n" +
                            $"Elements imported: {projectManager.Elements.Count}\n" +
                            $"Original schema captured for validation comparison",
                            "Import Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error importing AVA XML:\n{ex.Message}",
                            "Import Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
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
            ExportXmlWithValidation(false);
        }

        private void ExportGaebXml()
        {
            ExportXmlWithValidation(true);
        }

        private void ExportXmlWithValidation(bool useGaebFormat)
        {
            SetStatus("Validating data before export...");
            ShowProgress(true);

            try
            {
                var validationResult = projectManager.ValidateForExportEnhanced();

                ShowProgress(false);
                ShowEnhancedValidationResults(validationResult);

                if (validationResult.HasErrors)
                {
                    var forceResult = MessageBox.Show(
                        $"Validation found {validationResult.Errors.Count} critical error(s).\n\n" +
                        "Exporting with errors may result in data that cannot be re-imported into AVA NOVA.\n\n" +
                        "Do you want to force export anyway? (NOT RECOMMENDED)",
                        "Critical Validation Errors",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button2);

                    if (forceResult != DialogResult.Yes)
                    {
                        SetStatus("Export cancelled due to validation errors");
                        return;
                    }
                }

                if (validationResult.HasWarnings && !validationResult.HasErrors)
                {
                    var proceedResult = MessageBox.Show(
                        $"Validation found {validationResult.Warnings.Count} warning(s).\n\n" +
                        "Warnings indicate potential issues but export should be safe.\n\n" +
                        "Do you want to proceed with export?",
                        "Validation Warnings",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (proceedResult != DialogResult.Yes)
                    {
                        SetStatus("Export cancelled by user");
                        return;
                    }
                }

                using (var dialog = new SaveFileDialog())
                {
                    dialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                    dialog.Title = useGaebFormat ? "Export GAEB XML" : "Export AVA XML";
                    dialog.FileName = $"NovaAva_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xml";

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        SetStatus("Exporting XML...");
                        ShowProgress(true);

                        projectManager.ExportAvaXmlSafe(dialog.FileName, useGaebFormat, forceExport: true);

                        SetStatus($"XML exported: {Path.GetFileName(dialog.FileName)}");

                        var message = $"Export completed successfully!\n\n" +
                                     $"File: {Path.GetFileName(dialog.FileName)}\n" +
                                     $"Elements: {projectManager.Elements.Count}";

                        if (validationResult.HasWarnings)
                        {
                            message += $"\n\nNote: Exported with {validationResult.Warnings.Count} warning(s). " +
                                      "Review validation report for details.";
                        }

                        var showFileResult = MessageBox.Show(
                            message + "\n\nWould you like to open the exported file?",
                            "Export Complete",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (showFileResult == DialogResult.Yes)
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during export: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Export failed");
            }
            finally
            {
                ShowProgress(false);
            }
        }

        private void ShowComparisonWithOriginal()
        {
            if (!EnhancedValidator.OriginalXmlSchema.Any())
            {
                MessageBox.Show(
                    "No original XML data available for comparison.\n\n" +
                    "Import an XML file first to enable schema comparison.",
                    "No Original Data",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var comparison = projectManager.CompareWithOriginal();

            using (var form = new Form())
            {
                form.Text = "Comparison with Original XML";
                form.Size = new Size(700, 500);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.Sizable;
                form.MinimumSize = new Size(500, 400);

                var textBox = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Both,
                    Font = new Font("Consolas", 9),
                    Text = comparison.GetSummary()
                };

                form.Controls.Add(textBox);

                var buttonPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50
                };

                var btnClose = new Button
                {
                    Text = "Close",
                    Location = new Point(form.Width - 100, 10),
                    Size = new Size(80, 30),
                    Anchor = AnchorStyles.Right
                };
                btnClose.Click += (s, e) => form.Close();
                buttonPanel.Controls.Add(btnClose);

                if (comparison.HasDifferences)
                {
                    var btnExport = new Button
                    {
                        Text = "Export Report",
                        Location = new Point(20, 10),
                        Size = new Size(120, 30)
                    };
                    btnExport.Click += (s, e) => ExportComparisonReport(comparison);
                    buttonPanel.Controls.Add(btnExport);
                }

                form.Controls.Add(buttonPanel);
                form.ShowDialog();
            }
        }

        private void ExportComparisonReport(ComparisonResult comparison)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                dialog.Title = "Export Comparison Report";
                dialog.FileName = $"Comparison_Report_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(dialog.FileName, comparison.GetSummary());
                        MessageBox.Show("Comparison report exported successfully!", "Export Complete",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting report: {ex.Message}", "Export Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                            if (result == DialogResult.Yes)
                            {
                                projectManager.Elements.AddRange(convertedElements);
                                RefreshDataGrid();
                                SetStatus($"Template converted and appended: {convertedElements.Count} elements");
                            }
                            else if (result == DialogResult.No)
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
                SetStatus("Running enhanced validation...");
                ShowProgress(true);

                var result = projectManager.ValidateForExportEnhanced();
                ShowEnhancedValidationResults(result);
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

        private void ShowEnhancedValidationResults(ValidationResult result)
        {
            using (var form = new Form())
            {
                form.Text = "Enhanced Validation Results";
                form.Size = new Size(800, 600);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.Sizable;
                form.MinimumSize = new Size(600, 400);

                var tabControl = new TabControl
                {
                    Dock = DockStyle.Fill
                };

                // Tab 1: Summary
                var summaryTab = new TabPage("Summary");
                var summaryPanel = CreateSummaryPanel(result);
                summaryTab.Controls.Add(summaryPanel);
                tabControl.TabPages.Add(summaryTab);

                // Tab 2: Detailed Report
                var reportTab = new TabPage("Detailed Report");
                var reportText = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Both,
                    Font = new Font("Consolas", 9F),
                    Text = EnhancedValidator.GenerateValidationReport(result)
                };
                reportTab.Controls.Add(reportText);
                tabControl.TabPages.Add(reportTab);

                // Tab 3: Comparison with Original
                if (EnhancedValidator.OriginalXmlSchema.Any())
                {
                    var comparisonTab = new TabPage("Original Comparison");
                    var comparisonText = new TextBox
                    {
                        Dock = DockStyle.Fill,
                        Multiline = true,
                        ReadOnly = true,
                        ScrollBars = ScrollBars.Both,
                        Font = new Font("Consolas", 9F),
                        Text = projectManager.CompareWithOriginal().GetSummary()
                    };
                    comparisonTab.Controls.Add(comparisonText);
                    tabControl.TabPages.Add(comparisonTab);
                }

                form.Controls.Add(tabControl);

                var buttonPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50
                };

                var btnClose = new Button
                {
                    Text = "Close",
                    Location = new Point(form.Width - 100, 10),
                    Size = new Size(80, 30),
                    Anchor = AnchorStyles.Right | AnchorStyles.Top
                };
                btnClose.Click += (s, e) => form.Close();
                buttonPanel.Controls.Add(btnClose);

                if (result.HasErrors)
                {
                    var btnExport = new Button
                    {
                        Text = "Export Anyway (Risky)",
                        Location = new Point(form.Width - 200, 10),
                        Size = new Size(150, 30),
                        Anchor = AnchorStyles.Right | AnchorStyles.Top,
                        BackColor = Color.Orange
                    };
                    btnExport.Click += (s, e) =>
                    {
                        var confirmResult = MessageBox.Show(
                            "WARNING: Exporting with validation errors may result in data that cannot be imported back into AVA NOVA.\n\n" +
                            "Are you absolutely sure you want to export anyway?",
                            "Risky Export",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (confirmResult == DialogResult.Yes)
                        {
                            form.DialogResult = DialogResult.Ignore;
                            form.Close();
                        }
                    };
                    buttonPanel.Controls.Add(btnExport);
                }
                else
                {
                    var btnProceed = new Button
                    {
                        Text = "Proceed to Export",
                        Location = new Point(form.Width - 200, 10),
                        Size = new Size(130, 30),
                        Anchor = AnchorStyles.Right | AnchorStyles.Top,
                        BackColor = Color.LightGreen
                    };
                    btnProceed.Click += (s, e) =>
                    {
                        form.DialogResult = DialogResult.OK;
                        form.Close();
                    };
                    buttonPanel.Controls.Add(btnProceed);
                }

                form.Controls.Add(buttonPanel);
                form.ShowDialog();
            }
        }

        private Panel CreateSummaryPanel(ValidationResult result)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            int yPos = 20;

            var statusLabel = new Label
            {
                Location = new Point(20, yPos),
                Size = new Size(700, 60),
                Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold),
                ForeColor = result.IsValid ? Color.Green : Color.Red,
                Text = result.IsValid ?
                    "✓ VALIDATION PASSED - Ready for Export" :
                    "✗ VALIDATION FAILED - Cannot Export"
            };
            panel.Controls.Add(statusLabel);
            yPos += 70;

            var statsGroup = new GroupBox
            {
                Location = new Point(20, yPos),
                Size = new Size(700, 120),
                Text = "Validation Statistics"
            };

            var statsText = new Label
            {
                Location = new Point(10, 25),
                Size = new Size(680, 90),
                Font = new Font("Microsoft Sans Serif", 10F),
                Text = $"Total Elements Validated: {projectManager.Elements.Count}\n" +
                       $"Critical Errors: {result.Errors.Count}\n" +
                       $"Warnings: {result.Warnings.Count}\n" +
                       $"Schema Comparison: {(EnhancedValidator.OriginalXmlSchema.Any() ? "Available" : "Not Available")}"
            };
            statsGroup.Controls.Add(statsText);
            panel.Controls.Add(statsGroup);
            yPos += 130;

            if (result.HasErrors)
            {
                var errorGroup = new GroupBox
                {
                    Location = new Point(20, yPos),
                    Size = new Size(700, 150),
                    Text = "Critical Errors (Must Fix)",
                    ForeColor = Color.Red
                };

                var errorList = new ListBox
                {
                    Location = new Point(10, 25),
                    Size = new Size(680, 115),
                    Font = new Font("Consolas", 9F)
                };

                foreach (var error in result.Errors.Take(10))
                {
                    errorList.Items.Add(error);
                }

                if (result.Errors.Count > 10)
                {
                    errorList.Items.Add($"... and {result.Errors.Count - 10} more errors");
                }

                errorGroup.Controls.Add(errorList);
                panel.Controls.Add(errorGroup);
                yPos += 160;
            }

            if (result.HasWarnings)
            {
                var warningGroup = new GroupBox
                {
                    Location = new Point(20, yPos),
                    Size = new Size(700, 150),
                    Text = "Warnings (Should Review)",
                    ForeColor = Color.Orange
                };

                var warningList = new ListBox
                {
                    Location = new Point(10, 25),
                    Size = new Size(680, 115),
                    Font = new Font("Consolas", 9F)
                };

                foreach (var warning in result.Warnings.Take(10))
                {
                    warningList.Items.Add(warning);
                }

                if (result.Warnings.Count > 10)
                {
                    warningList.Items.Add($"... and {result.Warnings.Count - 10} more warnings");
                }

                warningGroup.Controls.Add(warningList);
                panel.Controls.Add(warningGroup);
            }

            return panel;
        }

        private void ShowQuickDiagnostics()
        {
            var diagnostics = projectManager.GenerateQuickDiagnostics();

            using (var form = new Form())
            {
                form.Text = "Quick Diagnostics";
                form.Size = new Size(600, 500);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.Sizable;
                form.MinimumSize = new Size(400, 300);

                var textBox = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Both,
                    Font = new Font("Consolas", 9),
                    Text = diagnostics,
                    WordWrap = false
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
                "- Enhanced validation and diagnostics\n\n" +
                "Built with C# WinForms",
                "About NOVA AVA Cost Management",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

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

        private bool ConfirmUnsavedChanges()
        {
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

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void MainForm_Load_1(object sender, EventArgs e)
        {

        }
    }
}