using CarSparePartsSystem.Controllers;
using CarSparePartsSystem.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CarSparePartsSystem.Views
{
    /// <summary>
    /// FR-09 (admin) / FR-10 / SFR-20: Admin grid of ALL orders with status-transition buttons.
    /// Transitions: New→Confirmed|Cancelled, Confirmed→Ready|Cancelled, Ready→Completed|Cancelled.
    /// Completed and Cancelled are terminal.
    /// Stock is restored exactly once when any order is cancelled here.
    /// </summary>
    public class OrderManagementForm : Form
    {
        private User currentUser;
        private DataGridView dgvOrders;
        private TextBox txtSearch;
        private Button btnRefresh, btnConfirm, btnReady, btnComplete, btnCancel, btnBack;
        private Label lblStatus, lblTitle;

        public OrderManagementForm(User user)
        {
            currentUser = user;
            InitializeComponents();
            LoadOrders();
        }

        private void InitializeComponents()
        {
            this.Text = "Order Management";
            this.Size = new Size(1000, 580);
            this.StartPosition = FormStartPosition.CenterScreen;

            lblTitle = new Label()
            {
                Text = "All Customer Orders",
                Location = new Point(20, 15), Size = new Size(400, 26),
                Font = new Font("Arial", 13, FontStyle.Bold)
            };

            // ── Search bar ────────────────────────────────────────────
            this.Controls.Add(new Label() { Text = "Search:", Location = new Point(20, 52), Size = new Size(55, 22) });
            txtSearch = new TextBox() { Location = new Point(80, 50), Size = new Size(250, 25) };

            btnRefresh = new Button()
            {
                Text = "Refresh", Location = new Point(345, 48), Size = new Size(90, 28)
            };
            btnRefresh.Click += (s, e) => LoadOrders(txtSearch.Text);
            txtSearch.TextChanged += (s, e) => LoadOrders(txtSearch.Text);

            // ── Grid ─────────────────────────────────────────────────
            dgvOrders = new DataGridView()
            {
                Location = new Point(20, 88),
                Size = new Size(940, 340),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false
            };

            // ── Status legend ─────────────────────────────────────────
            var lblLegend = new Label()
            {
                Text = "Transitions:  New → Confirmed | Cancelled    Confirmed → Ready | Cancelled    Ready → Completed | Cancelled",
                Location = new Point(20, 437), Size = new Size(940, 20),
                Font = new Font("Arial", 8), ForeColor = Color.DimGray
            };

            // ── Action buttons ────────────────────────────────────────
            int bx = 20, by = 462;
            btnConfirm  = MakeStatusButton("✔ Confirm",   Color.DodgerBlue,     bx);  bx += 148;
            btnReady    = MakeStatusButton("📦 Mark Ready", Color.DarkOrange,    bx);  bx += 148;
            btnComplete = MakeStatusButton("✅ Complete",  Color.MediumSeaGreen, bx);  bx += 148;
            btnCancel   = MakeStatusButton("✖ Cancel",    Color.Crimson,         bx);  bx += 148;

            btnConfirm.Click  += (s, e) => ChangeStatus("Confirmed");
            btnReady.Click    += (s, e) => ChangeStatus("Ready");
            btnComplete.Click += (s, e) => ChangeStatus("Completed");
            btnCancel.Click   += (s, e) => ChangeStatus("Cancelled");

            lblStatus = new Label()
            {
                Text = "", Location = new Point(20, 505), Size = new Size(940, 24),
                ForeColor = Color.DimGray, Font = new Font("Arial", 9)
            };

            btnBack = new Button()
            {
                Text = "← Back to Dashboard", Location = new Point(820, 462), Size = new Size(140, 36)
            };
            btnBack.Click += (s, e) => { new AdminDashboardForm(currentUser).Show(); this.Close(); };

            this.Controls.AddRange(new Control[]
            {
                lblTitle, txtSearch, btnRefresh, dgvOrders, lblLegend,
                btnConfirm, btnReady, btnComplete, btnCancel, lblStatus, btnBack
            });
        }

        private Button MakeStatusButton(string text, Color color, int x)
        {
            return new Button()
            {
                Text = text, Location = new Point(x, 462), Size = new Size(140, 36),
                BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
        }

        private void LoadOrders(string search = "")
        {
            var ctrl = new OrderController();
            dgvOrders.DataSource = ctrl.GetAllOrders(search);
            lblStatus.Text = "";
        }

        private void ChangeStatus(string newStatus)
        {
            if (dgvOrders.CurrentRow == null)
            {
                lblStatus.Text = "Select an order first.";
                lblStatus.ForeColor = Color.Crimson;
                return;
            }

            int orderId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["Order ID"].Value);
            string currentStatus = dgvOrders.CurrentRow.Cells["Status"].Value?.ToString() ?? "";

            // Confirm cancellation explicitly
            if (newStatus == "Cancelled")
            {
                var confirm = MessageBox.Show(
                    $"Cancel order #{orderId}? This will restore stock and cannot be undone.",
                    "Confirm Cancellation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;
            }

            var ctrl = new OrderController();
            string result = ctrl.UpdateOrderStatus(orderId, newStatus);

            if (result == "Success")
            {
                lblStatus.Text = $"Order #{orderId}: {currentStatus} → {newStatus}";
                lblStatus.ForeColor = Color.DarkGreen;
                LoadOrders(txtSearch.Text);
            }
            else
            {
                lblStatus.Text = result;
                lblStatus.ForeColor = Color.Crimson;
            }
        }
    }
}
