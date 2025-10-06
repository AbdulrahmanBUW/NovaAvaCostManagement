using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Form for adding/editing cost elements with scrolling support
    /// </summary>
    public partial class ElementEditForm : Form
    {
        public CostElement CostElement { get; private set; }

        private TextBox txtId, txtId2, txtName, txtType, txtText, txtLongText;
        private TextBox txtQty, txtQu, txtUp, txtBimKey, txtDescription;
        private TextBox txtLabel, txtNote, txtColor;
        private ComboBox cmbIfcType, cmbMaterial, cmbDimension, cmbSegmentType;
        private Label lblTotal, lblProperties;
        private Button btnOK, btnCancel, btnGenerateProperties, btnGenerateGuid;

        private Panel scrollPanel;
        private Panel buttonPanel;

        public ElementEditForm() : this(null) { }

        public ElementEditForm(CostElement element)
        {
            InitializeComponent();

            if (element == null)
            {
                CostElement = new CostElement();
                this.Text = "Add New Cost Element";
            }
            else
            {
                CostElement = CloneElement(element);
                this.Text = "Edit Cost Element";
            }

            InitializeCustomComponents();

            if (element != null)
            {
                LoadElementData();
            }
        }

        private void InitializeCustomComponents()
        {
            this.Size = new Size(650, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new Size(650, 550);

            // Main container panel
            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Scrollable content panel
            scrollPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(610, 580),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            mainContainer.Controls.Add(scrollPanel);

            int yPos = 20;
            const int labelWidth = 120;
            const int textBoxWidth = 300;
            const int spacing = 35;

            // ID field (user-editable)
            var lblId = new Label
            {
                Text = "ID:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20)
            };
            scrollPanel.Controls.Add(lblId);

            txtId = new TextBox
            {
                Location = new Point(150, yPos),
                Size = new Size(textBoxWidth, 20),
                Text = CostElement.Id
            };
            scrollPanel.Controls.Add(txtId);
            yPos += spacing;

            // Code (ID2) field with GUID generation button
            var lblId2 = new Label
            {
                Text = "Code (ID2):",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20)
            };
            scrollPanel.Controls.Add(lblId2);

            txtId2 = new TextBox
            {
                Location = new Point(150, yPos),
                Size = new Size(textBoxWidth, 20)
            };
            scrollPanel.Controls.Add(txtId2);

            // GUID generation button
            btnGenerateGuid = new Button
            {
                Text = "Generate GUID",
                Location = new Point(460, yPos),
                Size = new Size(100, 23),
                BackColor = Color.LightBlue
            };
            btnGenerateGuid.Click += BtnGenerateGuid_Click;
            scrollPanel.Controls.Add(btnGenerateGuid);

            yPos += spacing;

            AddLabelAndTextBox("Name:", ref txtName, ref yPos, labelWidth, textBoxWidth, spacing);
            AddLabelAndTextBox("Type:", ref txtType, ref yPos, labelWidth, textBoxWidth, spacing);
            AddLabelAndTextBox("Label:", ref txtLabel, ref yPos, labelWidth, textBoxWidth, spacing);

            // Text field
            var lblText = new Label
            {
                Text = "Text:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20)
            };
            scrollPanel.Controls.Add(lblText);

            txtText = new TextBox
            {
                Location = new Point(150, yPos),
                Size = new Size(textBoxWidth + 110, 20),
                MaxLength = 255
            };
            scrollPanel.Controls.Add(txtText);
            yPos += spacing;

            // Long Text field
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
                Size = new Size(textBoxWidth + 110, 60),
                Multiline = true,
                MaxLength = 2000,
                ScrollBars = ScrollBars.Vertical
            };
            scrollPanel.Controls.Add(txtLongText);
            yPos += 70;

            AddLabelAndTextBox("Quantity:", ref txtQty, ref yPos, labelWidth, textBoxWidth, spacing);
            txtQty.TextChanged += UpdateTotal;

            AddLabelAndTextBox("Unit:", ref txtQu, ref yPos, labelWidth, textBoxWidth, spacing);

            AddLabelAndTextBox("Unit Price:", ref txtUp, ref yPos, labelWidth, textBoxWidth, spacing);
            txtUp.TextChanged += UpdateTotal;

            // Total (read-only)
            var lblTotalLabel = new Label
            {
                Text = "Total:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20)
            };
            scrollPanel.Controls.Add(lblTotalLabel);

            lblTotal = new Label
            {
                Location = new Point(150, yPos),
                Size = new Size(textBoxWidth + 110, 20),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SystemColors.Control,
                Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold)
            };
            scrollPanel.Controls.Add(lblTotal);
            yPos += spacing;

            // IFC Type
            AddLabelAndComboBox("IFC Type:", ref cmbIfcType, ref yPos, labelWidth, textBoxWidth, spacing);
            cmbIfcType.Items.AddRange(new[] { "IFCPIPESEGMENT", "IFCWALL", "IFCBEAM", "IFCSLAB", "IFCDOOR", "IFCWINDOW", "IFCCOLUMN" });
            cmbIfcType.SelectedIndexChanged += (s, e) => UpdatePropertiesDisplay();

            // Material
            AddLabelAndComboBox("Material:", ref cmbMaterial, ref yPos, labelWidth, textBoxWidth, spacing);
            cmbMaterial.Items.AddRange(new[] { "P235HTC1", "Steel", "Concrete", "Wood", "Aluminum", "Plastic" });
            cmbMaterial.TextChanged += (s, e) => UpdatePropertiesDisplay();

            // Dimension
            AddLabelAndComboBox("Dimension:", ref cmbDimension, ref yPos, labelWidth, textBoxWidth, spacing);
            cmbDimension.Items.AddRange(new[] { "DN125", "DN100", "DN150", "200mm", "300mm", "IPE200", "IPE300" });
            cmbDimension.TextChanged += (s, e) => UpdatePropertiesDisplay();

            // Segment Type
            AddLabelAndComboBox("Segment Type:", ref cmbSegmentType, ref yPos, labelWidth, textBoxWidth, spacing);
            cmbSegmentType.Items.AddRange(new[] {
                "DX_CarbonSteel_1.0345 - DIN EN 10216-2",
                "Standard",
                "LoadBearing",
                "Structural",
                "Interior"
            });
            cmbSegmentType.TextChanged += (s, e) => UpdatePropertiesDisplay();

            AddLabelAndTextBox("BIM Key:", ref txtBimKey, ref yPos, labelWidth, textBoxWidth, spacing);
            AddLabelAndTextBox("Description:", ref txtDescription, ref yPos, labelWidth, textBoxWidth, spacing);
            AddLabelAndTextBox("Note:", ref txtNote, ref yPos, labelWidth, textBoxWidth, spacing);
            AddLabelAndTextBox("Color:", ref txtColor, ref yPos, labelWidth, textBoxWidth, spacing);

            // Properties (read-only display)
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
                Size = new Size(textBoxWidth + 110, 40),
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
                Location = new Point(460, yPos),
                Size = new Size(120, 23)
            };
            btnGenerateProperties.Click += BtnGenerateProperties_Click;
            scrollPanel.Controls.Add(btnGenerateProperties);
            yPos += 50;

            // Set the scroll panel height based on content
            scrollPanel.Height = Math.Min(yPos + 20, 580);
            scrollPanel.AutoScrollMinSize = new Size(0, yPos + 20);

            // Button panel at the bottom
            buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = SystemColors.Control
            };

            // OK button
            btnOK = new Button
            {
                Text = "OK",
                Size = new Size(75, 30),
                Location = new Point(mainContainer.Width - 165, 15),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                DialogResult = DialogResult.OK
            };
            btnOK.Click += BtnOK_Click;
            buttonPanel.Controls.Add(btnOK);

            // Cancel button
            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(75, 30),
                Location = new Point(mainContainer.Width - 80, 15),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            buttonPanel.Controls.Add(btnCancel);

            mainContainer.Controls.Add(buttonPanel);
            this.Controls.Add(mainContainer);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            UpdateTotal(null, null);
            UpdatePropertiesDisplay();
        }

        /// <summary>
        /// Generate GUID and put it in the ID2 field
        /// </summary>
        private void BtnGenerateGuid_Click(object sender, EventArgs e)
        {
            string newGuid = Guid.NewGuid().ToString();
            txtId2.Text = newGuid;

            // Visual feedback
            btnGenerateGuid.BackColor = Color.LightGreen;
            var timer = new System.Windows.Forms.Timer { Interval = 500 };
            timer.Tick += (s, args) =>
            {
                btnGenerateGuid.BackColor = Color.LightBlue;
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();

            // Optional: Show message
            MessageBox.Show($"GUID generated: {newGuid}", "GUID Generated",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AddLabelAndTextBox(string labelText, ref TextBox textBox, ref int yPos,
            int labelWidth, int textBoxWidth, int spacing)
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
                Size = new Size(textBoxWidth + 110, 20)
            };
            scrollPanel.Controls.Add(textBox);

            yPos += spacing;
        }

        private void AddLabelAndComboBox(string labelText, ref ComboBox comboBox, ref int yPos,
            int labelWidth, int textBoxWidth, int spacing)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20)
            };
            scrollPanel.Controls.Add(label);

            comboBox = new ComboBox
            {
                Location = new Point(150, yPos),
                Size = new Size(textBoxWidth + 110, 20),
                DropDownStyle = ComboBoxStyle.DropDown
            };
            scrollPanel.Controls.Add(comboBox);

            yPos += spacing;
        }

        private void LoadElementData()
        {
            txtId.Text = CostElement.Id;
            txtId2.Text = CostElement.Id2;
            txtName.Text = CostElement.Name;
            txtType.Text = CostElement.Type;
            txtLabel.Text = CostElement.Label;
            txtText.Text = CostElement.Text;
            txtLongText.Text = CostElement.LongText;
            txtQty.Text = CostElement.Qty.ToString();
            txtQu.Text = CostElement.Qu;
            txtUp.Text = CostElement.Up.ToString();
            txtBimKey.Text = CostElement.BimKey;
            txtDescription.Text = CostElement.Description;
            txtNote.Text = CostElement.Note;
            txtColor.Text = CostElement.Color;
            cmbIfcType.Text = CostElement.IfcType;
            cmbMaterial.Text = CostElement.Material;
            cmbDimension.Text = CostElement.Dimension;
            cmbSegmentType.Text = CostElement.SegmentType;

            // Display existing properties
            if (!string.IsNullOrEmpty(CostElement.Properties))
            {
                lblProperties.Text = CostElement.Properties.Length > 100
                    ? CostElement.Properties.Substring(0, 100) + "..."
                    : CostElement.Properties;
                lblProperties.ForeColor = Color.Black;
            }
        }

        private void UpdateTotal(object sender, EventArgs e)
        {
            if (decimal.TryParse(txtQty?.Text, out decimal quantity) &&
                decimal.TryParse(txtUp?.Text, out decimal unitPrice))
            {
                lblTotal.Text = (quantity * unitPrice).ToString("F2");
            }
            else
            {
                lblTotal.Text = "0.00";
            }
        }

        private void UpdatePropertiesDisplay()
        {
            // This just updates the display preview as you type
            if (lblProperties == null || string.IsNullOrWhiteSpace(cmbIfcType?.Text))
            {
                if (lblProperties != null)
                {
                    lblProperties.Text = "(Click Generate to create properties)";
                    lblProperties.ForeColor = Color.Gray;
                }
                return;
            }

            // Show preview
            try
            {
                var preview = PropertiesSerializer.SerializeProperties(
                    cmbIfcType.Text,
                    cmbMaterial.Text,
                    cmbDimension.Text,
                    cmbSegmentType.Text);

                lblProperties.Text = preview.Length > 100
                    ? preview.Substring(0, 100) + "... (Preview)"
                    : preview + " (Preview)";
                lblProperties.ForeColor = Color.DarkOrange;
            }
            catch
            {
                lblProperties.Text = "(Invalid IFC data)";
                lblProperties.ForeColor = Color.Red;
            }
        }

        private void BtnGenerateProperties_Click(object sender, EventArgs e)
        {
            // Validate IFC Type
            if (string.IsNullOrWhiteSpace(cmbIfcType.Text))
            {
                MessageBox.Show(
                    "Please select an IFC Type before generating properties.",
                    "IFC Type Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                cmbIfcType.Focus();
                return;
            }

            // Warn about missing fields (but don't block generation)
            var missingFields = new List<string>();
            if (string.IsNullOrWhiteSpace(cmbMaterial.Text))
                missingFields.Add("Material");
            if (string.IsNullOrWhiteSpace(cmbDimension.Text))
                missingFields.Add("Dimension");
            if (string.IsNullOrWhiteSpace(cmbSegmentType.Text))
                missingFields.Add("Segment Type");

            if (missingFields.Any())
            {
                var result = MessageBox.Show(
                    $"The following fields are empty:\n- {string.Join("\n- ", missingFields)}\n\n" +
                    "Properties will be generated with empty values for these fields.\n\n" +
                    "Do you want to continue?",
                    "Missing Fields",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;
            }

            try
            {
                // Generate properties
                var properties = PropertiesSerializer.SerializeProperties(
                    cmbIfcType.Text,
                    cmbMaterial.Text,
                    cmbDimension.Text,
                    cmbSegmentType.Text);

                // Update CostElement immediately
                CostElement.IfcType = cmbIfcType.Text;
                CostElement.Material = cmbMaterial.Text;
                CostElement.Dimension = cmbDimension.Text;
                CostElement.SegmentType = cmbSegmentType.Text;
                CostElement.Properties = properties;

                // Update display
                lblProperties.Text = properties.Length > 100
                    ? properties.Substring(0, 100) + "..."
                    : properties;
                lblProperties.ForeColor = Color.Green;

                // Visual feedback
                btnGenerateProperties.BackColor = Color.LightGreen;
                var timer = new System.Windows.Forms.Timer { Interval = 500 };
                timer.Tick += (s, args) =>
                {
                    btnGenerateProperties.BackColor = SystemColors.Control;
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();

                MessageBox.Show(
                    "Properties generated successfully!\n\n" +
                    $"Generated: {properties.Length} characters\n\n" +
                    "The properties have been saved to this element.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblProperties.Text = $"Error: {ex.Message}";
                lblProperties.ForeColor = Color.Red;

                MessageBox.Show(
                    $"Error generating properties:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Validate input first
            if (!ValidateInput())
                return;

            // Update all fields from form controls
            CostElement.Id = txtId.Text.Trim();
            CostElement.Id2 = txtId2.Text.Trim();
            CostElement.Name = txtName.Text.Trim();
            CostElement.Type = txtType.Text.Trim();
            CostElement.Label = txtLabel.Text.Trim();
            CostElement.Text = txtText.Text.Trim();
            CostElement.LongText = txtLongText.Text.Trim();
            CostElement.Qu = txtQu.Text.Trim();
            CostElement.ProcUnit = CostElement.Qu;
            CostElement.BimKey = txtBimKey.Text.Trim();
            CostElement.Description = txtDescription.Text.Trim();
            CostElement.Note = txtNote.Text.Trim();
            CostElement.Color = txtColor.Text.Trim();
            CostElement.IfcType = cmbIfcType.Text.Trim();
            CostElement.Material = cmbMaterial.Text.Trim();
            CostElement.Dimension = cmbDimension.Text.Trim();
            CostElement.SegmentType = cmbSegmentType.Text.Trim();

            if (decimal.TryParse(txtQty.Text, out decimal quantity))
                CostElement.Qty = quantity;
            else
                CostElement.Qty = 0;

            if (decimal.TryParse(txtUp.Text, out decimal unitPrice))
                CostElement.Up = unitPrice;
            else
                CostElement.Up = 0;

            // Always calculate fields to ensure Sum is correct
            CostElement.RecalculateSum();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private bool ValidateInput()
        {
            // Validate ID field
            if (string.IsNullOrWhiteSpace(txtId.Text))
            {
                MessageBox.Show("Please enter an ID for the element.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtId.Focus();
                return false;
            }

            // Only keep basic format validation for numeric fields and color
            if (!string.IsNullOrWhiteSpace(txtQty.Text) &&
                (!decimal.TryParse(txtQty.Text, out decimal quantity) || quantity < 0))
            {
                MessageBox.Show("Please enter a valid quantity (non-negative number).", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtQty.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(txtUp.Text) &&
                (!decimal.TryParse(txtUp.Text, out decimal unitPrice) || unitPrice < 0))
            {
                MessageBox.Show("Please enter a valid unit price (non-negative number).", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUp.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(txtColor.Text))
            {
                try
                {
                    ColorTranslator.FromHtml(txtColor.Text.Trim());
                }
                catch
                {
                    MessageBox.Show("Please enter a valid color format (e.g., #3498DB).", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtColor.Focus();
                    return false;
                }
            }

            return true;
        }

        private CostElement CloneElement(CostElement source)
        {
            var clone = new CostElement
            {
                Version = source.Version,
                Id = source.Id,
                Title = source.Title,
                Label = source.Label,
                Criteria = source.Criteria,
                Created = source.Created,
                Id2 = source.Id2,
                Type = source.Type,
                Name = source.Name,
                Description = source.Description,
                Properties = source.Properties,
                Children = source.Children,
                Openings = source.Openings,
                Created3 = source.Created3,
                Label4 = source.Label4,
                Id5 = source.Id5,
                Parent = source.Parent,
                Order = source.Order,
                Ident = source.Ident,
                BimKey = source.BimKey,
                Text = source.Text,
                LongText = source.LongText,
                TextSys = source.TextSys,
                TextKey = source.TextKey,
                StlNo = source.StlNo,
                OutlineTextFree = source.OutlineTextFree,
                Qty = source.Qty,
                QtyResult = source.QtyResult,
                Qu = source.Qu,
                Up = source.Up,
                UpResult = source.UpResult,
                UpBkdn = source.UpBkdn,
                UpComp1 = source.UpComp1,
                UpComp2 = source.UpComp2,
                UpComp3 = source.UpComp3,
                UpComp4 = source.UpComp4,
                UpComp5 = source.UpComp5,
                UpComp6 = source.UpComp6,
                TimeQu = source.TimeQu,
                It = source.It,
                Vat = source.Vat,
                VatValue = source.VatValue,
                Tax = source.Tax,
                TaxValue = source.TaxValue,
                ItGross = source.ItGross,
                Sum = source.Sum,
                Vob = source.Vob,
                VobFormula = source.VobFormula,
                VobCondition = source.VobCondition,
                VobType = source.VobType,
                VobFactor = source.VobFactor,
                On = source.On,
                PercTotal = source.PercTotal,
                Marked = source.Marked,
                PercMarked = source.PercMarked,
                ProcUnit = source.ProcUnit,
                Color = source.Color,
                Note = source.Note,
                Additional = source.Additional,
                Id6 = source.Id6,
                FilePath = source.FilePath,
                FileName = source.FileName,
                Data = source.Data,
                CatalogName = source.CatalogName,
                CatalogType = source.CatalogType,
                Name7 = source.Name7,
                Number = source.Number,
                Reference = source.Reference,
                Filter = source.Filter,
                IfcType = source.IfcType,
                Material = source.Material,
                Dimension = source.Dimension,
                SegmentType = source.SegmentType,
                AdditionalData = new Dictionary<string, object>(source.AdditionalData)
            };

            return clone;
        }

        private void ElementEditForm_Load(object sender, EventArgs e)
        {
        }

        private void ElementEditForm_Load_1(object sender, EventArgs e)
        {
        }

        private void ElementEditForm_Load_2(object sender, EventArgs e)
        {
        }
    }
}