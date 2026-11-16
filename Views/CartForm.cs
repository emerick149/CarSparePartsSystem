using System;
using System.Drawing;
using System.Windows.Forms;
using CarSparePartsSystem.Models;
using CarSparePartsSystem.Data;
using MySqlConnector;

namespace CarSparePartsSystem.Views
{
    public class CartForm : Form
    {
        private User currentUser;
        private DataGridView dgvCart;
        private Label lblTotal;
        private Button btnCheckout, btnClear, btnBack;
        private readonly string connectionString = AppConfig.ConnectionString;

        public CartForm(User user)
        {
            currentUser = user;
            InitializeComponents();
            LoadCart();
        }

        private void InitializeComponents()
        {
            this.Text = "Shopping Cart";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            dgvCart = new DataGridView() { Location = new Point(20, 20), Size = new Size(540, 300), ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            lblTotal = new Label() { Text = "Total: $0.00", Location = new Point(20, 340), Font = new Font("Arial", 14, FontStyle.Bold), Size = new Size(300, 30) };

            btnCheckout = new Button() { Text = "Place Order", Location = new Point(20, 390), Size = new Size(200, 40), BackColor = Color.Green, ForeColor = Color.White };
            btnCheckout.Click += BtnCheckout_Click;

            btnClear = new Button() { Text = "Clear Cart", Location = new Point(240, 390), Size = new Size(150, 40), BackColor = Color.Red, ForeColor = Color.White };
            btnClear.Click += (s, e) => { CartManager.Clear(); LoadCart(); };

            btnBack = new Button() { Text = "Continue Shopping", Location = new Point(20, 440), Size = new Size(150, 30) };
            btnBack.Click += (s, e) => { new BrowsePartsForm(currentUser).Show(); this.Close(); };

            this.Controls.AddRange(new Control[] { dgvCart, lblTotal, btnCheckout, btnClear, btnBack });
        }

        private void LoadCart()
        {
            dgvCart.DataSource = null;
            dgvCart.Rows.Clear();
            dgvCart.Columns.Clear();

            dgvCart.Columns.Add("Name", "Part Name");
            dgvCart.Columns.Add("Price", "Unit Price");
            dgvCart.Columns.Add("Qty", "Quantity");
            dgvCart.Columns.Add("Total", "Line Total");

            foreach (var item in CartManager.Cart)
            {
                dgvCart.Rows.Add(item.Name, item.Price.ToString("C"), item.Quantity, (item.Price * item.Quantity).ToString("C"));
            }

            lblTotal.Text = $"Total: {CartManager.GetTotal():C}";
        }

        private void BtnCheckout_Click(object sender, EventArgs e)
        {
            if (CartManager.Cart.Count == 0) { MessageBox.Show("Your cart is empty."); return; }

            // FR-08 & NFR-02: Place Order and Reserve Stock using Transactions
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                var transaction = conn.BeginTransaction();

                try
                {
                    decimal total = CartManager.GetTotal();

                    // 1. Create Order
                    string orderSql = "INSERT INTO Orders (CustomerId, TotalAmount, Status) VALUES (@CustId, @Total, 'New'); SELECT LAST_INSERT_ID();";
                    int orderId;
                    using (var cmd = new MySqlCommand(orderSql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CustId", currentUser.Id);
                        cmd.Parameters.AddWithValue("@Total", total);
                        orderId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // 2. Create Order Items & Reduce Stock
                    foreach (var item in CartManager.Cart)
                    {
                        // Check stock again
                        string checkStockSql = "SELECT StockQuantity FROM SpareParts WHERE Id = @Id FOR UPDATE;";
                        using (var cmd = new MySqlCommand(checkStockSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", item.PartId);
                            int currentStock = Convert.ToInt32(cmd.ExecuteScalar());
                            if (currentStock < item.Quantity) throw new Exception($"Insufficient stock for {item.Name}.");
                        }

                        // Insert OrderItem (SFR-16: Save unit price)
                        string itemSql = "INSERT INTO OrderItems (OrderId, SparePartId, Quantity, UnitPrice) VALUES (@OrderId, @PartId, @Qty, @Price);";
                        using (var cmd = new MySqlCommand(itemSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrderId", orderId);
                            cmd.Parameters.AddWithValue("@PartId", item.PartId);
                            cmd.Parameters.AddWithValue("@Qty", item.Quantity);
                            cmd.Parameters.AddWithValue("@Price", item.Price);
                            cmd.ExecuteNonQuery();
                        }

                        // Reduce Stock (SFR-17)
                        string updateStockSql = "UPDATE SpareParts SET StockQuantity = StockQuantity - @Qty WHERE Id = @PartId;";
                        using (var cmd = new MySqlCommand(updateStockSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Qty", item.Quantity);
                            cmd.Parameters.AddWithValue("@PartId", item.PartId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    CartManager.Clear();
                    MessageBox.Show("Order placed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    new CustomerDashboardForm(currentUser).Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Error placing order: {ex.Message}");
                }
            }
        }
    }
}