using CarSparePartsSystem.Data;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;

namespace CarSparePartsSystem.Controllers
{
    public class CustomerController
    {
        private readonly string connectionString = AppConfig.ConnectionString;

        // FR-04 / SFR-05: Browse and search parts with optional sort
        public DataTable GetAvailableParts(string search = "", string category = "",
            bool inStockOnly = false, string sortColumn = "Name", string sortDirection = "ASC")
        {
            var allowedColumns = new HashSet<string> { "Id", "Sku", "Name", "Category", "Price", "StockQuantity" };
            if (!allowedColumns.Contains(sortColumn)) sortColumn = "Name";
            if (sortDirection != "ASC" && sortDirection != "DESC") sortDirection = "ASC";

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT Id, Sku, Name, Category, Price, StockQuantity FROM SpareParts WHERE 1=1";

                if (!string.IsNullOrEmpty(search))   sql += " AND (Name LIKE @Search OR Sku LIKE @Search)";
                if (!string.IsNullOrEmpty(category)) sql += " AND Category = @Category";
                if (inStockOnly)                     sql += " AND StockQuantity > 0";

                sql += $" ORDER BY {sortColumn} {sortDirection};";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(search))   cmd.Parameters.AddWithValue("@Search", $"%{search}%");
                    if (!string.IsNullOrEmpty(category)) cmd.Parameters.AddWithValue("@Category", category);

                    using (var adapter = new MySqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        // SFR-12: Get sorted distinct category list for filter dropdown
        public List<string> GetCategories()
        {
            var cats = new List<string>();
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT DISTINCT Category FROM SpareParts WHERE Category IS NOT NULL AND Category <> '' ORDER BY Category ASC;";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read()) cats.Add(reader["Category"].ToString());
            }
            return cats;
        }

        // SFR-11: Get vehicles compatible with a given part (shown in BrowsePartsForm detail)
        public DataTable GetCompatibleVehicles(int partId)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT Make, Model, YearStart, YearEnd
                               FROM VehicleCompatibility
                               WHERE SparePartId = @PartId
                               ORDER BY Make, Model, YearStart;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@PartId", partId);
                    using (var adapter = new MySqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        // FR-08 / SFR-13: Place a single-item order using a transaction
        public string PlaceOrder(int customerId, int partId, int quantity)
        {
            if (quantity <= 0) return "Quantity must be greater than zero.";

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Lock the row and read price / stock
                        decimal price = 0;
                        int currentStock = 0;
                        string checkSql = "SELECT Price, StockQuantity FROM SpareParts WHERE Id = @Id FOR UPDATE;";
                        using (var cmd = new MySqlCommand(checkSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", partId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.Read()) return "Part not found.";
                                price        = Convert.ToDecimal(reader["Price"]);
                                currentStock = Convert.ToInt32(reader["StockQuantity"]);
                            }
                        }

                        // 2. SFR-17: Prevent negative stock
                        if (currentStock < quantity)
                            return $"Insufficient stock. Only {currentStock} available.";

                        // 3. Create order
                        decimal total = price * quantity;
                        int orderId;
                        string insertOrderSql = "INSERT INTO Orders (CustomerId, Status, TotalAmount) VALUES (@CustomerId, 'New', @Total); SELECT LAST_INSERT_ID();";
                        using (var cmd = new MySqlCommand(insertOrderSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CustomerId", customerId);
                            cmd.Parameters.AddWithValue("@Total", total);
                            orderId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 4. SFR-16: Save unit price at time of order
                        string insertItemSql = "INSERT INTO OrderItems (OrderId, SparePartId, Quantity, UnitPrice) VALUES (@OrderId, @PartId, @Qty, @Price);";
                        using (var cmd = new MySqlCommand(insertItemSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrderId", orderId);
                            cmd.Parameters.AddWithValue("@PartId", partId);
                            cmd.Parameters.AddWithValue("@Qty", quantity);
                            cmd.Parameters.AddWithValue("@Price", price);
                            cmd.ExecuteNonQuery();
                        }

                        // 5. Reduce stock
                        string updateStockSql = "UPDATE SpareParts SET StockQuantity = StockQuantity - @Qty WHERE Id = @Id;";
                        using (var cmd = new MySqlCommand(updateStockSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Qty", quantity);
                            cmd.Parameters.AddWithValue("@Id", partId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return "Success";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return "Order failed: " + ex.Message;
                    }
                }
            }
        }

        // FR-09 / SFR-14: Customer order history with line-item detail (SFR-16)
        public DataTable GetCustomerOrders(int customerId)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT o.Id                   AS 'Order ID',
                           o.OrderDate            AS 'Date',
                           o.Status,
                           o.TotalAmount          AS 'Total ($)',
                           GROUP_CONCAT(
                               CONCAT(p.Name, ' x', oi.Quantity, ' @ $', FORMAT(oi.UnitPrice,2),
                                      ' = $', FORMAT(oi.Quantity * oi.UnitPrice, 2))
                               ORDER BY p.Name SEPARATOR ' | '
                           ) AS 'Items'
                    FROM Orders o
                    JOIN OrderItems oi ON o.Id = oi.OrderId
                    JOIN SpareParts p  ON oi.SparePartId = p.Id
                    WHERE o.CustomerId = @CustomerId
                    GROUP BY o.Id
                    ORDER BY o.OrderDate DESC;";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerId", customerId);
                    using (var adapter = new MySqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        // FR-10 / SFR-15: Customer cancels own order (New or Confirmed only)
        public string CancelOrder(int orderId, int customerId)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string checkSql = "SELECT Status FROM Orders WHERE Id = @Id AND CustomerId = @CustomerId FOR UPDATE;";
                        string status;
                        using (var cmd = new MySqlCommand(checkSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", orderId);
                            cmd.Parameters.AddWithValue("@CustomerId", customerId);
                            object result = cmd.ExecuteScalar();
                            if (result == null) return "Order not found.";
                            status = result.ToString();
                        }

                        if (status == "Completed" || status == "Cancelled")
                            return $"Order cannot be cancelled — it is already {status}.";
                        if (status == "Ready")
                            return "Order is Ready for pickup and can no longer be cancelled.";

                        // Restore stock
                        string restoreSql = @"
                            UPDATE SpareParts sp
                            JOIN OrderItems oi ON sp.Id = oi.SparePartId
                            SET sp.StockQuantity = sp.StockQuantity + oi.Quantity
                            WHERE oi.OrderId = @OrderId;";
                        using (var cmd = new MySqlCommand(restoreSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrderId", orderId);
                            cmd.ExecuteNonQuery();
                        }

                        string updateSql = "UPDATE Orders SET Status = 'Cancelled' WHERE Id = @Id;";
                        using (var cmd = new MySqlCommand(updateSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", orderId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return "Success";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return "Cancellation failed: " + ex.Message;
                    }
                }
            }
        }
    }
}
