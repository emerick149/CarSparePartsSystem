using CarSparePartsSystem.Controllers;
using CarSparePartsSystem.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CarSparePartsSystem.Views
{
    public class BrowsePartsForm : Form
    {
        private User currentUser;
        private DataGridView dgvParts, dgvCompatible;
        private TextBox txtSearch, txtQuantity;
        private ComboBox cmbCategory;
        private CheckBox chkInStockOnly;
        private Button btnFilter, btnAddToCart, btnViewCart, btnBack;
        private Label lblMessage, lblCompatTitle;

        // SFR-05: sort state
        private string sortColumn    = "Name";
        private string sortDirection = "ASC";

        public BrowsePartsForm(User user)
        {
            currentUser = user;
            InitializeComponents();
            PopulateCategoryDropdown(); // SFR-12: load categories from DB
            LoadParts();
        }

        private void InitializeComponents()
        {
            this.Text = "Browse Spare Parts";
            this.Size = new Size(900, 660);
            this.StartPosition = FormStartPosition.CenterScreen;

            // ── Filter bar ────────────────────────────────────────────
            this.Controls.Add(new Label() { Text = "Search:",   Location = new Point(20, 20), Size = new Size(60, 22) });
            txtSearch = new TextBox() { Location = new Point(82, 18), Size = new Size(190, 25) };

            this.Controls.Add(new Label() { Text = "Category:", Location = new Point(285, 20), Size = new Size(65, 22) });
            cmbCategory = new ComboBox()
            {
                Location = new Point(354, 18), Size = new Size(160, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            chkInStockOnly = new CheckBox() { Text = "In Stock Only", Location = new Point(530, 20), Size = new Size(115, 22) };

            btnFilter = new Button()
            {
                Text = "Filter", Location = new Point(655, 16), Size = new Size(80, 28),
                BackColor = Color.DodgerBlue, ForeColor = Color.White
            };
            btnFilter.Click += (s, e) => LoadParts();

            // ── Main parts grid ───────────────────────────────────────
            dgvParts = new DataGridView()
            {
                Location = new Point(20, 55),
                Size = new Size(840, 260),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false
            };
            dgvParts.SelectionChanged       += DgvParts_SelectionChanged;
            dgvParts.ColumnHeaderMouseClick += DgvParts_ColumnHeaderMouseClick; // SFR-05

            // ── Order / cart controls ─────────────────────────────────
            int y = 328;
            this.Controls.Add(new Label() { Text = "Quantity:", Location = new Point(20, y + 3), Size = new Size(70, 22) });
            txtQuantity = new TextBox() { Location = new Point(94, y), Size = new Size(70, 25), Text = "1" };

            btnAddToCart = new Button()
            {
                Text = "Add to Cart", Location = new Point(180, y - 2), Size = new Size(130, 30),
                BackColor = Color.DarkOrange, ForeColor = Color.White
            };
            btnAddToCart.Click += BtnAddToCart_Click;

            btnViewCart = new Button()
            {
                Text = "View Cart 🛒", Location = new Point(325, y - 2), Size = new Size(120, 30),
                BackColor = Color.MediumSeaGreen, ForeColor = Color.White
            };
            btnViewCart.Click += (s, e) => { new CartForm(currentUser).Show(); this.Hide(); };

            lblMessage = new Label()
            {
                Text = "", Location = new Point(20, 367), Size = new Size(840, 24),
                ForeColor = Color.Crimson, Font = new Font("Arial", 9, FontStyle.Bold)
            };

            // ── Compatible vehicles for selected part (SFR-11) ────────
            lblCompatTitle = new Label()
            {
                Text = "Compatible Vehicles for selected part:",
                Location = new Point(20, 398), Size = new Size(500, 22),
                Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = Color.DimGray
            };

            dgvCompatible = new DataGridView()
            {
                Location = new Point(20, 424),
                Size = new Size(840, 150),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false
            };

            btnBack = new Button()
            {
                Text = "← Dashboard", Location = new Point(20, 585), Size = new Size(130, 32)
            };
            btnBack.Click += (s, e) => { new CustomerDashboardForm(currentUser).Show(); this.Close(); };

            this.Controls.AddRange(new Control[]
            {
                txtSearch, cmbCategory, chkInStockOnly, btnFilter,
                dgvParts, txtQuantity, btnAddToCart, btnViewCart,
                lblMessage, lblCompatTitle, dgvCompatible, btnBack
            });
        }

        // SFR-12: Load categories from DB instead of hard-coding them
        private void PopulateCategoryDropdown()
        {
            cmbCategory.Items.Clear();
            cmbCategory.Items.Add("All");
            var ctrl = new CustomerController();
            foreach (var cat in ctrl.GetCategories())
                cmbCategory.Items.Add(cat);
            cmbCategory.SelectedIndex = 0;
        }

        // SFR-05: sort by clicking column header
        private void DgvParts_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            string clicked = dgvParts.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrEmpty(clicked)) clicked = dgvParts.Columns[e.ColumnIndex].Name;

            sortDirection = (clicked == sortColumn && sortDirection == "ASC") ? "DESC" : "ASC";
            sortColumn = clicked;
            LoadParts();
        }

        private void LoadParts()
        {
            lblMessage.Text = "";
            string cat = (cmbCategory.SelectedIndex <= 0) ? "" : cmbCategory.SelectedItem.ToString();
            var ctrl = new CustomerController();
            dgvParts.DataSource = ctrl.GetAvailableParts(
                txtSearch.Text, cat, chkInStockOnly.Checked, sortColumn, sortDirection);
        }

        // SFR-11: Show compatible vehicles when a part row is selected
        private void DgvParts_SelectionChanged(object sender, EventArgs e)
        {
            lblMessage.Text = "";
            if (dgvParts.CurrentRow == null) return;
            int partId = Convert.ToInt32(dgvParts.CurrentRow.Cells["Id"].Value);
            string partName = dgvParts.CurrentRow.Cells["Name"].Value?.ToString() ?? "";

            lblCompatTitle.Text = $"Compatible vehicles for: {partName}";
            var custCtrl = new CustomerController();
            dgvCompatible.DataSource = custCtrl.GetCompatibleVehicles(partId);
        }

        private void BtnAddToCart_Click(object sender, EventArgs e)
        {
            lblMessage.Text = "";
            if (dgvParts.CurrentRow == null) { lblMessage.Text = "Please select a part."; return; }

            if (!int.TryParse(txtQuantity.Text, out int qty) || qty <= 0)
            { lblMessage.Text = "Enter a valid quantity (whole number > 0)."; return; }

            int partId   = Convert.ToInt32(dgvParts.CurrentRow.Cells["Id"].Value);
            int stock    = Convert.ToInt32(dgvParts.CurrentRow.Cells["StockQuantity"].Value);
            string name  = dgvParts.CurrentRow.Cells["Name"].Value?.ToString() ?? "";
            decimal price = Convert.ToDecimal(dgvParts.CurrentRow.Cells["Price"].Value);

            if (qty > stock)
            { lblMessage.Text = $"Only {stock} in stock."; return; }

            CartManager.AddToCart(new CartItem
            {
                PartId = partId, Name = name, Price = price,
                Quantity = qty, MaxStock = stock
            });
            lblMessage.Text = $"✔ '{name}' (x{qty}) added to cart.";
            lblMessage.ForeColor = Color.DarkGreen;
        }
    }
}
