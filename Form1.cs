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
            _contextMenu.Items.Add("Test Stdin Mode", null, TestStdinMode_Click);
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

        private async void TestStdinMode_Click(object? sender, EventArgs e)
        {
            try
            {
                ShowBalloonTip("Testing...", "Testing stdin/stdout mode", ToolTipIcon.Info, 2000);
                
                var isWorking = await _presidioService.TestStdinModeAsync();
                
                if (isWorking)
                {
                    ShowBalloonTip("Stdin Mode Working", 
                        "stdin/stdout communication is functioning correctly", 
                        ToolTipIcon.Info, 3000);
                }
                else
                {
                    ShowBalloonTip("Stdin Mode Failed", 
                        "stdin/stdout mode is not working, using file mode fallback", 
                        ToolTipIcon.Warning, 5000);
                }
            }
            catch (Exception ex)
            {
                ShowBalloonTip("Test Error", 
                    $"Error testing stdin mode: {ex.Message}", 
                    ToolTipIcon.Error, 5000);
            }
        }