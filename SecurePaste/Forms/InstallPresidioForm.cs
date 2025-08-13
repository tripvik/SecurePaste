using SecurePaste.Services;
using System.Diagnostics;

namespace SecurePaste.Forms
{
    public partial class InstallPresidioForm : Form
    {
        private readonly ConfigurationService _configService;
        private PresidioService _presidioService;

        // Controls
        private RichTextBox? txtInstructions;
        private Button? btnInstallBasic;
        private Button? btnInstallFull;
        private Button? btnTestInstallation;
        private Button? btnClose;
        private ProgressBar? progressBar;
        private Label? lblStatus;

        public InstallPresidioForm(ConfigurationService configService)
        {
            _configService = configService;
            _presidioService = new PresidioService(_configService.GetConfiguration());
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Install Microsoft Presidio";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Instructions
            txtInstructions = new RichTextBox
            {
                Location = new Point(20, 20),
                Size = new Size(540, 250),
                ReadOnly = true,
                BackColor = SystemColors.Control,
                Text = GetInstructionsText()
            };

            // Basic installation button
            btnInstallBasic = new Button
            {
                Text = "Install Basic Presidio",
                Location = new Point(20, 290),
                Size = new Size(150, 30),
                BackColor = Color.LightBlue
            };
            btnInstallBasic.Click += BtnInstallBasic_Click;

            // Full installation button
            btnInstallFull = new Button
            {
                Text = "Install Full Presidio + SpaCy",
                Location = new Point(180, 290),
                Size = new Size(180, 30),
                BackColor = Color.LightGreen
            };
            btnInstallFull.Click += BtnInstallFull_Click;

            // Test installation button
            btnTestInstallation = new Button
            {
                Text = "Test Installation",
                Location = new Point(380, 290),
                Size = new Size(120, 30)
            };
            btnTestInstallation.Click += BtnTestInstallation_Click;

            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new Point(20, 340),
                Size = new Size(540, 25),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            // Status label
            lblStatus = new Label
            {
                Location = new Point(20, 375),
                Size = new Size(540, 40),
                Text = "Ready to install Presidio",
                ForeColor = Color.Blue
            };

            // Close button
            btnClose = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Location = new Point(480, 430),
                Size = new Size(80, 25)
            };

            // Add all controls
            this.Controls.AddRange(new Control[]
            {
                txtInstructions, btnInstallBasic, btnInstallFull, 
                btnTestInstallation, progressBar, lblStatus, btnClose
            });
        }

        private string GetInstructionsText()
        {
            return @"Microsoft Presidio Installation Guide

Presidio is a data protection and de-identification SDK that provides fast identification and anonymization modules for private entities in text.

REQUIREMENTS:
• Python 3.7 or higher
• pip package manager

INSTALLATION OPTIONS:

1. BASIC INSTALLATION (Recommended for most users):
   • Installs presidio-analyzer and presidio-anonymizer
   • Uses basic text processing
   • Smaller download size
   • Good for most PII detection needs

2. FULL INSTALLATION (Better accuracy):
   • Includes everything from basic installation
   • Adds SpaCy NLP library with English model
   • Better entity recognition accuracy
   • Larger download size (~50MB+)

MANUAL INSTALLATION:
If automatic installation fails, you can manually run these commands in Command Prompt or PowerShell:

Basic:
pip install presidio-analyzer presidio-anonymizer

Full:
pip install presidio-analyzer presidio-anonymizer spacy
python -m spacy download en_core_web_sm

TROUBLESHOOTING:
• Ensure Python is in your system PATH
• Try running Command Prompt as Administrator
• Update pip: python -m pip install --upgrade pip
• Check your internet connection

Click one of the installation buttons below to begin automatic installation.";
        }

        private async void BtnInstallBasic_Click(object? sender, EventArgs e)
        {
            await RunInstallation(false);
        }

        private async void BtnInstallFull_Click(object? sender, EventArgs e)
        {
            await RunInstallation(true);
        }

        private async Task RunInstallation(bool fullInstallation)
        {
            var config = _configService.GetConfiguration();
            
            SetButtonsEnabled(false);
            if (progressBar != null) progressBar.Visible = true;
            
            if (lblStatus != null)
            {
                lblStatus.ForeColor = Color.Blue;
                lblStatus.Text = fullInstallation ? "Installing Presidio with SpaCy..." : "Installing basic Presidio...";
            }

            try
            {
                var packages = new List<string> { "presidio-analyzer", "presidio-anonymizer" };
                
                if (fullInstallation)
                {
                    packages.Add("spacy");
                }

                // Step 1: Install basic packages
                if (lblStatus != null) lblStatus.Text = "Installing Presidio packages...";
                var installResult = await _presidioService.InstallPythonPackagesAsync(packages.ToArray());

                if (!installResult.Success)
                {
                    throw new Exception($"Package installation failed: {installResult.StandardError}");
                }

                // Step 2: If full installation, download SpaCy model
                if (fullInstallation)
                {
                    if (lblStatus != null) lblStatus.Text = "Downloading SpaCy English model...";
                    
                    var spacyModelOptions = new CliOptions
                    {
                        Timeout = TimeSpan.FromMinutes(10)
                    };
                    
                    var spacyResult = await CliWrapper.ExecuteAsync(
                        config.PythonPath, 
                        "-m spacy download en_core_web_sm", 
                        spacyModelOptions
                    );

                    if (!spacyResult.Success)
                    {
                        // SpaCy model download is optional, just warn the user
                        MessageBox.Show(
                            "Presidio installed successfully, but SpaCy model download failed.\n" +
                            "You can manually install it later with:\npython -m spacy download en_core_web_sm",
                            "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                // Step 3: Test installation
                if (lblStatus != null) lblStatus.Text = "Testing installation...";
                var isWorking = await _presidioService.CheckPresidioInstallationAsync();

                if (isWorking)
                {
                    if (lblStatus != null)
                    {
                        lblStatus.ForeColor = Color.Green;
                        lblStatus.Text = "Installation completed successfully!";
                    }
                    
                    MessageBox.Show("Presidio has been installed successfully!\n\nYou can now use SecurePaste to anonymize clipboard content.", 
                        "Installation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Update configuration
                    config.PresidioInstalled = true;
                    _configService.SaveConfiguration(config);
                }
                else
                {
                    if (lblStatus != null)
                    {
                        lblStatus.ForeColor = Color.Orange;
                        lblStatus.Text = "Installation completed but verification failed.";
                    }
                    
                    MessageBox.Show("Installation completed but Presidio verification failed.\n" +
                        "Please test manually or check Python/Presidio installation.", 
                        "Installation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                if (lblStatus != null)
                {
                    lblStatus.ForeColor = Color.Red;
                    lblStatus.Text = "Installation failed.";
                }
                
                ShowErrorDialog("Installation Error", $"An error occurred during installation:\n\n{ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
                if (progressBar != null) progressBar.Visible = false;
            }
        }

        private async void BtnTestInstallation_Click(object? sender, EventArgs e)
        {
            SetButtonsEnabled(false);
            if (progressBar != null) progressBar.Visible = true;
            if (lblStatus != null)
            {
                lblStatus.ForeColor = Color.Blue;
                lblStatus.Text = "Testing Presidio installation...";
            }

            try
            {
                var config = _configService.GetConfiguration();
                
                // Test Python first
                var pythonWorking = await _presidioService.TestPythonInstallationAsync();
                if (!pythonWorking)
                {
                    if (lblStatus != null)
                    {
                        lblStatus.ForeColor = Color.Red;
                        lblStatus.Text = "Python installation not found or not working.";
                    }
                    
                    MessageBox.Show("Python installation not found or not working.\n\n" +
                        "Please check your Python path in the configuration.", 
                        "Python Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Test Presidio
                var presidioWorking = await _presidioService.CheckPresidioInstallationAsync();
                
                if (presidioWorking)
                {
                    if (lblStatus != null)
                    {
                        lblStatus.ForeColor = Color.Green;
                        lblStatus.Text = "Presidio is installed and working correctly!";
                    }
                    
                    // Update configuration
                    config.PresidioInstalled = true;
                    _configService.SaveConfiguration(config);
                    
                    // Get Python version for additional info
                    var pythonVersion = await _presidioService.GetPythonVersionAsync();
                    var versionInfo = pythonVersion != null ? $"\n\n{pythonVersion}" : "";
                    
                    MessageBox.Show($"Presidio is installed and working correctly!{versionInfo}", 
                        "Test Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    if (lblStatus != null)
                    {
                        lblStatus.ForeColor = Color.Red;
                        lblStatus.Text = "Presidio is not installed or not working properly.";
                    }
                    
                    MessageBox.Show("Presidio is not installed or not working properly.\n\n" +
                        "Please try one of the installation options above.", 
                        "Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                if (lblStatus != null)
                {
                    lblStatus.ForeColor = Color.Red;
                    lblStatus.Text = "Error testing installation.";
                }
                
                MessageBox.Show($"Error testing Presidio installation:\n\n{ex.Message}", 
                    "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetButtonsEnabled(true);
                if (progressBar != null) progressBar.Visible = false;
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            if (btnInstallBasic != null) btnInstallBasic.Enabled = enabled;
            if (btnInstallFull != null) btnInstallFull.Enabled = enabled;
            if (btnTestInstallation != null) btnTestInstallation.Enabled = enabled;
        }

        private void ShowErrorDialog(string title, string message)
        {
            using var form = new Form
            {
                Text = title,
                Size = new Size(600, 400),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable
            };

            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Text = message,
                Font = new Font("Consolas", 9)
            };

            var buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom
            };

            var btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(510, 15),
                Size = new Size(75, 25)
            };

            buttonPanel.Controls.Add(btnOK);
            form.Controls.AddRange(new Control[] { textBox, buttonPanel });
            
            form.ShowDialog();
        }
    }
}