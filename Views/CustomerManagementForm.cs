using CarSparePartsSystem.Controllers;
using CarSparePartsSystem.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CarSparePartsSystem.Views
{
    /// <summary>
    /// FR-11: Admin view of all customers with filterable list and order statistics.
    /// </summary>
    public class CustomerManagementForm : Form
    {
        private User currentUser;
        private DataGridView dgvCustomers;
        private TextBox txtSearch;
        private Label lblTotalCustomers, lblTotalOrders, lblNewOrders;
        private Button btnBack;

        public CustomerManagementForm(User user)
        {
            currentUser = user;
            InitializeComponents();
            LoadData();
        }

        private void InitializeComponents()
        {
            this.Text = "Customer Management";
            this.Size = new Size(1000, 560);
            this.StartPosition = FormStartPosition.CenterScreen;

            var lblTitle = new Label()
            {
                Text = "Customer Management",
                Location = new Point(20, 15), Size = new Size(400, 26),
                Font = new Font("Arial", 13, FontStyle.Bold)
            };

            // ── Stats strip ───────────────────────────────────────────
            lblTotalCustomers = new Label() { Location = new Point(20, 50),  Size = new Size(220, 22), Font = new Font("Arial", 10) };
            lblTotalOrders    = new Label() { Location = new Point(255, 50), Size = new Size(220, 22), Font = new Font("Arial", 10) };
            lblNewOrders      = new Label() { Location = new Point(490, 50), Size = new Size(220, 22), Font = new Font("Arial", 10), ForeColor = Color.Crimson };

            // ── Search bar ────────────────────────────────────────────
            this.Controls.Add(new Label() { Text = "Filter by email:", Location = new Point(20, 83), Size = new Size(105, 22) });
            txtSearch = new TextBox() { Location = new Point(130, 81), Size = new Size(260, 25) };
            txtSearch.TextChanged += (s, e) => LoadData(txtSearch.Text);

            // ── Grid ─────────────────────────────────────────────────
            dgvCustomers = new DataGridView()
            {
                Location = new Point(20, 115),
                Size = new Size(940, 360),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false
            };

            btnBack = new Button()
            {
                Text = "← Back to Dashboard", Location = new Point(20, 490), Size = new Size(170, 32)
            };
            btnBack.Click += (s, e) => { new AdminDashboardForm(currentUser).Show(); this.Close(); };

            this.Controls.AddRange(new Control[]
            {
                lblTitle, lblTotalCustomers, lblTotalOrders, lblNewOrders,
                txtSearch, dgvCustomers, btnBack
            });
        }

        private void LoadData(string search = "")
        {
            var ctrl = new CustomerManagementController();

            // Load stats
            var stats = ctrl.GetCustomerStats();
            lblTotalCustomers.Text = $"Total Customers: {stats["TotalCustomers"]}";
            lblTotalOrders.Text    = $"Total Orders: {stats["TotalOrders"]}";
            lblNewOrders.Text      = $"New (pending) Orders: {stats["NewOrders"]}";

            // Load grid
            dgvCustomers.DataSource = ctrl.GetCustomerList(search);
        }
    }
}
