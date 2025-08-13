using SecurePaste.Core;
using SecurePaste.Services;
using SecurePaste.Forms;
using System.ComponentModel;

namespace SecurePaste
{
    public partial class MainForm : Form
    {
        private readonly int HOTKEY_ID = 1;
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private ConfigurationService _configService;
        private PresidioService _presidioService;
        private bool _isProcessing = false;

        public MainForm()
        {
            InitializeComponent();
            
            // Initialize services
            _configService = new ConfigurationService();
            _presidioService = new PresidioService(_configService.GetConfiguration());
            
            // Setup the form
            SetupForm();
            SetupSystemTray();
            RegisterGlobalHotkey();
            
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
                Icon = SystemIcons.Shield, // We'll replace this with a custom icon later
                ContextMenuStrip = _contextMenu,
                Visible = true,
                Text = "SecurePaste - Clipboard Anonymizer"
            };

            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            
            UpdateNotifyIconText();
        }

        private void RegisterGlobalHotkey()
        {
            if (!WindowsApi.RegisterHotKey(this.Handle, HOTKEY_ID, WindowsApi.MOD_CTRL, WindowsApi.VK_V))
            {
                MessageBox.Show("Failed to register global hotkey Ctrl+V. Another application might be using it.", 
                    "SecurePaste", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WindowsApi.WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                // Handle Ctrl+V hotkey
                _ = Task.Run(HandleClipboardPaste);
            }
            
            base.WndProc(ref m);
        }

        private async Task HandleClipboardPaste()
        {
            if (_isProcessing)
                return;

            _isProcessing = true;
            
            try
            {
                var config = _configService.GetConfiguration();
                if (!config.Enabled)
                {
                    // If disabled, just perform normal paste
                    ClipboardService.SimulatePaste();
                    return;
                }

                // Get clipboard text
                var originalText = ClipboardService.GetText();
                if (string.IsNullOrWhiteSpace(originalText))
                {
                    // No text in clipboard, perform normal paste
                    ClipboardService.SimulatePaste();
                    return;
                }

                // Show processing notification
                if (config.NotificationsEnabled)
                {
                    ShowBalloonTip("Processing...", "Anonymizing clipboard content", ToolTipIcon.Info, 2000);
                }

                // Anonymize the text
                var anonymizedText = await _presidioService.AnonymizeTextAsync(originalText);
                
                // Update statistics
                bool success = !string.Equals(originalText, anonymizedText, StringComparison.Ordinal);
                _configService.UpdateStatistics(true);

                // Set anonymized text to clipboard
                if (ClipboardService.SetText(anonymizedText))
                {
                    // Perform the paste
                    await Task.Delay(100); // Small delay to ensure clipboard is updated
                    ClipboardService.SimulatePaste();
                    
                    // Show success notification
                    if (config.NotificationsEnabled && success)
                    {
                        ShowBalloonTip("Content Anonymized", 
                            "Sensitive information has been protected", 
                            ToolTipIcon.Info, 3000);
                    }
                }
                else
                {
                    // Failed to set clipboard, perform normal paste with original content
                    ClipboardService.SetText(originalText);
                    ClipboardService.SimulatePaste();
                    _configService.UpdateStatistics(false);
                    
                    if (config.NotificationsEnabled)
                    {
                        ShowBalloonTip("Anonymization Failed", 
                            "Using original content", 
                            ToolTipIcon.Warning, 3000);
                    }
                }
            }
            catch (Exception ex)
            {
                // On any error, try to restore original clipboard and paste
                _configService.UpdateStatistics(false);
                ClipboardService.SimulatePaste();
                
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
                _isProcessing = false;
            }
        }

        private async Task CheckPresidioInstallation()
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

        private void ShowBalloonTip(string title, string text, ToolTipIcon icon, int timeout)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(timeout, title, text, icon);
            }
        }

        private void UpdateNotifyIconText()
        {
            if (_notifyIcon != null)
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
            
            // Refresh Presidio service with new configuration
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
            
            // Refresh status
            _ = Task.Run(CheckPresidioInstallation);
        }

        private void About_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "SecurePaste v1.0\n\n" +
                "A clipboard anonymization tool that uses Microsoft Presidio to protect sensitive information.\n\n" +
                "Features:\n" +
                "• Global Ctrl+V interception\n" +
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
            WindowsApi.UnregisterHotKey(this.Handle, HOTKEY_ID);
            _notifyIcon?.Dispose();
            
            base.OnFormClosing(e);
        }
    }
}
