using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NovaAvaCostManagement
{
    public partial class ElementEditForm : Form
    {
        public CostElement CostElement { get; private set; }
        private int nextAvailableId;

        // Editable controls
        private TextBox txtName, txtDescription, txtChildren;
        private TextBox txtIdent, txtText, txtLongText, txtQty, txtQtyResult, txtQu, txtUp;

        // SPEC fields
        private TextBox txtSpecFilter, txtSpecName, txtSpecSize;
        private TextBox txtSpecType, txtSpecManufacturer, txtSpecMaterial;

        // Properties display
        private Label lblProperties;
        private Button btnGenerateProperties, btnGenerateGuid;

        private Button btnOK, btnCancel;
        private Panel scrollPanel, buttonPanel;

        public ElementEditForm() : this(null, 1) { }

        public ElementEditForm(CostElement element, int nextId)
        {
            InitializeComponent();
            nextAvailableId = nextId;

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
            this.Size = new Size(750, 900);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(750, 700);

            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Scrollable panel
            scrollPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(710, 780),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            mainContainer.Controls.Add(scrollPanel);

            int yPos = 20;
            const int labelWidth = 150;
            const int textBoxWidth = 500;
            const int spacing = 35;

            // SECTION 1: BASIC INFORMATION
            var lblBasicSection = new Label
            {
                Text = "BASIC INFORMATION",
                Location = new Point(20, yPos),
                Size = new Size(textBoxWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            scrollPanel.Controls.Add(lblBasicSection);
            yPos += 35;

            // Cost-ID (Read-only display)
            var lblCostId = new Label
            {
                Text = "Cost-ID:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            scrollPanel.Controls.Add(lblCostId);

            var txtCostId = new TextBox
            {
                Location = new Point(180, yPos),
                Size = new Size(150, 20),
                Text = CostElement.Id,
                ReadOnly = true,
                BackColor = SystemColors.Control
            };
            scrollPanel.Controls.Add(txtCostId);
            yPos += spacing;

            // Name
            AddLabelAndTextBox("Name *:", ref txtName, ref yPos, labelWidth, textBoxWidth, spacing);

            // Description
            AddLabelAndTextBox("Description:", ref txtDescription, ref yPos, labelWidth, textBoxWidth, spacing);

            // SECTION 2: SPEC PARAMETERS
            var lblSpecSection = new Label
            {
                Text = "SPEC PARAMETERS (for Properties Generation)",
                Location = new Point(20, yPos),
                Size = new Size(textBoxWidth + 50, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };
            scrollPanel.Controls.Add(lblSpecSection);
            yPos += 35;

            // SPEC Filter
            AddLabelAndTextBox("DX.SPEC_filter:", ref txtSpecFilter, ref yPos, labelWidth, textBoxWidth, spacing);

            // SPEC Name
            AddLabelAndTextBox("DX.SPEC_Name:", ref txtSpecName, ref yPos, labelWidth, textBoxWidth, spacing);

            // SPEC Size
            AddLabelAndTextBox("DX.SPEC_Size:", ref txtSpecSize, ref yPos, labelWidth, textBoxWidth, spacing);

            // SPEC Type
            AddLabelAndTextBox("DX.SPEC_Type:", ref txtSpecType, ref yPos, labelWidth, textBoxWidth, spacing);

            // SPEC Manufacturer
            AddLabelAndTextBox("DX.SPEC_Manufacturer:", ref txtSpecManufacturer, ref yPos, labelWidth, textBoxWidth, spacing);

            // SPEC Material
            AddLabelAndTextBox("DX.SPEC_Material:", ref txtSpecMaterial, ref yPos, labelWidth, textBoxWidth, spacing);

            // Properties (read-only display with generate button)
            var lblPropertiesLabel = new Label
            {
                Text = "Properties (Generated):",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20)
            };
            scrollPanel.Controls.Add(lblPropertiesLabel);

            lblProperties = new Label
            {
                Location = new Point(180, yPos),
                Size = new Size(textBoxWidth, 60),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SystemColors.Control,
                Font = new Font("Consolas", 8F),
                ForeColor = Color.Gray,
                Text = "(Click Generate to create properties)"
            };
            scrollPanel.Controls.Add(lblProperties);

            btnGenerateProperties = new Button
            {
                Text = "Generate Properties",
                Location = new Point(180, yPos + 65),
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnGenerateProperties.Click += BtnGenerateProperties_Click;
            scrollPanel.Controls.Add(btnGenerateProperties);
            yPos += 105;

            // SECTION 3: CALCULATION FIELDS
            var lblCalcSection = new Label
            {
                Text = "CALCULATION INFORMATION",
                Location = new Point(20, yPos),
                Size = new Size(textBoxWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            scrollPanel.Controls.Add(lblCalcSection);
            yPos += 35;

            // Line-ID and Order (Read-only display)
            var lblLineId = new Label
            {
                Text = "Line-ID:",
                Location = new Point(20, yPos),
                Size = new Size(75, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            scrollPanel.Controls.Add(lblLineId);

            var txtLineId = new TextBox
            {
                Location = new Point(100, yPos),
                Size = new Size(100, 20),
                Text = CostElement.CalculationId.ToString(),
                ReadOnly = true,
                BackColor = SystemColors.Control
            };
            scrollPanel.Controls.Add(txtLineId);

            var lblOrder = new Label
            {
                Text = "Order:",
                Location = new Point(220, yPos),
                Size = new Size(60, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            scrollPanel.Controls.Add(lblOrder);

            var txtOrder = new TextBox
            {
                Location = new Point(285, yPos),
                Size = new Size(100, 20),
                Text = CostElement.Order.ToString(),
                ReadOnly = true,
                BackColor = SystemColors.Control
            };
            scrollPanel.Controls.Add(txtOrder);
            yPos += spacing;

            // Ident (GUID) with generate button
            var lblIdent = new Label
            {
                Text = "GUID-Ident *:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20)
            };
            scrollPanel.Controls.Add(lblIdent);

            txtIdent = new TextBox
            {
                Location = new Point(180, yPos),
                Size = new Size(300, 20),
                ReadOnly = true,
                BackColor = Color.LightYellow
            };
            scrollPanel.Controls.Add(txtIdent);

            btnGenerateGuid = new Button
            {
                Text = "Generate",
                Location = new Point(490, yPos),
                Size = new Size(80, 23)
            };
            btnGenerateGuid.Click += BtnGenerateGuid_Click;
            scrollPanel.Controls.Add(btnGenerateGuid);
            yPos += spacing;

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
                Location = new Point(180, yPos),
                Size = new Size(textBoxWidth, 60),
                Multiline = true,
                MaxLength = 4000,
                ScrollBars = ScrollBars.Vertical
            };
            scrollPanel.Controls.Add(txtLongText);
            yPos += 70;

            // Quantity (formula)
            AddLabelAndTextBox("Quantity (Formula):", ref txtQty, ref yPos, labelWidth, textBoxWidth, spacing);

            // Quantity Result
            AddLabelAndTextBox("Qty Result *:", ref txtQtyResult, ref yPos, labelWidth, textBoxWidth, spacing);

            // Unit (Einheit)
            AddLabelAndTextBox("Einheit (Unit):", ref txtQu, ref yPos, labelWidth, textBoxWidth, spacing);

            // Price (Unit Price)
            AddLabelAndTextBox("Price (Up) *:", ref txtUp, ref yPos, labelWidth, textBoxWidth, spacing);

            // Total (Auto-calculated - read-only display)
            var lblTotal = new Label
            {
                Text = "Total (Auto-calc):",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            scrollPanel.Controls.Add(lblTotal);

            var txtTotal = new TextBox
            {
                Location = new Point(180, yPos),
                Size = new Size(200, 20),
                ReadOnly = true,
                BackColor = Color.FromArgb(230, 255, 230),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = CostElement.UpResult.ToString("F3")
            };
            scrollPanel.Controls.Add(txtTotal);
            yPos += spacing;

            // Children
            AddLabelAndTextBox("Children:", ref txtChildren, ref yPos, labelWidth, textBoxWidth, spacing);

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
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
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
                Location = new Point(180, yPos),
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

        private void LoadElementData()
        {
            txtName.Text = CostElement.Name;
            txtDescription.Text = CostElement.Description;
            txtChildren.Text = CostElement.Children;

            // Parse existing properties to populate SPEC fields
            if (!string.IsNullOrEmpty(CostElement.Properties))
            {
                CostElement.ParseIfcParameters();
            }

            txtSpecFilter.Text = CostElement.SpecFilter;
            txtSpecName.Text = CostElement.SpecName;
            txtSpecSize.Text = CostElement.SpecSize;
            txtSpecType.Text = CostElement.SpecType;
            txtSpecManufacturer.Text = CostElement.SpecManufacturer;
            txtSpecMaterial.Text = CostElement.SpecMaterial;

            if (!string.IsNullOrEmpty(CostElement.Properties))
            {
                lblProperties.Text = CostElement.Properties.Length > 150
                    ? CostElement.Properties.Substring(0, 150) + "..."
                    : CostElement.Properties;
                lblProperties.ForeColor = Color.Black;
            }

            txtIdent.Text = CostElement.Ident;
            txtText.Text = CostElement.Text;
            txtLongText.Text = CostElement.LongText;
            txtQty.Text = CostElement.Qty;
            txtQtyResult.Text = CostElement.QtyResult.ToString();
            txtQu.Text = CostElement.Qu;
            txtUp.Text = CostElement.Up.ToString();
        }

        private void BtnGenerateGuid_Click(object sender, EventArgs e)
        {
            txtIdent.Text = Guid.NewGuid().ToString().Replace("-", "");
            MessageBox.Show($"New GUID generated:\n{txtIdent.Text}", "GUID Generated",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnGenerateProperties_Click(object sender, EventArgs e)
        {
            try
            {
                var properties = PropertiesSerializer.SerializeSpecProperties(
                    txtSpecFilter.Text?.Trim() ?? "",
                    txtSpecName.Text?.Trim() ?? "",
                    txtSpecSize.Text?.Trim() ?? "",
                    txtSpecType.Text?.Trim() ?? "",
                    txtSpecManufacturer.Text?.Trim() ?? "",
                    txtSpecMaterial.Text?.Trim() ?? "");

                if (string.IsNullOrEmpty(properties))
                {
                    MessageBox.Show("Please fill in at least one SPEC field.", "No SPEC Data",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                CostElement.Properties = properties;

                lblProperties.Text = properties.Length > 150
                    ? properties.Substring(0, 150) + "..."
                    : properties;
                lblProperties.ForeColor = Color.Green;

                MessageBox.Show("Properties generated successfully!\n\n" + properties, "Success",
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
            CostElement.Description = txtDescription.Text.Trim();
            CostElement.Children = txtChildren.Text.Trim();

            CostElement.SpecFilter = txtSpecFilter.Text.Trim();
            CostElement.SpecName = txtSpecName.Text.Trim();
            CostElement.SpecSize = txtSpecSize.Text.Trim();
            CostElement.SpecType = txtSpecType.Text.Trim();
            CostElement.SpecManufacturer = txtSpecManufacturer.Text.Trim();
            CostElement.SpecMaterial = txtSpecMaterial.Text.Trim();

            CostElement.Ident = txtIdent.Text.Trim();
            CostElement.Text = txtText.Text.Trim();
            CostElement.LongText = txtLongText.Text.Trim();
            CostElement.Qty = txtQty.Text.Trim();

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
                    MessageBox.Show("Price must be a non-negative number.", "Validation Error",
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