using SecurePaste.Services;

namespace SecurePaste.Forms
{
    public partial class StatisticsForm : Form
    {
        private readonly IConfigurationService _configService;

        // Controls
        private Label? lblTotalOperations;
        private Label? lblSuccessfulOperations;
        private Label? lblFailedOperations;
        private Label? lblSuccessRate;
        private Label? lblLastOperation;
        private DataGridView? dgvEntitiesFound;
        private Button? btnReset;
        private Button? btnClose;
        private Button? btnRefresh;

        public StatisticsForm(IConfigurationService configService)
        {
            _configService = configService;
            InitializeComponent();
            LoadStatistics();
        }

        private void InitializeComponent()
        {
            this.Text = "SecurePaste Statistics";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Main panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(20)
            };

            // Statistics labels
            var lblTotalLabel = new Label { Text = "Total Operations:", AutoSize = true, Anchor = AnchorStyles.Left };
            lblTotalOperations = new Label { Text = "0", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font(Font, FontStyle.Bold) };

            var lblSuccessfulLabel = new Label { Text = "Successful Operations:", AutoSize = true, Anchor = AnchorStyles.Left };
            lblSuccessfulOperations = new Label { Text = "0", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font(Font, FontStyle.Bold) };

            var lblFailedLabel = new Label { Text = "Failed Operations:", AutoSize = true, Anchor = AnchorStyles.Left };
            lblFailedOperations = new Label { Text = "0", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font(Font, FontStyle.Bold) };

            var lblSuccessRateLabel = new Label { Text = "Success Rate:", AutoSize = true, Anchor = AnchorStyles.Left };
            lblSuccessRate = new Label { Text = "0%", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font(Font, FontStyle.Bold) };

            var lblLastOperationLabel = new Label { Text = "Last Operation:", AutoSize = true, Anchor = AnchorStyles.Left };
            lblLastOperation = new Label { Text = "Never", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font(Font, FontStyle.Bold) };

            // Entities DataGridView
            var lblEntitiesLabel = new Label { Text = "Entities Detected:", AutoSize = true, Anchor = AnchorStyles.Left };
            dgvEntitiesFound = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Size = new Size(440, 150),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
            };

            dgvEntitiesFound.Columns.Add("EntityType", "Entity Type");
            dgvEntitiesFound.Columns.Add("Count", "Count");
            dgvEntitiesFound.Columns.Add("Percentage", "Percentage");

            // Add controls to table layout
            mainPanel.Controls.Add(lblTotalLabel, 0, 0);
            mainPanel.Controls.Add(lblTotalOperations, 1, 0);
            mainPanel.Controls.Add(lblSuccessfulLabel, 0, 1);
            mainPanel.Controls.Add(lblSuccessfulOperations, 1, 1);
            mainPanel.Controls.Add(lblFailedLabel, 0, 2);
            mainPanel.Controls.Add(lblFailedOperations, 1, 2);
            mainPanel.Controls.Add(lblSuccessRateLabel, 0, 3);
            mainPanel.Controls.Add(lblSuccessRate, 1, 3);
            mainPanel.Controls.Add(lblLastOperationLabel, 0, 4);
            mainPanel.Controls.Add(lblLastOperation, 1, 4);

            mainPanel.SetColumnSpan(lblEntitiesLabel, 2);
            mainPanel.Controls.Add(lblEntitiesLabel, 0, 5);

            mainPanel.SetColumnSpan(dgvEntitiesFound, 2);
            mainPanel.Controls.Add(dgvEntitiesFound, 0, 6);

            this.Controls.Add(mainPanel);

            // Buttons panel
            var buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom
            };

            btnRefresh = new Button
            {
                Text = "Refresh",
                Location = new Point(10, 15),
                Size = new Size(75, 23)
            };
            btnRefresh.Click += BtnRefresh_Click;

            btnReset = new Button
            {
                Text = "Reset Statistics",
                Location = new Point(95, 15),
                Size = new Size(120, 23)
            };
            btnReset.Click += BtnReset_Click;

            btnClose = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Location = new Point(405, 15),
                Size = new Size(75, 23)
            };

            buttonPanel.Controls.AddRange(new Control[] { btnRefresh, btnReset, btnClose });
            this.Controls.Add(buttonPanel);
        }

        private void LoadStatistics()
        {
            var stats = _configService.GetStatistics();

            if (lblTotalOperations != null) lblTotalOperations.Text = stats.TotalOperations.ToString();
            if (lblSuccessfulOperations != null) lblSuccessfulOperations.Text = stats.SuccessfulOperations.ToString();
            if (lblFailedOperations != null) lblFailedOperations.Text = stats.FailedOperations.ToString();

            // Calculate success rate
            double successRate = stats.TotalOperations > 0 
                ? (double)stats.SuccessfulOperations / stats.TotalOperations * 100 
                : 0;
            if (lblSuccessRate != null) lblSuccessRate.Text = $"{successRate:F1}%";

            // Last operation
            if (lblLastOperation != null) lblLastOperation.Text = stats.LastOperation?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never";

            // Load entities data
            LoadEntitiesData(stats);
        }

        private void LoadEntitiesData(Models.Statistics stats)
        {
            if (dgvEntitiesFound == null) return;
            
            dgvEntitiesFound.Rows.Clear();

            if (stats.EntitiesFound.Count == 0)
            {
                dgvEntitiesFound.Rows.Add("No entities detected yet", "0", "0%");
                return;
            }

            var totalEntities = stats.EntitiesFound.Values.Sum();

            foreach (var entity in stats.EntitiesFound.OrderByDescending(e => e.Value))
            {
                var percentage = totalEntities > 0 ? (double)entity.Value / totalEntities * 100 : 0;
                dgvEntitiesFound.Rows.Add(
                    entity.Key,
                    entity.Value.ToString(),
                    $"{percentage:F1}%"
                );
            }
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            LoadStatistics();
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all statistics?\n\nThis action cannot be undone.",
                "Reset Statistics",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                _configService.ResetStatistics();
                LoadStatistics();
                
                MessageBox.Show("Statistics have been reset.", "Statistics Reset", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}