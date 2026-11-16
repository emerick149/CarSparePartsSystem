using System;
using System.Drawing;
using System.Windows.Forms;
using CarSparePartsSystem.Models;

namespace CarSparePartsSystem.Views
{
    public class CustomerDashboardForm : Form
    {
        private User currentUser;
        private Label lblWelcome, lblTitle;
        private Button btnBrowseParts, btnByVehicle, btnMyOrders, btnLogout;

        public CustomerDashboardForm(User user)
        {
            currentUser = user;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Customer Dashboard";
            this.Size = new Size(500, 460);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            lblWelcome = new Label()
            {
                Text = $"Welcome, {currentUser.Email}",
                Location = new Point(20, 20), Size = new Size(450, 28),
                Font = new Font("Arial", 12, FontStyle.Bold)
            };
            lblTitle = new Label()
            {
                Text = "Customer Menu",
                Location = new Point(20, 58), Size = new Size(450, 30),
                Font = new Font("Arial", 15, FontStyle.Bold)
            };

            btnBrowseParts = MakeButton("🔍  Browse & Search Spare Parts", Color.DodgerBlue,     118);
            btnByVehicle   = MakeButton("🚗  Find Parts by My Vehicle",    Color.MediumSlateBlue, 178);
            btnMyOrders    = MakeButton("📋  View My Order History",        Color.MediumSeaGreen,  238);
            btnLogout      = MakeButton("🚪  Logout",                       Color.Crimson,          318);

            btnBrowseParts.Click += (s, e) => { new BrowsePartsForm(currentUser).Show(); this.Hide(); };
            btnByVehicle.Click   += (s, e) => { new SearchByVehicleForm(currentUser).Show(); };
            btnMyOrders.Click    += (s, e) => { new OrderHistoryForm(currentUser).Show(); this.Hide(); };
            btnLogout.Click      += (s, e) => { new LoginForm().Show(); this.Close(); };

            this.Controls.AddRange(new Control[]
            {
                lblWelcome, lblTitle, btnBrowseParts, btnByVehicle, btnMyOrders, btnLogout
            });
        }

        private Button MakeButton(string text, Color color, int y)
        {
            return new Button()
            {
                Text = text,
                Location = new Point(40, y), Size = new Size(400, 48),
                BackColor = color, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 11),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
        }
    }
}
