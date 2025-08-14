using SecurePaste.Core;
using SecurePaste.Forms;
using SecurePaste.Services;
using System.Runtime.InteropServices;

namespace SecurePaste
{
    public partial class MainForm : Form
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private ConfigurationService _configService;
        private PresidioService _presidioService;
        private bool _isProcessing = false;
        private readonly object _processLock = new object();
        private string? _lastProcessedText = null;

        public MainForm()
        {
            InitializeComponent();

            // Initialize services
            _configService = new ConfigurationService();
            _presidioService = new PresidioService(_configService.GetConfiguration());

            // Setup the form
            SetupForm();
            SetupSystemTray();
            RegisterForClipboardUpdates();

            // Check Presidio installation on startup
            _ = Task.Run(CheckPresidioInstallation);
        }

        private void SetupForm()
        {
            // Hide the form from taskbar and make it invisible
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;

            // Set form properties
            this.Text = "SecurePaste";
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void SetupSystemTray()
        {
            // Create context menu
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("Enable/Disable", null, ToggleEnabled_Click);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Configuration", null, Configuration_Click);
            _contextMenu.Items.Add("Statistics", null, Statistics_Click);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Test Clipboard", null, TestClipboard_Click);
            _contextMenu.Items.Add("Install Presidio", null, InstallPresidio_Click);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("About", null, About_Click);
            _contextMenu.Items.Add("Exit", null, Exit_Click);

            // Create notify icon
            _notifyIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Shield,
                ContextMenuStrip = _contextMenu,
                Visible = true,
                Text = "SecurePaste - Clipboard Anonymizer"
            };

            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            UpdateNotifyIconText();
        }

        private void RegisterForClipboardUpdates()
        {
            // Register this window to receive clipboard update notifications
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
            // Use lock to prevent multiple simultaneous processing
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

                // Get current clipboard text
                var currentText = ClipboardService.GetText();
                if (string.IsNullOrWhiteSpace(currentText))
                    return;

                // Check if this is the same text we just processed (to avoid loops)
                if (string.Equals(currentText, _lastProcessedText, StringComparison.Ordinal))
                    return;

                // Show processing notification
                if (config.NotificationsEnabled)
                {
                    ShowBalloonTip("Processing...", "Anonymizing clipboard content", ToolTipIcon.Info, 2000);
                }

                // Store original text to detect loops
                var originalText = currentText;

                // Anonymize the text
                var anonymizedText = await _presidioService.AnonymizeTextAsync(originalText);

                // Check if anonymization actually changed anything
                bool wasAnonymized = !string.Equals(originalText, anonymizedText, StringComparison.Ordinal);

                // Update statistics
                _configService.UpdateStatistics(wasAnonymized);

                // If text was anonymized, replace it in clipboard
                if (wasAnonymized)
                {
                    _lastProcessedText = anonymizedText;

                    if (ClipboardService.SetText(anonymizedText))
                    {
                        // Show success notification
                        if (config.NotificationsEnabled)
                        {
                            ShowBalloonTip("Content Anonymized",
                                "Sensitive information has been protected",
                                ToolTipIcon.Info, 3000);
                        }
                    }
                    else
                    {
                        _configService.UpdateStatistics(false);

                        if (config.NotificationsEnabled)
                        {
                            ShowBalloonTip("Anonymization Failed",
                                "Could not update clipboard",
                                ToolTipIcon.Warning, 3000);
                        }
                    }
                }
                else
                {
                    // No anonymization needed, just track that we processed it
                    _lastProcessedText = originalText;
                }
            }
            catch (Exception ex)
            {
                _configService.UpdateStatistics(false);

                var config = _configService.GetConfiguration();
                if (config.NotificationsEnabled)
                {
                    ShowBalloonTip("Error",
                        $"Anonymization failed: {ex.Message}",
                        ToolTipIcon.Error, 5000);
                }
            }
            finally
            {
                lock (_processLock)
                {
                    _isProcessing = false;
                }

                // Clear the last processed text after a delay to allow for normal usage
                _ = Task.Delay(5000).ContinueWith(_ => _lastProcessedText = null);
            }
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

        // Event handlers
        private void ToggleEnabled_Click(object? sender, EventArgs e)
        {
            var config = _configService.GetConfiguration();
            config.Enabled = !config.Enabled;
            _configService.SaveConfiguration(config);
            UpdateNotifyIconText();

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

        private void About_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "SecurePaste v1.0\n\n" +
                "A clipboard anonymization tool that uses Microsoft Presidio to protect sensitive information.\n\n" +
                "Features:\n" +
                "• Automatic clipboard monitoring\n" +
                "• PII detection and anonymization\n" +
                "• Configurable entity types\n" +
                "• Real-time processing\n\n" +
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
            }
            catch
            {
                // Ignore cleanup errors
            }

            base.OnFormClosing(e);
        }
    }
}