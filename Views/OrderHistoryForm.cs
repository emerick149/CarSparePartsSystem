using CarSparePartsSystem.Controllers;
using CarSparePartsSystem.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CarSparePartsSystem.Views
{
    /// <summary>
    /// FR-09 (customer): View own order history with line-item detail (SFR-16).
    /// FR-10 / SFR-15: Cancel an order if status is New or Confirmed.
    /// </summary>
    public class OrderHistoryForm : Form
    {
        private User currentUser;
        private DataGridView dgvOrders;
        private Button btnCancel, btnBack;
        private Label lblStatus;

        public OrderHistoryForm(User user)
        {
            currentUser = user;
            InitializeComponents();
            LoadOrders();
        }

        private void InitializeComponents()
        {
            this.Text = "My Order History";
            this.Size = new Size(860, 520);
            this.StartPosition = FormStartPosition.CenterScreen;

            var lblTitle = new Label()
            {
                Text = "My Orders",
                Location = new Point(20, 15), Size = new Size(400, 26),
                Font = new Font("Arial", 13, FontStyle.Bold)
            };

            var lblNote = new Label()
            {
                Text = "Items column shows: Part x Qty @ unit price = line total",
                Location = new Point(20, 48), Size = new Size(700, 20),
                ForeColor = Color.DimGray, Font = new Font("Arial", 8)
            };

            dgvOrders = new DataGridView()
            {
                Location = new Point(20, 76),
                Size = new Size(800, 340),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false
            };

            lblStatus = new Label()
            {
                Text = "", Location = new Point(20, 426), Size = new Size(800, 22),
                Font = new Font("Arial", 9), ForeColor = Color.DimGray
            };

            btnCancel = new Button()
            {
                Text = "✖  Cancel Selected Order",
                Location = new Point(20, 452), Size = new Size(200, 34),
                BackColor = Color.Crimson, ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += BtnCancel_Click;

            btnBack = new Button()
            {
                Text = "← Back to Dashboard", Location = new Point(240, 452), Size = new Size(170, 34)
            };
            btnBack.Click += (s, e) => { new CustomerDashboardForm(currentUser).Show(); this.Close(); };

            this.Controls.AddRange(new Control[] { lblTitle, lblNote, dgvOrders, lblStatus, btnCancel, btnBack });
        }

        private void LoadOrders()
        {
            var ctrl = new CustomerController();
            dgvOrders.DataSource = ctrl.GetCustomerOrders(currentUser.Id);
            lblStatus.Text = "";
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null)
            {
                lblStatus.Text = "Select an order to cancel.";
                lblStatus.ForeColor = Color.Crimson;
                return;
            }

            int orderId     = Convert.ToInt32(dgvOrders.CurrentRow.Cells["Order ID"].Value);
            string status   = dgvOrders.CurrentRow.Cells["Status"].Value?.ToString() ?? "";

            if (status == "Completed" || status == "Cancelled" || status == "Ready")
            {
                lblStatus.Text = $"Order #{orderId} ({status}) cannot be cancelled.";
                lblStatus.ForeColor = Color.Crimson;
                return;
            }

            if (MessageBox.Show($"Cancel order #{orderId}?", "Confirm Cancellation",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var ctrl = new CustomerController();
            string result = ctrl.CancelOrder(orderId, currentUser.Id);

            if (result == "Success")
            {
                lblStatus.Text = $"Order #{orderId} cancelled successfully.";
                lblStatus.ForeColor = Color.DarkGreen;
                LoadOrders();
            }
            else
            {
                lblStatus.Text = result;
                lblStatus.ForeColor = Color.Crimson;
            }
        }
    }
}
