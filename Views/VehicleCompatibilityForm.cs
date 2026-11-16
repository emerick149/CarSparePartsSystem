using CarSparePartsSystem.Controllers;
using CarSparePartsSystem.Models;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace CarSparePartsSystem.Views
{
    /// <summary>
    /// FR-06 / SFR-10 / SFR-11: Admin UI for managing vehicle fitment records.
    /// Left panel: select a part → see its fitments.
    /// Right panel: add a new fitment or delete a selected one.
    /// </summary>
    public class VehicleCompatibilityForm : Form
    {
        private User currentUser;

        // Left — parts list
        private DataGridView dgvParts;
        private TextBox txtPartSearch;

        // Right — fitments for selected part
        private DataGridView dgvFitments;
        private Label lblSelectedPart;

        // Add-fitment inputs
        private TextBox txtMake, txtModel, txtYearStart, txtYearEnd;
        private Button btnAddFitment, btnDeleteFitment, btnBack, btnSearchByVehicle;

        private int selectedPartId   = 0;
        private string selectedPartName = "";

        public VehicleCompatibilityForm(User user)
        {
            currentUser = user;
            InitializeComponents();
            LoadParts();
        }

        private void InitializeComponents()
        {
            this.Text = "Vehicle Compatibility Management";
            this.Size = new Size(1050, 650);
            this.StartPosition = FormStartPosition.CenterScreen;

            // ══════════════════════════════════════════════════════════
            // LEFT PANEL — parts list
            // ══════════════════════════════════════════════════════════
            var lblPartsTitle = new Label()
            {
                Text = "Select a Spare Part",
                Location = new Point(15, 15), Size = new Size(300, 22),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            var lblPartSearch = new Label() { Text = "Search:", Location = new Point(15, 44), Size = new Size(55, 22) };
            txtPartSearch = new TextBox() { Location = new Point(75, 42), Size = new Size(220, 25) };
            txtPartSearch.TextChanged += (s, e) => LoadParts(txtPartSearch.Text);

            dgvParts = new DataGridView()
            {
                Location = new Point(15, 76),
                Size = new Size(390, 480),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false
            };
            dgvParts.SelectionChanged += DgvParts_SelectionChanged;

            // ══════════════════════════════════════════════════════════
            // RIGHT PANEL — fitments for selected part
            // ══════════════════════════════════════════════════════════
            lblSelectedPart = new Label()
            {
                Text = "No part selected",
                Location = new Point(425, 15), Size = new Size(590, 22),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DodgerBlue
            };

            dgvFitments = new DataGridView()
            {
                Location = new Point(425, 44),
                Size = new Size(590, 220),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false
            };

            // ── Add-fitment form ──────────────────────────────────────
            int fy = 280;
            var lblAdd = new Label()
            {
                Text = "Add New Fitment Record",
                Location = new Point(425, fy), Size = new Size(590, 22),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            fy += 28;

            this.Controls.Add(new Label() { Text = "Make:",       Location = new Point(425, fy), Size = new Size(60, 22) });
            txtMake = new TextBox() { Location = new Point(490, fy - 2), Size = new Size(130, 25) };

            this.Controls.Add(new Label() { Text = "Model:",      Location = new Point(635, fy), Size = new Size(50, 22) });
            txtModel = new TextBox() { Location = new Point(690, fy - 2), Size = new Size(130, 25) };
            fy += 36;

            this.Controls.Add(new Label() { Text = "Year Start:", Location = new Point(425, fy), Size = new Size(80, 22) });
            txtYearStart = new TextBox() { Location = new Point(510, fy - 2), Size = new Size(80, 25) };

            this.Controls.Add(new Label() { Text = "Year End:",   Location = new Point(610, fy), Size = new Size(70, 22) });
            txtYearEnd = new TextBox() { Location = new Point(685, fy - 2), Size = new Size(80, 25) };
            fy += 42;

            btnAddFitment = new Button()
            {
                Text = "Add Fitment", Location = new Point(425, fy), Size = new Size(150, 32),
                BackColor = Color.Green, ForeColor = Color.White
            };
            btnAddFitment.Click += BtnAddFitment_Click;

            btnDeleteFitment = new Button()
            {
                Text = "Delete Selected Fitment", Location = new Point(590, fy), Size = new Size(200, 32),
                BackColor = Color.Crimson, ForeColor = Color.White
            };
            btnDeleteFitment.Click += BtnDeleteFitment_Click;

            // ── Bottom buttons ────────────────────────────────────────
            fy += 60;
            btnSearchByVehicle = new Button()
            {
                Text = "🔍  Search Parts by Vehicle →", Location = new Point(425, fy), Size = new Size(250, 32),
                BackColor = Color.MediumSlateBlue, ForeColor = Color.White
            };
            btnSearchByVehicle.Click += (s, e) => { new SearchByVehicleForm(currentUser).Show(); };

            btnBack = new Button()
            {
                Text = "← Back to Dashboard", Location = new Point(15, 575), Size = new Size(170, 32)
            };
            btnBack.Click += (s, e) => { new AdminDashboardForm(currentUser).Show(); this.Close(); };

            this.Controls.AddRange(new Control[]
            {
                lblPartsTitle, lblPartSearch, txtPartSearch, dgvParts,
                lblSelectedPart, dgvFitments,
                lblAdd, txtMake, txtModel, txtYearStart, txtYearEnd,
                btnAddFitment, btnDeleteFitment, btnSearchByVehicle, btnBack
            });
        }

        private void LoadParts(string search = "")
        {
            var ctrl = new SparePartController();
            dgvParts.DataSource = ctrl.GetAllParts(search);
        }

        private void DgvParts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvParts.CurrentRow == null) return;
            selectedPartId   = Convert.ToInt32(dgvParts.CurrentRow.Cells["Id"].Value);
            selectedPartName = dgvParts.CurrentRow.Cells["Name"].Value?.ToString() ?? "";
            lblSelectedPart.Text = $"Fitments for: {selectedPartName} (ID {selectedPartId})";
            LoadFitments();
        }

        private void LoadFitments()
        {
            if (selectedPartId == 0) return;
            var ctrl = new VehicleCompatibilityController();
            dgvFitments.DataSource = ctrl.GetFitments(selectedPartId);
        }

        private void BtnAddFitment_Click(object sender, EventArgs e)
        {
            if (selectedPartId == 0) { MessageBox.Show("Select a part first."); return; }

            if (!int.TryParse(txtYearStart.Text, out int yearStart))
            { MessageBox.Show("Year Start must be a 4-digit number."); return; }
            if (!int.TryParse(txtYearEnd.Text, out int yearEnd))
            { MessageBox.Show("Year End must be a 4-digit number."); return; }

            var ctrl = new VehicleCompatibilityController();
            string result = ctrl.AddFitment(selectedPartId, txtMake.Text, txtModel.Text, yearStart, yearEnd);
            MessageBox.Show(result, result == "Success" ? "Added" : "Error",
                MessageBoxButtons.OK, result == "Success" ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            if (result == "Success")
            {
                LoadFitments();
                txtMake.Clear(); txtModel.Clear(); txtYearStart.Clear(); txtYearEnd.Clear();
            }
        }

        private void BtnDeleteFitment_Click(object sender, EventArgs e)
        {
            if (dgvFitments.CurrentRow == null) { MessageBox.Show("Select a fitment record to delete."); return; }

            int fitmentId = Convert.ToInt32(dgvFitments.CurrentRow.Cells["Id"].Value);
            if (MessageBox.Show("Delete this fitment record?", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            var ctrl = new VehicleCompatibilityController();
            string result = ctrl.DeleteFitment(fitmentId);
            MessageBox.Show(result, result == "Success" ? "Deleted" : "Error",
                MessageBoxButtons.OK, result == "Success" ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (result == "Success") LoadFitments();
        }
    }
}
