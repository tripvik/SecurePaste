using SecurePaste.Models;
using SecurePaste.Services;
using Newtonsoft.Json;

namespace SecurePaste.Forms
{
    public partial class PatternTestForm : Form
    {
        private readonly IPresidioService _presidioService;
        private readonly CustomPatternConfiguration _pattern;
        
        // Controls
        private TextBox? txtTestInput;
        private TextBox? txtResult;
        private Button? btnTest;
        private Label? lblPatternInfo;
        private Button? btnClose;

        public PatternTestForm(IPresidioService presidioService, CustomPatternConfiguration pattern)
        {
            _presidioService = presidioService;
            _pattern = pattern;
            InitializeComponent();
            LoadPatternInfo();
        }

        private void InitializeComponent()
        {
            this.Text = $"Test Pattern: {_pattern.Name}";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblPatternInfo = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(560, 60),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.LightBlue
            };

            var lblInput = new Label
            {
                Text = "Test Input Text:",
                Location = new Point(20, 100),
                Size = new Size(100, 23)
            };

            txtTestInput = new TextBox
            {
                Location = new Point(20, 125),
                Size = new Size(420, 100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "Enter text to test the pattern against..."
            };

            var btnExamples = new Button
            {
                Text = "Examples",
                Location = new Point(450, 125),
                Size = new Size(70, 25)
            };
            btnExamples.Click += BtnExamples_Click;

            btnTest = new Button
            {
                Text = "Test Pattern",
                Location = new Point(20, 240),
                Size = new Size(100, 30)
            };
            btnTest.Click += BtnTest_Click;

            var lblResult = new Label
            {
                Text = "Test Result:",
                Location = new Point(20, 285),
                Size = new Size(100, 23)
            };

            txtResult = new TextBox
            {
                Location = new Point(20, 310),
                Size = new Size(520, 100),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White
            };

            btnClose = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Location = new Point(465, 430),
                Size = new Size(75, 25)
            };

            this.Controls.AddRange(new Control[] 
            {
                lblPatternInfo,
                lblInput, txtTestInput, btnExamples,
                btnTest,
                lblResult, txtResult,
                btnClose
            });
        }

        private void LoadPatternInfo()
        {
            if (lblPatternInfo == null) return;

            lblPatternInfo.Text = $"Pattern: {_pattern.Name}\n" +
                                 $"Entity Type: {_pattern.EntityType}\n" +
                                 $"Confidence: {_pattern.ConfidenceScore:P0} | Method: {_pattern.AnonymizationMethod}";
        }

        private async void BtnTest_Click(object? sender, EventArgs e)
        {
            if (txtTestInput?.Text == null || txtResult == null || btnTest == null)
                return;

            if (string.IsNullOrWhiteSpace(txtTestInput.Text))
            {
                MessageBox.Show("Please enter some test text.", "No Input", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnTest.Enabled = false;
            btnTest.Text = "Testing...";
            txtResult.Text = "Testing pattern...";

            try
            {
                var resultJson = await _presidioService.TestCustomPatternAsync(txtTestInput.Text, _pattern);
                var result = JsonConvert.DeserializeObject<dynamic>(resultJson);
                
                if (result?.success == true)
                {
                    var originalText = txtTestInput.Text;
                    var anonymizedText = result.anonymized_text?.ToString() ?? originalText;
                    var entitiesFound = result.entities_found ?? new { };
                    var totalEntities = result.total_entities ?? 0;

                    var resultText = $"Original Text:\n{originalText}\n\n";
                    resultText += $"Anonymized Text:\n{anonymizedText}\n\n";
                    resultText += $"Entities Found: {totalEntities}\n";
                    
                    if (totalEntities > 0)
                    {
                        resultText += $"Pattern matched! Found {totalEntities} occurrence(s) of {_pattern.EntityType}\n";
                        
                        if (result.analyzer_results != null)
                        {
                            resultText += "\nDetection Details:\n";
                            foreach (var detection in result.analyzer_results)
                            {
                                resultText += $"- Text: '{detection.text}' | Score: {detection.score:F2} | Position: {detection.start}-{detection.end}\n";
                            }
                        }
                    }
                    else
                    {
                        resultText += "No matches found with this pattern.";
                    }

                    txtResult.Text = resultText;
                }
                else
                {
                    txtResult.Text = $"Error testing pattern:\n{result?.error ?? "Unknown error"}";
                }
            }
            catch (Exception ex)
            {
                txtResult.Text = $"Error testing pattern:\n{ex.Message}";
            }
            finally
            {
                btnTest.Enabled = true;
                btnTest.Text = "Test Pattern";
            }
        }

        private void BtnExamples_Click(object? sender, EventArgs e)
        {
            if (txtTestInput == null) return;

            var examples = new Dictionary<string, string>
            {
                ["API Key"] = "Here's my API key: api_key=abc123def456ghi789 for the service.",
                ["JWT Token"] = "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
                ["Database Connection"] = "Server=localhost;Database=mydb;User Id=admin;Password=mySecretPass123;",
                ["Credit Card"] = "Please charge 4532-1234-5678-9012 for the purchase.",
                ["Password"] = "Login credentials: username=john password=myPassword123",
                ["Email"] = "Contact us at support@example.com or admin@test.org",
                ["Mixed"] = "API: key=xyz789abc, JWT: eyJ..., Email: user@test.com, Password: pwd=secret123"
            };

            using var exampleForm = new Form
            {
                Text = "Example Test Texts",
                Size = new Size(600, 400),
                StartPosition = FormStartPosition.CenterParent
            };

            var listBox = new ListBox
            {
                Location = new Point(10, 10),
                Size = new Size(150, 320),
                DisplayMember = "Key"
            };

            var textBox = new TextBox
            {
                Location = new Point(170, 10),
                Size = new Size(400, 280),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            var btnUse = new Button
            {
                Text = "Use This Example",
                Location = new Point(170, 300),
                Size = new Size(120, 30)
            };

            var btnClose = new Button
            {
                Text = "Close",
                Location = new Point(495, 300),
                Size = new Size(75, 30)
            };

            foreach (var example in examples)
            {
                listBox.Items.Add(new KeyValuePair<string, string>(example.Key, example.Value));
            }

            listBox.SelectedIndexChanged += (s, e) =>
            {
                if (listBox.SelectedItem is KeyValuePair<string, string> selected)
                {
                    textBox.Text = selected.Value;
                }
            };

            btnUse.Click += (s, e) =>
            {
                txtTestInput.Text = textBox.Text;
                exampleForm.Close();
            };

            btnClose.Click += (s, e) => exampleForm.Close();

            exampleForm.Controls.AddRange(new Control[] { listBox, textBox, btnUse, btnClose });
            exampleForm.ShowDialog();
        }
    }
}