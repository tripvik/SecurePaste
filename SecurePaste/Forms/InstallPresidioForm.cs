using SecurePaste.Services;
using System.Diagnostics;

namespace SecurePaste.Forms
{
    public partial class InstallPresidioForm : Form
    {
        private readonly ConfigurationService _configService;

        public InstallPresidioForm(ConfigurationService configService)
        {
            _configService = configService;
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

            // Simple implementation for now
            var lblSimple = new Label
            {
                Text = "Presidio installation form - Implementation in progress",
                Location = new Point(50, 50),
                Size = new Size(400, 30)
            };

            var btnCloseSimple = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Location = new Point(500, 450),
                Size = new Size(80, 25)
            };

            this.Controls.AddRange(new Control[] { lblSimple, btnCloseSimple });
        }
    }
}