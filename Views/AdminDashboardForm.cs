using System;
using System.Drawing;
using System.Windows.Forms;
using CarSparePartsSystem.Controllers;
using CarSparePartsSystem.Models;

namespace CarSparePartsSystem.Views
{
    public class AdminDashboardForm : Form
    {
        private User currentUser;
        private Label lblWelcome, lblTitle;
        private Label lblTotalParts, lblTotalStock, lblLowStock, lblValue, lblFitments, lblCategories;
        private Button btnManageParts, btnVehicleCompat, btnOrders, btnCustomers, btnLogout;

        public AdminDashboardForm(User user)
        {
            currentUser = user;
            InitializeComponents();
            LoadStats();
        }

        private void InitializeComponents()
        {
            this.Text = "Admin Dashboard";
            this.Size = new Size(650, 540);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            lblWelcome = new Label()
            {
                Text = $"Welcome, {currentUser.Email}",
                Location = new Point(20, 15),
                Size = new Size(600, 25),
                Font = new Font("Arial", 11, FontStyle.Bold)
            };

            lblTitle = new Label()
            {
                Text = "Inventory Dashboard",
                Location = new Point(20, 48),
                Size = new Size(400, 30),
                Font = new Font("Arial", 15, FontStyle.Bold)
            };

            // ── Stats panel ──────────────────────────────────────────
            int sy = 95;
            lblTotalParts = MakeStat("Total Parts: 0",      sy);       sy += 35;
            lblTotalStock = MakeStat("Total Stock Units: 0", sy);      sy += 35;
            lblLowStock   = MakeStat("Low Stock Items: 0",  sy);       sy += 35;
            lblValue      = MakeStat("Inventory Value: $0.00", sy);    sy += 35;
            lblFitments   = MakeStat("Vehicle Fitment Records: 0", sy); sy += 35;
            lblCategories = MakeStat("Categories: —", sy);

            lblLowStock.ForeColor = Color.Crimson;

            // ── Buttons ──────────────────────────────────────────────
            int by = 320;
            btnManageParts = MakeButton("📦  Manage Spare Parts",   Color.DodgerBlue,    by);       by += 48;
            btnVehicleCompat = MakeButton("🚗  Vehicle Compatibility", Color.MediumSlateBlue, by);   by += 48;
            btnOrders      = MakeButton("📋  Order Management",     Color.DarkOrange,    by);       by += 48;
            btnCustomers   = MakeButton("👥  Customer Management",  Color.MediumSeaGreen, by);      by += 48;
            btnLogout      = MakeButton("🚪  Logout",               Color.Crimson,       by);

            btnManageParts.Click   += (s, e) => { new ManageSparePartsForm(currentUser).Show(); this.Hide(); };
            btnVehicleCompat.Click += (s, e) => { new VehicleCompatibilityForm(currentUser).Show(); this.Hide(); };
            btnOrders.Click        += (s, e) => { new OrderManagementForm(currentUser).Show(); this.Hide(); };
            btnCustomers.Click     += (s, e) => { new CustomerManagementForm(currentUser).Show(); this.Hide(); };
            btnLogout.Click        += (s, e) => { new LoginForm().Show(); this.Close(); };

            this.Controls.AddRange(new Control[]
            {
                lblWelcome, lblTitle,
                lblTotalParts, lblTotalStock, lblLowStock, lblValue, lblFitments, lblCategories,
                btnManageParts, btnVehicleCompat, btnOrders, btnCustomers, btnLogout
            });
        }

        private Label MakeStat(string text, int y)
        {
            var lbl = new Label()
            {
                Text = text,
                Location = new Point(30, y),
                Size = new Size(580, 26),
                Font = new Font("Arial", 10)
            };
            return lbl;
        }

        private Button MakeButton(string text, Color color, int y)
        {
            return new Button()
            {
                Text = text,
                Location = new Point(20, y),
                Size = new Size(590, 38),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
        }

        private void LoadStats()
        {
            try
            {
                var ctrl = new SparePartController();
                var stats = ctrl.GetDashboardStats();

                lblTotalParts.Text  = $"Total Parts: {stats["TotalParts"]}";
                lblTotalStock.Text  = $"Total Stock Units: {stats["TotalStock"]}";
                lblLowStock.Text    = $"Low Stock Items (< 5 units): {stats["LowStock"]}";
                lblValue.Text       = $"Inventory Value: {stats["InventoryValue"]}";
                lblFitments.Text    = $"Vehicle Fitment Records: {stats["TotalFitments"]}";
                lblCategories.Text  = $"Categories: {stats["Categories"]}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load stats: " + ex.Message, "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
