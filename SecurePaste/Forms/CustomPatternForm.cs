using SecurePaste.Models;
using System.Text.RegularExpressions;

namespace SecurePaste.Forms
{
    public partial class CustomPatternForm : Form
    {
        private CustomPatternConfiguration? _pattern;
        
        // Controls
        private TextBox? txtName;
        private TextBox? txtEntityType;
        private TextBox? txtPattern;
        private NumericUpDown? numConfidence;
        private ComboBox? cmbMethod;
        private TextBox? txtCustomReplacement;
        private TextBox? txtDescription;
        private TextBox? txtTestText;
        private Button? btnTest;
        private Label? lblTestResult;
        private Button? btnOK;
        private Button? btnCancel;

        public CustomPatternForm(CustomPatternConfiguration? pattern = null)
        {
            _pattern = pattern;
            InitializeComponent();
            LoadPattern();
        }

        private void InitializeComponent()
        {
            this.Text = _pattern == null ? "Add Custom Pattern" : "Edit Custom Pattern";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Name
            var lblName = new Label
            {
                Text = "Pattern Name:",
                Location = new Point(20, 20),
                Size = new Size(100, 23)
            };

            txtName = new TextBox
            {
                Location = new Point(130, 18),
                Size = new Size(400, 23),
                PlaceholderText = "e.g., API Key Pattern"
            };

            // Entity Type
            var lblEntityType = new Label
            {
                Text = "Entity Type:",
                Location = new Point(20, 55),
                Size = new Size(100, 23)
            };

            txtEntityType = new TextBox
            {
                Location = new Point(130, 53),
                Size = new Size(400, 23),
                PlaceholderText = "e.g., API_KEY"
            };

            // Pattern
            var lblPattern = new Label
            {
                Text = "Regex Pattern:",
                Location = new Point(20, 90),
                Size = new Size(100, 23)
            };

            txtPattern = new TextBox
            {
                Location = new Point(130, 88),
                Size = new Size(400, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "Example: (?i)\\b(?:api[_-]?key|apikey)\\s*[:=]\\s*['\"]?([a-zA-Z0-9_-]{20,})['\"]?"
            };

            // Add a help button for regex patterns
            var btnHelp = new Button
            {
                Text = "?",
                Location = new Point(540, 88),
                Size = new Size(25, 25),
                BackColor = Color.LightBlue
            };
            btnHelp.Click += BtnHelp_Click;

            // Confidence Score
            var lblConfidence = new Label
            {
                Text = "Confidence Score:",
                Location = new Point(20, 165),
                Size = new Size(100, 23)
            };

            numConfidence = new NumericUpDown
            {
                Location = new Point(130, 163),
                Size = new Size(100, 23),
                Minimum = 0.1m,
                Maximum = 1.0m,
                DecimalPlaces = 2,
                Increment = 0.05m,
                Value = 0.8m
            };

            // Anonymization Method
            var lblMethod = new Label
            {
                Text = "Method:",
                Location = new Point(250, 165),
                Size = new Size(60, 23)
            };

            cmbMethod = new ComboBox
            {
                Location = new Point(320, 163),
                Size = new Size(100, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbMethod.Items.AddRange(new[] { "redact", "replace", "mask", "hash" });
            cmbMethod.SelectedItem = "redact";

            // Custom Replacement
            var lblCustomReplacement = new Label
            {
                Text = "Custom Replacement:",
                Location = new Point(20, 200),
                Size = new Size(120, 23)
            };

            txtCustomReplacement = new TextBox
            {
                Location = new Point(150, 198),
                Size = new Size(380, 23),
                PlaceholderText = "Optional: custom replacement text"
            };

            // Description
            var lblDescription = new Label
            {
                Text = "Description:",
                Location = new Point(20, 235),
                Size = new Size(100, 23)
            };

            txtDescription = new TextBox
            {
                Location = new Point(130, 233),
                Size = new Size(400, 40),
                Multiline = true,
                PlaceholderText = "Optional: describe what this pattern detects"
            };

            // Test section
            var lblTest = new Label
            {
                Text = "Test Pattern:",
                Location = new Point(20, 285),
                Size = new Size(100, 23),
                Font = new Font(this.Font, FontStyle.Bold)
            };

            var lblTestText = new Label
            {
                Text = "Test Text:",
                Location = new Point(20, 315),
                Size = new Size(100, 23)
            };

            txtTestText = new TextBox
            {
                Location = new Point(130, 313),
                Size = new Size(300, 23),
                PlaceholderText = "Enter text to test the pattern against"
            };

            btnTest = new Button
            {
                Text = "Test",
                Location = new Point(440, 312),
                Size = new Size(60, 25)
            };
            btnTest.Click += BtnTest_Click;

            lblTestResult = new Label
            {
                Location = new Point(130, 345),
                Size = new Size(400, 40),
                Text = "Test your pattern to see if it works correctly",
                ForeColor = Color.Gray,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Buttons
            btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(375, 410),
                Size = new Size(75, 25)
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(455, 410),
                Size = new Size(75, 25)
            };

            this.Controls.AddRange(new Control[] 
            {
                lblName, txtName,
                lblEntityType, txtEntityType,
                lblPattern, txtPattern, btnHelp,
                lblConfidence, numConfidence,
                lblMethod, cmbMethod,
                lblCustomReplacement, txtCustomReplacement,
                lblDescription, txtDescription,
                lblTest, lblTestText, txtTestText, btnTest, lblTestResult,
                btnOK, btnCancel
            });
        }

        private void LoadPattern()
        {
            if (_pattern == null) return;

            if (txtName != null) txtName.Text = _pattern.Name;
            if (txtEntityType != null) txtEntityType.Text = _pattern.EntityType;
            if (txtPattern != null) txtPattern.Text = _pattern.Pattern;
            if (numConfidence != null) numConfidence.Value = (decimal)_pattern.ConfidenceScore;
            if (cmbMethod != null) cmbMethod.SelectedItem = _pattern.AnonymizationMethod;
            if (txtCustomReplacement != null) txtCustomReplacement.Text = _pattern.CustomReplacement ?? "";
            if (txtDescription != null) txtDescription.Text = _pattern.Description ?? "";
        }

        private void BtnTest_Click(object? sender, EventArgs e)
        {
            if (txtPattern?.Text == null || txtTestText?.Text == null || lblTestResult == null)
                return;

            try
            {
                var regex = new Regex(txtPattern.Text, RegexOptions.IgnoreCase);
                var matches = regex.Matches(txtTestText.Text);

                if (matches.Count > 0)
                {
                    lblTestResult.Text = $"? Found {matches.Count} match(es): {string.Join(", ", matches.Cast<Match>().Select(m => m.Value))}";
                    lblTestResult.ForeColor = Color.Green;
                }
                else
                {
                    lblTestResult.Text = "No matches found";
                    lblTestResult.ForeColor = Color.Orange;
                }
            }
            catch (Exception ex)
            {
                lblTestResult.Text = $"? Regex Error: {ex.Message}";
                lblTestResult.ForeColor = Color.Red;
            }
        }

        private async void BtnOK_Click(object? sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            // Additional server-side validation via Python if available
            try
            {
                var tempPattern = GetPattern();
                if (tempPattern != null)
                {
                    // Note: This would require access to IPresidioService
                    // For now, we'll rely on client-side validation
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Pattern validation failed: {ex.Message}", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtName?.Text))
            {
                MessageBox.Show("Please enter a pattern name.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName?.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtEntityType?.Text))
            {
                MessageBox.Show("Please enter an entity type.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEntityType?.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPattern?.Text))
            {
                MessageBox.Show("Please enter a regex pattern.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPattern?.Focus();
                return false;
            }

            // Test regex validity
            try
            {
                var regex = new Regex(txtPattern.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid regex pattern: {ex.Message}", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPattern?.Focus();
                return false;
            }

            return true;
        }

        private void BtnHelp_Click(object? sender, EventArgs e)
        {
            var helpText = @"Regex Pattern Help:

Common Patterns:
• API Keys: (?i)\b(?:api[_-]?key|apikey)\s*[:=]\s*['""]?([a-zA-Z0-9_-]{20,})['""]?
• JWT Tokens: \b(eyJ[a-zA-Z0-9_-]*\.eyJ[a-zA-Z0-9_-]*\.[a-zA-Z0-9_-]*)\b
• Database URLs: (?i)(?:server|host|database)\s*=\s*[^;]+;.*(?:password|pwd)\s*=\s*[^;]+
• Credit Cards: \b(?:\d{4}[-\s]?){3}\d{4}\b
• Email Addresses: \b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b

Tips:
• Use (?i) at the start for case-insensitive matching
• Use \b for word boundaries
• Use parentheses () to capture the sensitive part
• Test your pattern before saving!

Regex Groups:
• The pattern should capture the sensitive data in a group ()
• Everything in the first capturing group will be anonymized";

            MessageBox.Show(helpText, "Regex Pattern Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public CustomPatternConfiguration? GetPattern()
        {
            if (!ValidateInput()) return null;

            return new CustomPatternConfiguration
            {
                Name = txtName?.Text ?? "",
                EntityType = txtEntityType?.Text ?? "",
                Pattern = txtPattern?.Text ?? "",
                ConfidenceScore = (double)(numConfidence?.Value ?? 0.8m),
                AnonymizationMethod = cmbMethod?.SelectedItem?.ToString() ?? "redact",
                CustomReplacement = string.IsNullOrWhiteSpace(txtCustomReplacement?.Text) ? null : txtCustomReplacement.Text,
                Description = string.IsNullOrWhiteSpace(txtDescription?.Text) ? null : txtDescription.Text,
                Enabled = true
            };
        }
    }
}