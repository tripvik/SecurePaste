namespace SecurePaste.Forms
{
    partial class StatisticsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "SecurePaste Statistics";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Title
            var lblTitle = new Label
            {
                Text = "Usage Statistics",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(200, 25)
            };

            // Statistics labels
            var yPos = 60;
            var spacing = 25;

            var lblTotalLabel = new Label
            {
                Text = "Total Operations:",
                Location = new Point(20, yPos),
                Size = new Size(150, 20)
            };

            lblTotalOperations = new Label
            {
                Location = new Point(180, yPos),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            yPos += spacing;
            var lblSuccessfulLabel = new Label
            {
                Text = "Successful:",
                Location = new Point(20, yPos),
                Size = new Size(150, 20)
            };

            lblSuccessfulOperations = new Label
            {
                Location = new Point(180, yPos),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Green
            };

            yPos += spacing;
            var lblFailedLabel = new Label
            {
                Text = "Failed:",
                Location = new Point(20, yPos),
                Size = new Size(150, 20)
            };

            lblFailedOperations = new Label
            {
                Location = new Point(180, yPos),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Red
            };

            yPos += spacing;
            var lblSuccessRateLabel = new Label
            {
                Text = "Success Rate:",
                Location = new Point(20, yPos),
                Size = new Size(150, 20)
            };

            lblSuccessRate = new Label
            {
                Location = new Point(180, yPos),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            yPos += spacing;
            var lblLastOperationLabel = new Label
            {
                Text = "Last Operation:",
                Location = new Point(20, yPos),
                Size = new Size(150, 20)
            };

            lblLastOperation = new Label
            {
                Location = new Point(180, yPos),
                Size = new Size(280, 20)
            };

            // Entities found section
            yPos += spacing + 10;
            var lblEntitiesTitle = new Label
            {
                Text = "Entities Found:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(20, yPos),
                Size = new Size(200, 25)
            };

            yPos += 30;
            dgvEntitiesFound = new DataGridView
            {
                Location = new Point(20, yPos),
                Size = new Size(440, 150),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Add columns to entities grid
            dgvEntitiesFound.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "EntityType",
                HeaderText = "Entity Type",
                Width = 200
            });

            dgvEntitiesFound.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Count",
                HeaderText = "Count",
                Width = 100
            });

            dgvEntitiesFound.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Percentage",
                HeaderText = "Percentage",
                Width = 140
            });

            // Buttons
            var buttonY = yPos + 160;
            btnRefresh = new Button
            {
                Text = "Refresh",
                Location = new Point(20, buttonY),
                Size = new Size(80, 25)
            };
            btnRefresh.Click += BtnRefresh_Click;

            btnReset = new Button
            {
                Text = "Reset Statistics",
                Location = new Point(110, buttonY),
                Size = new Size(120, 25)
            };
            btnReset.Click += BtnReset_Click;

            btnClose = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Location = new Point(380, buttonY),
                Size = new Size(80, 25)
            };

            // Add all controls
            this.Controls.AddRange(new Control[]
            {
                lblTitle,
                lblTotalLabel, lblTotalOperations,
                lblSuccessfulLabel, lblSuccessfulOperations,
                lblFailedLabel, lblFailedOperations,
                lblSuccessRateLabel, lblSuccessRate,
                lblLastOperationLabel, lblLastOperation,
                lblEntitiesTitle, dgvEntitiesFound,
                btnRefresh, btnReset, btnClose
            });
        }
    }
}