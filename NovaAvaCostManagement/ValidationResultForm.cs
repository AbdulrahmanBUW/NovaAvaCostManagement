using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Form to display validation results
    /// </summary>
    public partial class ValidationResultForm : Form
    {
        private ValidationResult validationResult;
        private Label lblSummary;
        private TextBox txtDetails;
        private Button btnClose, btnFixFirst, btnContinue;

        public ValidationResultForm(ValidationResult result)
        {
            validationResult = result;
            InitializeComponent();
            InitializeCustomComponents();
            LoadValidationResults();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Validation Results";
            this.Size = new Size(600, 450);  // Smaller height
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;  // Make resizable
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.MinimumSize = new Size(400, 300);

            // Summary label
            lblSummary = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(560, 40),
                Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold),
                Text = GetSummaryText()
            };
            this.Controls.Add(lblSummary);

            // Details text box
            txtDetails = new TextBox
            {
                Location = new Point(20, 70),
                Size = new Size(560, 350),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F)
            };
            this.Controls.Add(txtDetails);

            // Buttons
            btnClose = new Button
            {
                Text = "Close",
                Location = new Point(505, 430),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnClose);

            if (validationResult.HasErrors)
            {
                btnFixFirst = new Button
                {
                    Text = "Fix First Error",
                    Location = new Point(350, 430),
                    Size = new Size(100, 30),
                    DialogResult = DialogResult.Retry
                };
                this.Controls.Add(btnFixFirst);

                btnContinue = new Button
                {
                    Text = "Continue Anyway",
                    Location = new Point(220, 430),
                    Size = new Size(120, 30),
                    DialogResult = DialogResult.Yes,
                    BackColor = Color.Orange
                };
                this.Controls.Add(btnContinue);
            }
            else
            {
                btnContinue = new Button
                {
                    Text = "OK",
                    Location = new Point(420, 430),
                    Size = new Size(75, 30),
                    DialogResult = DialogResult.OK
                };
                this.Controls.Add(btnContinue);
            }

            this.CancelButton = btnClose;
            this.AcceptButton = validationResult.HasErrors ? btnClose : btnContinue;
        }

        private string GetSummaryText()
        {
            if (validationResult.HasErrors)
            {
                return $"Validation Failed: {validationResult.Errors.Count} errors, {validationResult.Warnings.Count} warnings";
            }
            else if (validationResult.HasWarnings)
            {
                return $"Validation Passed with Warnings: {validationResult.Warnings.Count} warnings";
            }
            else
            {
                return "Validation Passed: No errors or warnings found";
            }
        }

        private void ValidationResultForm_Load_1(object sender, EventArgs e)
        {

        }

        private void LoadValidationResults()
        {
            var details = new System.Text.StringBuilder();

            if (validationResult.HasErrors)
            {
                details.AppendLine("=== ERRORS ===");
                foreach (var error in validationResult.Errors)
                {
                    details.AppendLine($"ERROR: {error}");
                }
                details.AppendLine();
            }

            if (validationResult.HasWarnings)
            {
                details.AppendLine("=== WARNINGS ===");
                foreach (var warning in validationResult.Warnings)
                {
                    details.AppendLine($"WARNING: {warning}");
                }
                details.AppendLine();
            }

            if (!validationResult.HasErrors && !validationResult.HasWarnings)
            {
                details.AppendLine("All elements passed validation successfully!");
                details.AppendLine("The data is ready for export.");
            }

            txtDetails.Text = details.ToString();
        }

        // Empty event handler for designer compatibility
        private void ValidationResultForm_Load(object sender, EventArgs e)
        {
            // This method is required by the designer but not used
        }
    }
}