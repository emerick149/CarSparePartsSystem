using CarSparePartsSystem.Controllers;
using CarSparePartsSystem.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CarSparePartsSystem.Views
{
    public class ManageSparePartsForm : Form
    {
        private User currentUser;
        private DataGridView dgvParts;
        private TextBox txtSku, txtName, txtCategory, txtPrice, txtStock, txtSearch;
        private Button btnAdd, btnUpdate, btnDelete, btnRefresh, btnBack;
        private int selectedId = 0;

        // SFR-05: sort state
        private string sortColumn    = "Name";
        private string sortDirection = "ASC";

        public ManageSparePartsForm(User user)
        {
            currentUser = user;
            InitializeComponents();
            LoadParts();
        }

        private void InitializeComponents()
        {
            this.Text = "Manage Spare Parts";
            this.Size = new Size(960, 640);
            this.StartPosition = FormStartPosition.CenterScreen;

            // ── Grid ─────────────────────────────────────────────────
            dgvParts = new DataGridView()
            {
                Location = new Point(20, 20),
                Size = new Size(900, 270),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false
            };
            dgvParts.SelectionChanged   += DgvParts_SelectionChanged;
            dgvParts.ColumnHeaderMouseClick += DgvParts_ColumnHeaderMouseClick; // SFR-05

            // ── Input fields ─────────────────────────────────────────
            int y = 310;
            this.Controls.Add(new Label() { Text = "SKU:",      Location = new Point(20,  y), Size = new Size(70, 22) });
            txtSku      = new TextBox() { Location = new Point(95,  y - 2), Size = new Size(130, 25) };

            this.Controls.Add(new Label() { Text = "Name:",     Location = new Point(240, y), Size = new Size(50, 22) });
            txtName     = new TextBox() { Location = new Point(295, y - 2), Size = new Size(220, 25) };

            this.Controls.Add(new Label() { Text = "Category:", Location = new Point(530, y), Size = new Size(70, 22) });
            txtCategory = new TextBox() { Location = new Point(610, y - 2), Size = new Size(130, 25) };

            y += 38;
            this.Controls.Add(new Label() { Text = "Price:",    Location = new Point(20,  y), Size = new Size(70, 22) });
            txtPrice    = new TextBox() { Location = new Point(95,  y - 2), Size = new Size(100, 25) };

            this.Controls.Add(new Label() { Text = "Stock:",    Location = new Point(210, y), Size = new Size(50, 22) });
            txtStock    = new TextBox() { Location = new Point(265, y - 2), Size = new Size(80,  25) };

            // ── Search bar ───────────────────────────────────────────
            y += 48;
            this.Controls.Add(new Label() { Text = "Search:", Location = new Point(20, y), Size = new Size(60, 22) });
            txtSearch = new TextBox() { Location = new Point(85, y - 2), Size = new Size(220, 25) };
            txtSearch.TextChanged += (s, e) => LoadParts(txtSearch.Text);

            // ── Action buttons ───────────────────────────────────────
            btnAdd = new Button()
            {
                Text = "Add New", Location = new Point(330, y - 4), Size = new Size(100, 30),
                BackColor = Color.Green, ForeColor = Color.White
            };
            btnAdd.Click += (s, e) => SavePart();

            btnUpdate = new Button()
            {
                Text = "Update", Location = new Point(440, y - 4), Size = new Size(100, 30),
                BackColor = Color.DarkOrange, ForeColor = Color.White
            };
            btnUpdate.Click += (s, e) => UpdatePart();

            btnDelete = new Button()
            {
                Text = "Delete", Location = new Point(550, y - 4), Size = new Size(100, 30),
                BackColor = Color.Crimson, ForeColor = Color.White
            };
            btnDelete.Click += (s, e) => DeletePart();

            btnRefresh = new Button()
            {
                Text = "Refresh", Location = new Point(660, y - 4), Size = new Size(100, 30)
            };
            btnRefresh.Click += (s, e) => { LoadParts(); ClearInputs(); };

            y += 45;
            btnBack = new Button()
            {
                Text = "← Back to Dashboard", Location = new Point(20, y), Size = new Size(170, 30)
            };
            btnBack.Click += (s, e) => { new AdminDashboardForm(currentUser).Show(); this.Close(); };

            this.Controls.AddRange(new Control[]
            {
                dgvParts, txtSku, txtName, txtCategory, txtPrice, txtStock,
                txtSearch, btnAdd, btnUpdate, btnDelete, btnRefresh, btnBack
            });
        }

        // SFR-05: sort by clicking a column header
        private void DgvParts_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            string clicked = dgvParts.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrEmpty(clicked)) clicked = dgvParts.Columns[e.ColumnIndex].Name;

            if (clicked == sortColumn)
                sortDirection = (sortDirection == "ASC") ? "DESC" : "ASC";
            else
            {
                sortColumn    = clicked;
                sortDirection = "ASC";
            }
            LoadParts(txtSearch.Text);
        }

        private void LoadParts(string search = "")
        {
            var ctrl = new SparePartController();
            dgvParts.DataSource = ctrl.GetAllParts(search, sortColumn, sortDirection);
        }

        private void DgvParts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvParts.CurrentRow == null) return;
            selectedId          = Convert.ToInt32(dgvParts.CurrentRow.Cells["Id"].Value);
            txtSku.Text         = dgvParts.CurrentRow.Cells["Sku"].Value?.ToString()          ?? "";
            txtName.Text        = dgvParts.CurrentRow.Cells["Name"].Value?.ToString()         ?? "";
            txtCategory.Text    = dgvParts.CurrentRow.Cells["Category"].Value?.ToString()     ?? "";
            txtPrice.Text       = dgvParts.CurrentRow.Cells["Price"].Value?.ToString()        ?? "";
            txtStock.Text       = dgvParts.CurrentRow.Cells["StockQuantity"].Value?.ToString() ?? "";
        }

        private void SavePart()
        {
            if (!decimal.TryParse(txtPrice.Text, out decimal price))  { MessageBox.Show("Price must be a number."); return; }
            if (!int.TryParse(txtStock.Text, out int stock))          { MessageBox.Show("Stock must be an integer."); return; }

            var ctrl = new SparePartController();
            string result = ctrl.AddPart(txtSku.Text, txtName.Text, txtCategory.Text, price, stock);
            MessageBox.Show(result, result == "Success" ? "Added" : "Error",
                MessageBoxButtons.OK, result == "Success" ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (result == "Success") { LoadParts(); ClearInputs(); }
        }

        private void UpdatePart()
        {
            if (selectedId == 0) { MessageBox.Show("Select a part first."); return; }
            if (!decimal.TryParse(txtPrice.Text, out decimal price))  { MessageBox.Show("Price must be a number."); return; }
            if (!int.TryParse(txtStock.Text, out int stock))          { MessageBox.Show("Stock must be an integer."); return; }

            var ctrl = new SparePartController();
            string result = ctrl.UpdatePart(selectedId, txtSku.Text, txtName.Text, txtCategory.Text, price, stock);
            MessageBox.Show(result, result == "Success" ? "Updated" : "Error",
                MessageBoxButtons.OK, result == "Success" ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (result == "Success") { LoadParts(); ClearInputs(); }
        }

        private void DeletePart()
        {
            if (selectedId == 0) { MessageBox.Show("Select a part first."); return; }
            if (MessageBox.Show("Are you sure you want to delete this part?", "Confirm Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            var ctrl = new SparePartController();
            string result = ctrl.DeletePart(selectedId);
            MessageBox.Show(result, result == "Success" ? "Deleted" : "Cannot Delete",
                MessageBoxButtons.OK, result == "Success" ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (result == "Success") { LoadParts(); ClearInputs(); }
        }

        private void ClearInputs()
        {
            selectedId = 0;
            txtSku.Clear(); txtName.Clear(); txtCategory.Clear(); txtPrice.Clear(); txtStock.Clear();
        }
    }
}
