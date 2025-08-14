// MainForm.cs
using SecurePaste.Core;
using SecurePaste.Forms;
using SecurePaste.Services;
using System.Drawing.Drawing2D; // Make sure this is included

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

        // Loading overlay component
        private ModernLoadingOverlay? _loadingOverlay;

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
            // All styling and logic is encapsulated in our new class!
            _loadingOverlay = new ModernLoadingOverlay();
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
                _ = Task.Run(HandleClipboardChange);
            }
            base.WndProc(ref m);
        }

        private async Task HandleClipboardChange()
        {
            lock (_processLock)
            {
                if (_isProcessing) return;
                _isProcessing = true;
            }

            try
            {
                var config = _configService.GetConfiguration();
                if (!config.Enabled) return;

                var currentText = ClipboardService.GetText();
                if (string.IsNullOrWhiteSpace(currentText)) return;

                if (string.Equals(currentText, _lastProcessedText, StringComparison.Ordinal)) return;

                ShowLoadingOverlay("Anonymizing clipboard content...");
                var originalText = currentText;
                var anonymizedText = await _presidioService.AnonymizeTextAsync(originalText);

                bool wasAnonymized = !string.Equals(originalText, anonymizedText, StringComparison.Ordinal);
                _configService.UpdateStatistics(wasAnonymized);

                if (wasAnonymized)
                {
                    _lastProcessedText = anonymizedText;
                    if (ClipboardService.SetText(anonymizedText))
                    {
                        UpdateLoadingOverlay("Content anonymized successfully!");
                        if (config.NotificationsEnabled)
                        {
                            ShowBalloonTip("Content Anonymized",
                                "Sensitive information has been protected.",
                                ToolTipIcon.Info, 4000);
                        }
                        await Task.Delay(1000); // Wait briefly before hiding
                    }
                    else
                    {
                        UpdateLoadingOverlay("Anonymization failed!");
                        _configService.UpdateStatistics(false);
                        if (config.NotificationsEnabled)
                        {
                            ShowBalloonTip("Anonymization Failed", "Could not update clipboard", ToolTipIcon.Warning, 3000);
                        }
                        await Task.Delay(1500);
                    }
                }
                else
                {
                    _lastProcessedText = originalText;
                    UpdateLoadingOverlay("No sensitive data detected");
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                _configService.UpdateStatistics(false);
                UpdateLoadingOverlay($"Error: {ex.Message}");
                var config = _configService.GetConfiguration();
                if (config.NotificationsEnabled)
                {
                    ShowBalloonTip("Error", $"Anonymization failed: {ex.Message}", ToolTipIcon.Error, 5000);
                }
                await Task.Delay(2000);
            }
            finally
            {
                HideLoadingOverlay();
                lock (_processLock)
                {
                    _isProcessing = false;
                }
                _ = Task.Delay(10000).ContinueWith(_ => _lastProcessedText = null);
            }
        }

        private void ShowLoadingOverlay(string message)
        {
            if (_loadingOverlay != null)
            {
                this.Invoke(() => _loadingOverlay.ShowOverlay(message));
            }
        }

        private void UpdateLoadingOverlay(string message)
        {
            if (_loadingOverlay != null && _loadingOverlay.Visible)
            {
                this.Invoke(() => _loadingOverlay.UpdateMessage(message));
            }
        }

        private void HideLoadingOverlay()
        {
            if (_loadingOverlay != null)
            {
                this.Invoke(() => _loadingOverlay.HideOverlay());
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

        private void UpdateToggleMenuItemText()
        {
            if (_toggleMenuItem is null) return;
            var config = _configService.GetConfiguration();
            _toggleMenuItem.Text = config.Enabled ? "Disable" : "Enable";
        }

        // --- Event handlers remain the same as your original code ---

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
            using var configForm = new ConfigurationForm(_configService);
            configForm.ShowDialog();

            _presidioService?.Dispose();
            _presidioService = new PresidioService(_configService.GetConfiguration());
            UpdateNotifyIconText();
        }

        private void Statistics_Click(object? sender, EventArgs e)
        {
            using var statsForm = new StatisticsForm(_configService);
            statsForm.ShowDialog();
        }

        private async void ShowLoadingDemo_Click(object? sender, EventArgs e)
        {
            ShowLoadingOverlay("Demo: Anonymizing content...");
            await Task.Delay(2000);
            UpdateLoadingOverlay("Demo: Content anonymized!");
            await Task.Delay(2000);
            HideLoadingOverlay();
        }

        private void About_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "SecurePaste v1.0\n\n" +
                "A clipboard anonymization tool that uses Microsoft Presidio to protect sensitive information.",
                "About SecurePaste", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            try
            {
                WindowsApi.RemoveClipboardFormatListener(this.Handle);
                _notifyIcon?.Dispose();
                _loadingOverlay?.Dispose();
                _presidioService?.Dispose();
            }
            catch { /* Ignore cleanup errors */ }

            base.OnFormClosing(e);
        }
    }
}