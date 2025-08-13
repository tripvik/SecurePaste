using SecurePaste.Services;

namespace SecurePaste.Forms
{
    public partial class TestForm : Form
    {
        private readonly PresidioService _presidioService;

        // Controls
        private TextBox? txtInput;
        private TextBox? txtOutput;
        private Button? btnTest;
        private Button? btnLoadSample;
        private Button? btnCopyToClipboard;
        private Button? btnClose;
        private Label? lblInstructions;
        private ProgressBar? progressBar;

        public TestForm(PresidioService presidioService)
        {
            _presidioService = presidioService;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Test Clipboard Anonymization";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(600, 400);

            // Simple implementation for now
            var lblSimple = new Label
            {
                Text = "Test form - Implementation in progress",
                Location = new Point(50, 50),
                Size = new Size(300, 30)
            };

            var btnCloseSimple = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Location = new Point(300, 100),
                Size = new Size(80, 25)
            };

            this.Controls.AddRange(new Control[] { lblSimple, btnCloseSimple });
        }
    }
}