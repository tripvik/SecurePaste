using SecurePaste.Models;
using SecurePaste.Services;

namespace SecurePaste.Forms
{
    public partial class ConfigurationForm : Form
    {
        private readonly IConfigurationService _configService;
        private readonly IPresidioService _presidioService;
        private Configuration _configuration;

        // Controls
        private CheckBox? chkEnabled;
        private CheckBox? chkNotifications;
        private CheckBox? chkAutoStart;
        private TrackBar? trackConfidence;
        private Label? lblConfidence;
        private TextBox? txtPythonPath;
        private Button? btnBrowsePython;
        private ComboBox? cmbLanguage;
        private DataGridView? dgvEntities;
        private Button? btnSave;
        private Button? btnCancel;
        private Button? btnReset;
        private Button? btnTestPython;

        public ConfigurationForm(IConfigurationService configService, IPresidioService presidioService)
        {
            _configService = configService;
            _presidioService = presidioService;
            _configuration = _configService.GetConfiguration();
            
            InitializeComponent();
            LoadConfiguration();
        }

        private void InitializeComponent()
        {
            this.Text = "SecurePaste Configuration";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // General Tab
            var generalTab = new TabPage("General");
            CreateGeneralTab(generalTab);
            tabControl.TabPages.Add(generalTab);

            // Entities Tab
            var entitiesTab = new TabPage("Entity Detection");
            CreateEntitiesTab(entitiesTab);
            tabControl.TabPages.Add(entitiesTab);

            // Python Tab
            var pythonTab = new TabPage("Python Configuration");
            CreatePythonTab(pythonTab);
            tabControl.TabPages.Add(pythonTab);

            this.Controls.Add(tabControl);

            // Buttons
            var buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom
            };

            btnSave = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Location = new Point(430, 15),
                Size = new Size(75, 23)
            };
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(515, 15),
                Size = new Size(75, 23)
            };

            btnReset = new Button
            {
                Text = "Reset to Defaults",
                Location = new Point(10, 15),
                Size = new Size(120, 23)
            };
            btnReset.Click += BtnReset_Click;

            buttonPanel.Controls.AddRange(new Control[] { btnSave, btnCancel, btnReset });
            this.Controls.Add(buttonPanel);
        }

        private void CreateGeneralTab(TabPage tab)
        {
            chkEnabled = new CheckBox
            {
                Text = "Enable clipboard anonymization",
                Location = new Point(20, 20),
                Size = new Size(250, 23),
                Checked = _configuration.Enabled
            };

            chkNotifications = new CheckBox
            {
                Text = "Show notifications",
                Location = new Point(20, 50),
                Size = new Size(200, 23),
                Checked = _configuration.NotificationsEnabled
            };

            chkAutoStart = new CheckBox
            {
                Text = "Start with Windows",
                Location = new Point(20, 80),
                Size = new Size(200, 23),
                Checked = _configuration.AutoStart
            };

            var lblConfidenceLabel = new Label
            {
                Text = "Confidence Threshold:",
                Location = new Point(20, 120),
                Size = new Size(150, 23)
            };

            trackConfidence = new TrackBar
            {
                Location = new Point(20, 150),
                Size = new Size(300, 45),
                Minimum = 10,
                Maximum = 100,
                Value = (int)(_configuration.ConfidenceThreshold * 100),
                TickFrequency = 10,
                LargeChange = 10,
                SmallChange = 5
            };
            trackConfidence.ValueChanged += TrackConfidence_ValueChanged;

            lblConfidence = new Label
            {
                Location = new Point(330, 155),
                Size = new Size(100, 23),
                Text = $"{_configuration.ConfidenceThreshold:P0}"
            };

            var lblLanguageLabel = new Label
            {
                Text = "Detection Language:",
                Location = new Point(20, 220),
                Size = new Size(150, 23)
            };

            cmbLanguage = new ComboBox
            {
                Location = new Point(20, 250),
                Size = new Size(200, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbLanguage.Items.AddRange(new[] { "en", "es", "fr", "de", "it", "pt", "he", "ar" });
            cmbLanguage.SelectedItem = _configuration.Language;

            tab.Controls.AddRange(new Control[] 
            {
                chkEnabled, chkNotifications, chkAutoStart,
                lblConfidenceLabel, trackConfidence, lblConfidence,
                lblLanguageLabel, cmbLanguage
            });
        }

        private void CreateEntitiesTab(TabPage tab)
        {
            var lblInstructions = new Label
            {
                Text = "Configure which types of sensitive information to detect and how to anonymize them:",
                Location = new Point(20, 20),
                Size = new Size(540, 40),
                TextAlign = ContentAlignment.TopLeft
            };

            dgvEntities = new DataGridView
            {
                Location = new Point(20, 70),
                Size = new Size(540, 280),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Add columns
            dgvEntities.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Enabled",
                HeaderText = "Enabled",
                Width = 80
            });

            dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Type",
                HeaderText = "Entity Type",
                ReadOnly = true,
                Width = 150
            });

            var methodColumn = new DataGridViewComboBoxColumn
            {
                Name = "Method",
                HeaderText = "Anonymization Method",
                Width = 150
            };
            methodColumn.Items.AddRange(new[] { "redact", "replace", "mask", "hash" });
            dgvEntities.Columns.Add(methodColumn);

            dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CustomReplacement",
                HeaderText = "Custom Replacement",
                Width = 160
            });

            LoadEntitiesData();

            tab.Controls.AddRange(new Control[] { lblInstructions, dgvEntities });
        }

        private void CreatePythonTab(TabPage tab)
        {
            var lblPythonLabel = new Label
            {
                Text = "Python Executable Path:",
                Location = new Point(20, 20),
                Size = new Size(150, 23)
            };

            txtPythonPath = new TextBox
            {
                Location = new Point(20, 50),
                Size = new Size(400, 23),
                Text = _configuration.PythonPath
            };

            btnBrowsePython = new Button
            {
                Text = "Browse...",
                Location = new Point(430, 49),
                Size = new Size(75, 25)
            };
            btnBrowsePython.Click += BtnBrowsePython_Click;

            btnTestPython = new Button
            {
                Text = "Test Python Installation",
                Location = new Point(20, 90),
                Size = new Size(150, 25)
            };
            btnTestPython.Click += BtnTestPython_Click;

            var lblInstructions = new Label
            {
                Text = "SecurePaste requires Python with the Presidio library installed.\n\n" +
                       "To install Presidio, run:\n" +
                       "pip install presidio-analyzer presidio-anonymizer\n\n" +
                       "For better performance, also install:\n" +
                       "pip install spacy\n" +
                       "python -m spacy download en_core_web_sm",
                Location = new Point(20, 130),
                Size = new Size(540, 120),
                TextAlign = ContentAlignment.TopLeft
            };

            tab.Controls.AddRange(new Control[] 
            {
                lblPythonLabel, txtPythonPath, btnBrowsePython, btnTestPython, lblInstructions
            });
        }

        private void LoadConfiguration()
        {
            if (chkEnabled != null) chkEnabled.Checked = _configuration.Enabled;
            if (chkNotifications != null) chkNotifications.Checked = _configuration.NotificationsEnabled;
            if (chkAutoStart != null) chkAutoStart.Checked = _configuration.AutoStart;
            if (trackConfidence != null) trackConfidence.Value = (int)(_configuration.ConfidenceThreshold * 100);
            if (lblConfidence != null) lblConfidence.Text = $"{_configuration.ConfidenceThreshold:P0}";
            if (cmbLanguage != null) cmbLanguage.SelectedItem = _configuration.Language;
            if (txtPythonPath != null) txtPythonPath.Text = _configuration.PythonPath;
        }

        private void LoadEntitiesData()
        {
            if (dgvEntities == null) return;
            
            dgvEntities.Rows.Clear();
            
            foreach (var entity in _configuration.Entities)
            {
                dgvEntities.Rows.Add(
                    entity.Enabled,
                    entity.Type,
                    entity.AnonymizationMethod,
                    entity.CustomReplacement ?? ""
                );
            }
        }

        private void TrackConfidence_ValueChanged(object? sender, EventArgs e)
        {
            if (trackConfidence != null && lblConfidence != null)
            {
                var value = trackConfidence.Value / 100.0;
                lblConfidence.Text = $"{value:P0}";
            }
        }

        private void BtnBrowsePython_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Python Executable|python.exe;python3.exe|All Files|*.*",
                Title = "Select Python Executable"
            };

            if (dialog.ShowDialog() == DialogResult.OK && txtPythonPath != null)
            {
                txtPythonPath.Text = dialog.FileName;
            }
        }

        private async void BtnTestPython_Click(object? sender, EventArgs e)
        {
            if (btnTestPython == null || txtPythonPath == null) return;
            
            btnTestPython.Enabled = false;
            btnTestPython.Text = "Testing...";

            try
            {
                var isWorking = await _presidioService.CheckPresidioInstallationAsync();
                
                if (isWorking)
                {
                    // Update installation status if test is successful
                    await _presidioService.VerifyAndUpdateInstallationStatusAsync();
                    
                    MessageBox.Show("Python and Presidio are working correctly!\n\nInstallation status has been updated.", 
                        "Test Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Python or Presidio installation not found or not working.\n\n" +
                        "Please ensure Python is installed and Presidio packages are available.", 
                        "Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error testing Python installation:\n{ex.Message}", 
                    "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTestPython.Enabled = true;
                btnTestPython.Text = "Test Python Installation";
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                // Update configuration from UI
                if (chkEnabled != null) _configuration.Enabled = chkEnabled.Checked;
                if (chkNotifications != null) _configuration.NotificationsEnabled = chkNotifications.Checked;
                if (chkAutoStart != null) _configuration.AutoStart = chkAutoStart.Checked;
                if (trackConfidence != null) _configuration.ConfidenceThreshold = trackConfidence.Value / 100.0;
                if (cmbLanguage != null) _configuration.Language = cmbLanguage.SelectedItem?.ToString() ?? "en";
                if (txtPythonPath != null) _configuration.PythonPath = txtPythonPath.Text;

                // Update entities
                if (dgvEntities != null)
                {
                    for (int i = 0; i < dgvEntities.Rows.Count && i < _configuration.Entities.Count; i++)
                    {
                        var row = dgvEntities.Rows[i];
                        var entity = _configuration.Entities[i];
                        
                        entity.Enabled = (bool)(row.Cells["Enabled"].Value ?? false);
                        entity.AnonymizationMethod = row.Cells["Method"].Value?.ToString() ?? "redact";
                        entity.CustomReplacement = string.IsNullOrWhiteSpace(row.Cells["CustomReplacement"].Value?.ToString()) 
                            ? null 
                            : row.Cells["CustomReplacement"].Value?.ToString();
                    }
                }

                _configService.SaveConfiguration(_configuration);
                
                MessageBox.Show("Configuration saved successfully!", "Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration:\n{ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to reset all settings to defaults?", 
                "Reset Configuration", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // Use the ResetToDefaults method to ensure proper reset without duplication
                _configuration.ResetToDefaults();
                LoadConfiguration();
                LoadEntitiesData();
                
                MessageBox.Show("Configuration has been reset to defaults.", "Reset Complete", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}