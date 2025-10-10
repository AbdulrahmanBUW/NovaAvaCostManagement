using System;
using System.Drawing;
using System.Windows.Forms;

namespace NovaAvaCostManagement
{
    public partial class ElementEditForm : Form
    {
        private const int FormWidth = 750;
        private const int FormHeight = 900;
        private const int MinFormWidth = 750;
        private const int MinFormHeight = 700;
        private const int LabelWidth = 150;
        private const int TextBoxWidth = 500;
        private const int FieldSpacing = 35;
        private const int ScrollPanelWidth = 710;
        private const int ScrollPanelHeight = 780;

        public CostElement CostElement { get; private set; }
        private int nextAvailableId;

        private TextBox txtName, txtDescription, txtChildren;
        private TextBox txtIdent, txtText, txtLongText, txtQty, txtQtyResult, txtQu, txtUp;
        private TextBox txtSpecFilter, txtSpecName, txtSpecSize;
        private TextBox txtSpecType, txtSpecManufacturer, txtSpecMaterial;
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
                CostElement = new CostElement
                {
                    Id = nextAvailableId.ToString(),
                    Ident = Guid.NewGuid().ToString()
                };
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
            ConfigureFormProperties();
            var mainContainer = CreateMainContainer();
            scrollPanel = CreateScrollPanel();

            int yPos = 20;
            BuildFormSections(ref yPos);

            scrollPanel.AutoScrollMinSize = new Size(0, yPos + 20);
            mainContainer.Controls.Add(scrollPanel);

            buttonPanel = CreateButtonPanel(mainContainer.Width);
            mainContainer.Controls.Add(buttonPanel);

            this.Controls.Add(mainContainer);
            ConfigureFormButtons();
        }

        private void ConfigureFormProperties()
        {
            this.Size = new Size(FormWidth, FormHeight);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(MinFormWidth, MinFormHeight);
        }

        private Panel CreateMainContainer()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
        }

        private Panel CreateScrollPanel()
        {
            return new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(ScrollPanelWidth, ScrollPanelHeight),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void BuildFormSections(ref int yPos)
        {
            AddBasicInformationSection(ref yPos);
            AddSpecParametersSection(ref yPos);
            AddCalculationSection(ref yPos);
        }

        private void AddBasicInformationSection(ref int yPos)
        {
            AddSectionHeader("BASIC INFORMATION", ref yPos, Color.DarkBlue);

            AddReadOnlyField("Cost-ID:", CostElement.Id, ref yPos, 150);
            AddLabelAndTextBox("Name *:", ref txtName, ref yPos);
            AddLabelAndTextBox("Description:", ref txtDescription, ref yPos);
        }

        private void AddSpecParametersSection(ref int yPos)
        {
            AddSectionHeader("SPEC PARAMETERS (for Properties Generation)", ref yPos, Color.DarkGreen);

            AddLabelAndTextBox("DX.SPEC_filter:", ref txtSpecFilter, ref yPos);
            AddLabelAndTextBox("DX.SPEC_Name:", ref txtSpecName, ref yPos);
            AddLabelAndTextBox("DX.SPEC_Size:", ref txtSpecSize, ref yPos);
            AddLabelAndTextBox("DX.SPEC_Type:", ref txtSpecType, ref yPos);
            AddLabelAndTextBox("DX.SPEC_Manufacturer:", ref txtSpecManufacturer, ref yPos);
            AddLabelAndTextBox("DX.SPEC_Material:", ref txtSpecMaterial, ref yPos);

            AddPropertiesDisplay(ref yPos);
        }

        private void AddCalculationSection(ref int yPos)
        {
            AddSectionHeader("CALCULATION INFORMATION", ref yPos, Color.DarkBlue);

            AddCalculationIdFields(ref yPos);
            AddIdentField(ref yPos);
            AddLabelAndTextBox("Text *:", ref txtText, ref yPos);
            AddLongTextField(ref yPos);
            AddLabelAndTextBox("Quantity (Formula):", ref txtQty, ref yPos);
            AddLabelAndTextBox("Qty Result *:", ref txtQtyResult, ref yPos);
            AddLabelAndTextBox("Einheit (Unit):", ref txtQu, ref yPos);
            AddLabelAndTextBox("Price (Up) *:", ref txtUp, ref yPos);
            AddTotalField(ref yPos);
            AddLabelAndTextBox("Children:", ref txtChildren, ref yPos);
        }

        private void AddSectionHeader(string text, ref int yPos, Color color)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(20, yPos),
                Size = new Size(TextBoxWidth + 50, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = color
            };
            scrollPanel.Controls.Add(label);
            yPos += 35;
        }

        private void AddReadOnlyField(string labelText, string value, ref int yPos, int fieldWidth)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(20, yPos),
                Size = new Size(75, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            scrollPanel.Controls.Add(label);

            var textBox = new TextBox
            {
                Location = new Point(100, yPos),
                Size = new Size(fieldWidth, 20),
                Text = value,
                ReadOnly = true,
                BackColor = SystemColors.Control
            };
            scrollPanel.Controls.Add(textBox);
            yPos += FieldSpacing;
        }

        private void AddCalculationIdFields(ref int yPos)
        {
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
            yPos += FieldSpacing;
        }

        private void AddIdentField(ref int yPos)
        {
            var label = new Label
            {
                Text = "GUID-Ident *:",
                Location = new Point(20, yPos),
                Size = new Size(LabelWidth, 20)
            };
            scrollPanel.Controls.Add(label);

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
            yPos += FieldSpacing;
        }

        private void AddLongTextField(ref int yPos)
        {
            var label = new Label
            {
                Text = "Long Text:",
                Location = new Point(20, yPos),
                Size = new Size(LabelWidth, 20)
            };
            scrollPanel.Controls.Add(label);

            txtLongText = new TextBox
            {
                Location = new Point(180, yPos),
                Size = new Size(TextBoxWidth, 60),
                Multiline = true,
                MaxLength = 4000,
                ScrollBars = ScrollBars.Vertical
            };
            scrollPanel.Controls.Add(txtLongText);
            yPos += 70;
        }

        private void AddTotalField(ref int yPos)
        {
            var label = new Label
            {
                Text = "Total (Auto-calc):",
                Location = new Point(20, yPos),
                Size = new Size(LabelWidth, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            scrollPanel.Controls.Add(label);

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
            yPos += FieldSpacing;
        }

        private void AddPropertiesDisplay(ref int yPos)
        {
            var label = new Label
            {
                Text = "Properties (Generated):",
                Location = new Point(20, yPos),
                Size = new Size(LabelWidth, 20)
            };
            scrollPanel.Controls.Add(label);

            lblProperties = new Label
            {
                Location = new Point(180, yPos),
                Size = new Size(TextBoxWidth, 60),
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
        }

        private void AddLabelAndTextBox(string labelText, ref TextBox textBox, ref int yPos,
            bool readOnly = false)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(20, yPos),
                Size = new Size(LabelWidth, 20)
            };
            scrollPanel.Controls.Add(label);

            textBox = new TextBox
            {
                Location = new Point(180, yPos),
                Size = new Size(TextBoxWidth, 20),
                ReadOnly = readOnly
            };

            if (readOnly)
            {
                textBox.BackColor = SystemColors.Control;
            }

            scrollPanel.Controls.Add(textBox);
            yPos += FieldSpacing;
        }

        private Panel CreateButtonPanel(int containerWidth)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = SystemColors.Control
            };

            btnOK = new Button
            {
                Text = "Save",
                Size = new Size(100, 35),
                Location = new Point(containerWidth - 220, 15),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOK.Click += BtnOK_Click;
            panel.Controls.Add(btnOK);

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 35),
                Location = new Point(containerWidth - 110, 15),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                DialogResult = DialogResult.Cancel
            };
            panel.Controls.Add(btnCancel);

            return panel;
        }

        private void ConfigureFormButtons()
        {
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void LoadElementData()
        {
            txtName.Text = CostElement.Name;
            txtDescription.Text = CostElement.Description;
            txtChildren.Text = CostElement.Children;

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

            SaveElementData();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void SaveElementData()
        {
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

            if (!ValidateNumericField(txtQtyResult.Text, "Quantity"))
                return false;

            if (!ValidateNumericField(txtUp.Text, "Price"))
                return false;

            return true;
        }

        private bool ValidateNumericField(string value, string fieldName)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (!decimal.TryParse(value, out decimal result) || result < 0)
                {
                    MessageBox.Show($"{fieldName} must be a non-negative number.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            return true;
        }

        private void ElementEditForm_Load_2(object sender, EventArgs e)
        {
        }
    }
}