using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NovaAvaCostManagement
{
    public partial class ElementEditForm : Form
    {
        public CostElement CostElement { get; private set; }
        private int nextAvailableId;
        private List<string> availableIfcTypes = new List<string>();

        // Editable controls
        private TextBox txtName, txtChildren, txtCatalogName, txtIdent, txtCatalogType;
        private TextBox txtText, txtLongText, txtQtyResult, txtQu, txtUp;
        private ComboBox cmbIfcType;
        private Label lblProperties;
        private Button btnGenerateProperties, btnGenerateGuid;

        // Read-only info controls
        private GroupBox grpReadOnly;
        private TextBox txtIdReadOnly, txtCalcIdReadOnly, txtElementTypeReadOnly;
        private TextBox txtBimKeyReadOnly, txtOrderReadOnly, txtSumReadOnly;

        private Button btnOK, btnCancel;
        private Panel scrollPanel, buttonPanel;

        public ElementEditForm() : this(null, 1) { }

        public ElementEditForm(CostElement element, int nextId) : this(element, nextId, new List<string>()) { }

        public ElementEditForm(CostElement element, int nextId, List<string> ifcTypesFromXml)
        {
            InitializeComponent();
            nextAvailableId = nextId;
            availableIfcTypes = ifcTypesFromXml ?? new List<string>();

            if (element == null)
            {
                CostElement = new CostElement();
                CostElement.Id = nextAvailableId.ToString();
                CostElement.Ident = Guid.NewGuid().ToString();
                this.Text = "Add New Element";
            }
            else
            {
                CostElement = element.Clone();
                this.Text = "Edit Element";
            }

            InitializeCustomComponents();
            LoadElementData();
        }

        private void InitializeCustomComponents()
        {
            this.Size = new Size(700, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(700, 600);

            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Scrollable panel
            scrollPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(660, 650),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            mainContainer.Controls.Add(scrollPanel);

            int yPos = 20;
            const int labelWidth = 120;
            const int textBoxWidth = 500;
            const int spacing = 35;

            // SECTION 1: EDITABLE FIELDS
            var lblEditableSection = new Label
            {
                Text = "EDITABLE FIELDS",
                Location = new Point(20, yPos),
                Size = new Size(textBoxWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            scrollPanel.Controls.Add(lblEditableSection);
            yPos += 35;

            // ID (Auto-assigned for new, read-only for existing)
            AddLabelAndTextBox("ID (Auto):", ref txtIdReadOnly, ref yPos, labelWidth, textBoxWidth, spacing, true);
            txtIdReadOnly.Text = CostElement.Id;
            txtIdReadOnly.BackColor = SystemColors.Control;

            // Name
            AddLabelAndTextBox("Name *:", ref txtName, ref yPos, labelWidth, textBoxWidth, spacing);

            // Children
            AddLabelAndTextBox("Children:", ref txtChildren, ref yPos, labelWidth, textBoxWidth, spacing);

            // Catalog Name
            AddLabelAndTextBox("Catalog Name:", ref txtCatalogName, ref yPos, labelWidth, textBoxWidth, spacing);

            // Ident (GUID) with generate button
            var lblIdent = new Label
            {
                Text = "Ident (GUID) *:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20)
            };
            scrollPanel.Controls.Add(lblIdent);

            txtIdent = new TextBox
            {
                Location = new Point(150, yPos),
                Size = new Size(300, 20),
                ReadOnly = true,
                BackColor = Color.LightYellow
            };
            scrollPanel.Controls.Add(txtIdent);

            btnGenerateGuid = new Button
            {
                Text = "Generate",
                Location = new Point(460, yPos),
                Size = new Size(80, 23)
            };
            btnGenerateGuid.Click += BtnGenerateGuid_Click;
            scrollPanel.Controls.Add(btnGenerateGuid);
            yPos += spacing;

            // Catalog Type
            AddLabelAndTextBox("Catalog Type:", ref txtCatalogType, ref yPos, labelWidth, textBoxWidth, spacing);

            // Text
            AddLabelAndTextBox("Text *:", ref txtText, ref yPos, labelWidth, textBoxWidth, spacing);

            // Long Text (multiline)
            var lblLongText = new Label
            {
                Text = "Long Text:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20)
            };
            scrollPanel.Controls.Add(lblLongText);

            txtLongText = new TextBox
            {
                Location = new Point(150, yPos),
                Size = new Size(textBoxWidth, 60),
                Multiline = true,
                MaxLength = 2000,
                ScrollBars = ScrollBars.Vertical
            };
            scrollPanel.Controls.Add(txtLongText);
            yPos += 70;

            // Quantity Result
            AddLabelAndTextBox("Qty Result:", ref txtQtyResult, ref yPos, labelWidth, textBoxWidth, spacing);

            // Unit Price
            AddLabelAndTextBox("Unit Price:", ref txtUp, ref yPos, labelWidth, textBoxWidth, spacing);

            // Unit
            AddLabelAndTextBox("Unit (qu):", ref txtQu, ref yPos, labelWidth, textBoxWidth, spacing);

            // IFC SECTION
            var lblIfcSection = new Label
            {
                Text = "IFC PARAMETERS",
                Location = new Point(20, yPos),
                Size = new Size(textBoxWidth + 150, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };
            scrollPanel.Controls.Add(lblIfcSection);
            yPos += 30;

            // IFC Type dropdown
            var lblIfcType = new Label
            {
                Text = "IFC Type:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20)
            };
            scrollPanel.Controls.Add(lblIfcType);

            cmbIfcType = new ComboBox
            {
                Location = new Point(150, yPos),
                Size = new Size(textBoxWidth, 20),
                DropDownStyle = ComboBoxStyle.DropDown
            };

            if (availableIfcTypes != null && availableIfcTypes.Any())
            {
                cmbIfcType.Items.AddRange(availableIfcTypes.ToArray());
            }
            else
            {
                cmbIfcType.Items.AddRange(new[] {
                    "IFCPIPESEGMENT", "IFCPIPEFITTING", "IFCWALL", "IFCBEAM",
                    "IFCSLAB", "IFCDOOR", "IFCWINDOW", "IFCCOLUMN"
                });
            }

            scrollPanel.Controls.Add(cmbIfcType);
            yPos += spacing;

            // Properties (read-only display with generate button)
            var lblPropertiesLabel = new Label
            {
                Text = "Properties:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20)
            };
            scrollPanel.Controls.Add(lblPropertiesLabel);

            lblProperties = new Label
            {
                Location = new Point(150, yPos),
                Size = new Size(textBoxWidth, 40),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SystemColors.Control,
                Font = new Font("Consolas", 8F),
                ForeColor = Color.Gray,
                Text = "(Click Generate to create properties)"
            };
            scrollPanel.Controls.Add(lblProperties);

            btnGenerateProperties = new Button
            {
                Text = "Generate",
                Location = new Point(150, yPos + 45),
                Size = new Size(80, 25)
            };
            btnGenerateProperties.Click += BtnGenerateProperties_Click;
            scrollPanel.Controls.Add(btnGenerateProperties);
            yPos += 80;

            // SECTION 2: READ-ONLY INFORMATION
            grpReadOnly = new GroupBox
            {
                Text = "READ-ONLY INFORMATION",
                Location = new Point(20, yPos),
                Size = new Size(textBoxWidth + 150, 180),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            int readOnlyY = 25;
            const int readOnlySpacing = 30;

            AddReadOnlyField(grpReadOnly, "Calculation ID:", ref txtCalcIdReadOnly, readOnlyY, labelWidth);
            readOnlyY += readOnlySpacing;

            AddReadOnlyField(grpReadOnly, "Element Type:", ref txtElementTypeReadOnly, readOnlyY, labelWidth);
            readOnlyY += readOnlySpacing;

            AddReadOnlyField(grpReadOnly, "BIM Key:", ref txtBimKeyReadOnly, readOnlyY, labelWidth);
            readOnlyY += readOnlySpacing;

            AddReadOnlyField(grpReadOnly, "Order:", ref txtOrderReadOnly, readOnlyY, labelWidth);
            readOnlyY += readOnlySpacing;

            AddReadOnlyField(grpReadOnly, "Sum:", ref txtSumReadOnly, readOnlyY, labelWidth);

            scrollPanel.Controls.Add(grpReadOnly);
            yPos += 190;

            scrollPanel.AutoScrollMinSize = new Size(0, yPos + 20);

            // BUTTON PANEL
            buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = SystemColors.Control
            };

            btnOK = new Button
            {
                Text = "Save",
                Size = new Size(100, 35),
                Location = new Point(mainContainer.Width - 220, 15),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnOK.Click += BtnOK_Click;
            buttonPanel.Controls.Add(btnOK);

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 35),
                Location = new Point(mainContainer.Width - 110, 15),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                DialogResult = DialogResult.Cancel
            };
            buttonPanel.Controls.Add(btnCancel);

            mainContainer.Controls.Add(buttonPanel);
            this.Controls.Add(mainContainer);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void AddLabelAndTextBox(string labelText, ref TextBox textBox, ref int yPos,
            int labelWidth, int textBoxWidth, int spacing, bool readOnly = false)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20)
            };
            scrollPanel.Controls.Add(label);

            textBox = new TextBox
            {
                Location = new Point(150, yPos),
                Size = new Size(textBoxWidth, 20),
                ReadOnly = readOnly
            };

            if (readOnly)
            {
                textBox.BackColor = SystemColors.Control;
            }

            scrollPanel.Controls.Add(textBox);
            yPos += spacing;
        }

        private void AddReadOnlyField(GroupBox parent, string labelText, ref TextBox textBox, int y, int labelWidth)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(10, y),
                Size = new Size(labelWidth, 20),
                Font = new Font("Segoe UI", 9F)
            };
            parent.Controls.Add(label);

            textBox = new TextBox
            {
                Location = new Point(140, y),
                Size = new Size(200, 20),
                ReadOnly = true,
                BackColor = SystemColors.Control,
                Font = new Font("Segoe UI", 9F)
            };
            parent.Controls.Add(textBox);
        }

        private void LoadElementData()
        {
            txtName.Text = CostElement.Name;
            txtChildren.Text = CostElement.Children;
            txtCatalogName.Text = CostElement.CatalogName;
            txtIdent.Text = CostElement.Ident;
            txtCatalogType.Text = CostElement.CatalogType;
            txtText.Text = CostElement.Text;
            txtLongText.Text = CostElement.LongText;
            txtQtyResult.Text = CostElement.QtyResult.ToString();
            txtUp.Text = CostElement.Up.ToString();
            txtQu.Text = CostElement.Qu;

            string currentIfcType = CostElement.GetIfcTypeFromProperties();
            if (!string.IsNullOrEmpty(currentIfcType))
            {
                cmbIfcType.Text = currentIfcType;
            }

            if (!string.IsNullOrEmpty(CostElement.Properties))
            {
                lblProperties.Text = CostElement.Properties.Length > 100
                    ? CostElement.Properties.Substring(0, 100) + "..."
                    : CostElement.Properties;
                lblProperties.ForeColor = Color.Black;
            }

            txtIdReadOnly.Text = CostElement.Id;
            txtCalcIdReadOnly.Text = CostElement.CalculationId.ToString();
            txtElementTypeReadOnly.Text = CostElement.ElementType.ToString();
            txtBimKeyReadOnly.Text = CostElement.BimKey;
            txtOrderReadOnly.Text = CostElement.Order.ToString();
            txtSumReadOnly.Text = CostElement.Sum.ToString("F2");
        }

        private void BtnGenerateGuid_Click(object sender, EventArgs e)
        {
            txtIdent.Text = Guid.NewGuid().ToString();
            MessageBox.Show($"New GUID generated:\n{txtIdent.Text}", "GUID Generated",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnGenerateProperties_Click(object sender, EventArgs e)
        {
            try
            {
                string ifcType = cmbIfcType.Text?.Trim() ?? "";

                if (string.IsNullOrEmpty(ifcType))
                {
                    MessageBox.Show("Please select an IFC Type first.", "IFC Type Required",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbIfcType.Focus();
                    return;
                }

                var properties = PropertiesSerializer.SerializeProperties(
                    ifcType,
                    CostElement.Material ?? "",
                    CostElement.Dimension ?? "",
                    CostElement.SegmentType ?? "");

                CostElement.Properties = properties;

                lblProperties.Text = properties.Length > 100
                    ? properties.Substring(0, 100) + "..."
                    : properties;
                lblProperties.ForeColor = Color.Green;

                MessageBox.Show("Properties generated successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating properties:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            CostElement.Name = txtName.Text.Trim();
            CostElement.Children = txtChildren.Text.Trim();
            CostElement.CatalogName = txtCatalogName.Text.Trim();
            CostElement.Ident = txtIdent.Text.Trim();
            CostElement.CatalogType = txtCatalogType.Text.Trim();
            CostElement.Text = txtText.Text.Trim();
            CostElement.LongText = txtLongText.Text.Trim();

            if (decimal.TryParse(txtQtyResult.Text, out decimal qty))
                CostElement.QtyResult = qty;

            if (decimal.TryParse(txtUp.Text, out decimal up))
                CostElement.Up = up;

            CostElement.Qu = txtQu.Text.Trim();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Name is required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtIdent.Text))
            {
                MessageBox.Show("Ident (GUID) is required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtText.Text))
            {
                MessageBox.Show("Text is required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtText.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(txtQtyResult.Text))
            {
                if (!decimal.TryParse(txtQtyResult.Text, out decimal qty) || qty < 0)
                {
                    MessageBox.Show("Quantity must be a non-negative number.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtQtyResult.Focus();
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(txtUp.Text))
            {
                if (!decimal.TryParse(txtUp.Text, out decimal up) || up < 0)
                {
                    MessageBox.Show("Unit Price must be a non-negative number.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUp.Focus();
                    return false;
                }
            }

            return true;
        }

        private void ElementEditForm_Load_2(object sender, EventArgs e)
        {
            // Required for designer
        }
    }
}