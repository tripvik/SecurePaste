using SecurePaste.Core;
using SecurePaste.Forms;
using SecurePaste.Services;

namespace SecurePaste
{
    public partial class MainForm : Form
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private ToolStripMenuItem? _toggleMenuItem;
        private ConfigurationService _configService;
        private PresidioService _presidioService;
        private bool _isProcessing = false;
        private readonly object _processLock = new object();
        private string? _lastProcessedText = null;

        // Loading overlay components
        private Form? _loadingOverlay;
        private Label? _loadingLabel;
        private ProgressBar? _loadingProgress;
        private System.Windows.Forms.Timer? _loadingTimer;

        public MainForm()
        {
            InitializeComponent();

            // Initialize services
            _configService = new ConfigurationService();
            _presidioService = new PresidioService(_configService.GetConfiguration());

            // Setup the form
            SetupForm();
            SetupSystemTray();
            SetupLoadingOverlay();
            RegisterForClipboardUpdates();

            // Check Presidio installation on startup
            _ = Task.Run(CheckPresidioInstallation);
        }

        private void SetupForm()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            this.Text = "SecurePaste";
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void SetupSystemTray()
        {
            _contextMenu = new ContextMenuStrip();

            _toggleMenuItem = new ToolStripMenuItem("Enable/Disable", null, ToggleEnabled_Click);
            _contextMenu.Items.Add(_toggleMenuItem);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Configuration", null, Configuration_Click);
            _contextMenu.Items.Add("Statistics", null, Statistics_Click);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Test Clipboard", null, TestClipboard_Click);
            _contextMenu.Items.Add("Install Presidio", null, InstallPresidio_Click);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Show Loading Demo", null, ShowLoadingDemo_Click);
            _contextMenu.Items.Add("About", null, About_Click);
            _contextMenu.Items.Add("Exit", null, Exit_Click);

            _notifyIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Shield,
                ContextMenuStrip = _contextMenu,
                Visible = true,
                Text = "SecurePaste - Clipboard Anonymizer"
            };

            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            UpdateNotifyIconText();
            UpdateToggleMenuItemText();
        }

        private void SetupLoadingOverlay()
        {
            // Create a semi-transparent overlay window
            _loadingOverlay = new Form()
            {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.Black,
                Opacity = 0.7,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                Size = new Size(300, 150),
                Visible = false
            };

            // Create loading label
            _loadingLabel = new Label()
            {
                Text = "Anonymizing clipboard content...",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Create progress bar
            _loadingProgress = new ProgressBar()
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 50,
                Dock = DockStyle.Bottom,
                Height = 25,
                BackColor = Color.DarkBlue,
                ForeColor = Color.LightBlue
            };

            // Create loading timer for auto-hide
            _loadingTimer = new System.Windows.Forms.Timer()
            {
                Interval = 5000 // Auto-hide after 5 seconds
            };
            _loadingTimer.Tick += LoadingTimer_Tick;

            _loadingOverlay.Controls.Add(_loadingLabel);
            _loadingOverlay.Controls.Add(_loadingProgress);
        }

        private void RegisterForClipboardUpdates()
        {
            if (!WindowsApi.AddClipboardFormatListener(this.Handle))
            {
                MessageBox.Show("Failed to register for clipboard updates. Clipboard monitoring may not work properly.",
                    "SecurePaste", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CLIPBOARDUPDATE)
            {
                // Handle clipboard change
                _ = Task.Run(HandleClipboardChange);
            }

            base.WndProc(ref m);
        }

        private async Task HandleClipboardChange()
        {
            lock (_processLock)
            {
                if (_isProcessing)
                    return;
                _isProcessing = true;
            }

            try
            {
                var config = _configService.GetConfiguration();
                if (!config.Enabled)
                    return;

                var currentText = ClipboardService.GetText();
                if (string.IsNullOrWhiteSpace(currentText))
                    return;

                // Check if this is the same text we just processed
                if (string.Equals(currentText, _lastProcessedText, StringComparison.Ordinal))
                    return;

                // Show loading overlay immediately
                ShowLoadingOverlay("Anonymizing clipboard content...");

                var originalText = currentText;

                // Anonymize the text
                var anonymizedText = await _presidioService.AnonymizeTextAsync(originalText);

                bool wasAnonymized = !string.Equals(originalText, anonymizedText, StringComparison.Ordinal);
                _configService.UpdateStatistics(wasAnonymized);

                if (wasAnonymized)
                {
                    _lastProcessedText = anonymizedText;

                    if (ClipboardService.SetText(anonymizedText))
                    {
                        // Update loading overlay
                        UpdateLoadingOverlay("Content anonymized successfully!");

                        // Show success notification
                        if (config.NotificationsEnabled)
                        {
                            ShowBalloonTip("Content Anonymized",
                                "Sensitive information has been protected. Next paste will use anonymized content.",
                                ToolTipIcon.Info, 4000);
                        }

                        // Auto-hide overlay after 2 seconds
                        await Task.Delay(2000);
                        HideLoadingOverlay();
                    }
                    else
                    {
                        UpdateLoadingOverlay("Anonymization failed!");
                        _configService.UpdateStatistics(false);

                        if (config.NotificationsEnabled)
                        {
                            ShowBalloonTip("Anonymization Failed",
                                "Could not update clipboard",
                                ToolTipIcon.Warning, 3000);
                        }

                        await Task.Delay(2000);
                        HideLoadingOverlay();
                    }
                }
                else
                {
                    _lastProcessedText = originalText;
                    UpdateLoadingOverlay("No sensitive data detected");

                    await Task.Delay(1500);
                    HideLoadingOverlay();
                }
            }
            catch (Exception ex)
            {
                _configService.UpdateStatistics(false);

                UpdateLoadingOverlay($"Error: {ex.Message}");

                var config = _configService.GetConfiguration();
                if (config.NotificationsEnabled)
                {
                    ShowBalloonTip("Error",
                        $"Anonymization failed: {ex.Message}",
                        ToolTipIcon.Error, 5000);
                }

                await Task.Delay(3000);
                HideLoadingOverlay();
            }
            finally
            {
                lock (_processLock)
                {
                    _isProcessing = false;
                }

                // Clear the last processed text after a delay
                _ = Task.Delay(10000).ContinueWith(_ => _lastProcessedText = null);
            }
        }

        private void ShowLoadingOverlay(string message)
        {
            if (_loadingOverlay != null && _loadingLabel != null && _loadingTimer != null)
            {
                this.Invoke(() =>
                {
                    _loadingLabel.Text = message;
                    _loadingOverlay.Size = new Size(Math.Max(300, message.Length * 8), 150);

                    var screen = Screen.FromPoint(Cursor.Position);
                    int margin = 20; // Margin from the screen edges

                    // Position the overlay at the bottom-right corner
                    _loadingOverlay.Location = new Point(
                        screen.WorkingArea.Right - _loadingOverlay.Width - margin,
                        screen.WorkingArea.Bottom - _loadingOverlay.Height - margin
                    );

                    _loadingOverlay.Show();
                    _loadingOverlay.BringToFront();

                    // Start auto-hide timer
                    _loadingTimer.Stop();
                    _loadingTimer.Start();
                });
            }
        }

        private void UpdateLoadingOverlay(string message)
        {
            if (_loadingOverlay != null && _loadingLabel != null && _loadingOverlay.Visible)
            {
                this.Invoke(() =>
                {
                    _loadingLabel.Text = message;
                    _loadingOverlay.Size = new Size(Math.Max(300, message.Length * 8), 150);
                });
            }
        }

        private void HideLoadingOverlay()
        {
            if (_loadingOverlay != null && _loadingTimer != null)
            {
                this.Invoke(() =>
                {
                    _loadingOverlay.Hide();
                    _loadingTimer.Stop();
                });
            }
        }

        private void LoadingTimer_Tick(object? sender, EventArgs e)
        {
            HideLoadingOverlay();
        }

        private async Task CheckPresidioInstallation()
        {
            try
            {
                var config = _configService.GetConfiguration();
                var isInstalled = await _presidioService.CheckPresidioInstallationAsync();

                if (config.PresidioInstalled != isInstalled)
                {
                    config.PresidioInstalled = isInstalled;
                    _configService.SaveConfiguration(config);
                }

                if (!isInstalled)
                {
                    this.Invoke(() =>
                    {
                        ShowBalloonTip("Presidio Not Found",
                            "Click 'Install Presidio' in the context menu to set up anonymization",
                            ToolTipIcon.Warning, 5000);
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to check Presidio installation: {ex.Message}");
            }
        }

        private void ShowBalloonTip(string title, string text, ToolTipIcon icon, int timeout)
        {
            if (_notifyIcon != null && !this.IsDisposed)
            {
                _notifyIcon.ShowBalloonTip(timeout, title, text, icon);
            }
        }

        private void UpdateNotifyIconText()
        {
            if (_notifyIcon != null && !this.IsDisposed)
            {
                var config = _configService.GetConfiguration();
                var status = config.Enabled ? "Enabled" : "Disabled";
                var presidioStatus = config.PresidioInstalled ? "Ready" : "Not Installed";
                _notifyIcon.Text = $"SecurePaste - {status} (Presidio: {presidioStatus})";
            }
        }

        private void UpdateToggleMenuItemText()
        {
            if (_toggleMenuItem is null) return;

            var config = _configService.GetConfiguration();
            _toggleMenuItem.Text = config.Enabled ? "Disable" : "Enable";
        }

        // Event handlers
        private void ToggleEnabled_Click(object? sender, EventArgs e)
        {
            var config = _configService.GetConfiguration();
            config.Enabled = !config.Enabled;
            _configService.SaveConfiguration(config);
            UpdateNotifyIconText();
            UpdateToggleMenuItemText();

            ShowBalloonTip("SecurePaste",
                config.Enabled ? "Anonymization enabled" : "Anonymization disabled",
                ToolTipIcon.Info, 2000);
        }

        private void Configuration_Click(object? sender, EventArgs e)
        {
            var configForm = new ConfigurationForm(_configService);
            configForm.ShowDialog();

            _presidioService = new PresidioService(_configService.GetConfiguration());
            UpdateNotifyIconText();
        }

        private void Statistics_Click(object? sender, EventArgs e)
        {
            var statsForm = new StatisticsForm(_configService);
            statsForm.ShowDialog();
        }

        private void TestClipboard_Click(object? sender, EventArgs e)
        {
            var testForm = new TestForm(_presidioService);
            testForm.ShowDialog();
        }

        private void InstallPresidio_Click(object? sender, EventArgs e)
        {
            var installForm = new InstallPresidioForm(_configService);
            installForm.ShowDialog();

            _ = Task.Run(CheckPresidioInstallation);
        }

        private void ShowLoadingDemo_Click(object? sender, EventArgs e)
        {
            // Demo the loading overlay
            ShowLoadingOverlay("Demo: Anonymizing clipboard content...");

            Task.Delay(2000).ContinueWith(_ =>
            {
                UpdateLoadingOverlay("Demo: Content anonymized successfully!");
                Task.Delay(2000).ContinueWith(_ => HideLoadingOverlay());
            });
        }

        private void About_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "SecurePaste v1.0\n\n" +
                "A clipboard anonymization tool that uses Microsoft Presidio to protect sensitive information.\n\n" +
                "Features:\n" +
                "• Automatic clipboard monitoring\n" +
                "• Visual processing feedback\n" +
                "• PII detection and anonymization\n" +
                "• Configurable entity types\n" +
                "• Real-time processing\n\n" +
                "How it works:\n" +
                "1. Copy text to clipboard\n" +
                "2. SecurePaste automatically processes it\n" +
                "3. Next paste uses anonymized version\n\n" +
                "Created with .NET 9 and Windows Forms",
                "About SecurePaste",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void Exit_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            Configuration_Click(sender, e);
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                return;
            }

            // Cleanup
            try
            {
                WindowsApi.RemoveClipboardFormatListener(this.Handle);
                _notifyIcon?.Dispose();
                _loadingOverlay?.Dispose();
                _loadingTimer?.Dispose();
            }
            catch
            {
                // Ignore cleanup errors
            }

            base.OnFormClosing(e);
        }
    }
}