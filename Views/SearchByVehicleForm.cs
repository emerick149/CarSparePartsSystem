using CarSparePartsSystem.Controllers;
using CarSparePartsSystem.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CarSparePartsSystem.Views
{
    /// <summary>
    /// SFR-11: Search for parts compatible with a given make / model / year.
    /// Accessible from both the Customer Dashboard and the VehicleCompatibilityForm.
    /// </summary>
    public class SearchByVehicleForm : Form
    {
        private User currentUser;
        private TextBox txtMake, txtModel, txtYear;
        private DataGridView dgvResults;
        private Button btnSearch, btnBack;
        private Label lblResultCount;

        public SearchByVehicleForm(User user)
        {
            currentUser = user;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Find Parts by Vehicle";
            this.Size = new Size(820, 520);
            this.StartPosition = FormStartPosition.CenterScreen;

            // ── Filter bar ────────────────────────────────────────────
            int x = 20, y = 20;
            this.Controls.Add(new Label() { Text = "Make:",  Location = new Point(x, y + 2), Size = new Size(45, 22) });
            x += 48;
            txtMake = new TextBox() { Location = new Point(x, y), Size = new Size(140, 25) };
            x += 155;

            this.Controls.Add(new Label() { Text = "Model:", Location = new Point(x, y + 2), Size = new Size(48, 22) });
            x += 52;
            txtModel = new TextBox() { Location = new Point(x, y), Size = new Size(140, 25) };
            x += 155;

            this.Controls.Add(new Label() { Text = "Year:",  Location = new Point(x, y + 2), Size = new Size(40, 22) });
            x += 44;
            txtYear = new TextBox() { Location = new Point(x, y), Size = new Size(80, 25) };
            // placeholder hint shown via label — PlaceholderText is .NET 5+ only
            x += 95;

            btnSearch = new Button()
            {
                Text = "Search", Location = new Point(x, y - 2), Size = new Size(90, 30),
                BackColor = Color.DodgerBlue, ForeColor = Color.White
            };
            btnSearch.Click += BtnSearch_Click;

            // ── Results ───────────────────────────────────────────────
            lblResultCount = new Label()
            {
                Text = "Enter make/model/year and click Search.",
                Location = new Point(20, 60), Size = new Size(760, 22),
                ForeColor = Color.DimGray
            };

            dgvResults = new DataGridView()
            {
                Location = new Point(20, 88),
                Size = new Size(760, 340),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false
            };

            btnBack = new Button()
            {
                Text = "← Back", Location = new Point(20, 445), Size = new Size(120, 30)
            };
            btnBack.Click += BtnBack_Click;

            this.Controls.AddRange(new Control[]
            {
                txtMake, txtModel, txtYear, btnSearch,
                lblResultCount, dgvResults, btnBack
            });
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            int year = 0;
            if (!string.IsNullOrWhiteSpace(txtYear.Text) && !int.TryParse(txtYear.Text.Trim(), out year))
            {
                MessageBox.Show("Year must be a number (e.g. 2019).", "Invalid Year",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ctrl = new VehicleCompatibilityController();
            var results = ctrl.SearchCompatibleParts(txtMake.Text.Trim(), txtModel.Text.Trim(), year);
            dgvResults.DataSource = results;
            lblResultCount.Text = $"{results.Rows.Count} compatible part(s) found.";
            lblResultCount.ForeColor = results.Rows.Count > 0 ? Color.DarkGreen : Color.Crimson;
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (currentUser.Role == "Administrator")
                new AdminDashboardForm(currentUser).Show();
            else
                new CustomerDashboardForm(currentUser).Show();
            this.Close();
        }
    }
}
