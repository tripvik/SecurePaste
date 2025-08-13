using SecurePaste.Services;

namespace SecurePaste.Forms
{
    public partial class StatisticsForm : Form
    {
        private readonly ConfigurationService _configService;

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

        public StatisticsForm(ConfigurationService configService)
        {
            _configService = configService;
            InitializeComponent();
            LoadStatistics();
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