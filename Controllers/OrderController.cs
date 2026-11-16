using CarSparePartsSystem.Data;
using MySqlConnector;
using System;
using System.Data;

namespace CarSparePartsSystem.Controllers
{
    /// <summary>
    /// FR-09 (admin side) / FR-10 / SFR-15 / SFR-20: Admin order management.
    /// Status transitions: New→Confirmed|Cancelled, Confirmed→Ready|Cancelled,
    ///                     Ready→Completed|Cancelled. Completed and Cancelled are terminal.
    /// Stock is restored exactly once when any order is cancelled.
    /// </summary>
    public class OrderController
    {
        private readonly string connectionString = AppConfig.ConnectionString;

        // FR-09: Grid of ALL orders with customer email and item summary
        public DataTable GetAllOrders(string search = "")
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT o.Id            AS 'Order ID',
                           u.Email         AS 'Customer',
                           o.OrderDate     AS 'Date',
                           o.Status,
                           o.TotalAmount   AS 'Total ($)',
                           GROUP_CONCAT(
                               CONCAT(p.Name, ' x', oi.Quantity)
                               ORDER BY p.Name SEPARATOR ', '
                           ) AS 'Items'
                    FROM Orders o
                    JOIN Users u          ON o.CustomerId = u.Id
                    JOIN OrderItems oi    ON o.Id = oi.OrderId
                    JOIN SpareParts p     ON oi.SparePartId = p.Id
                    WHERE @Search = ''
                       OR u.Email  LIKE @SearchLike
                       OR o.Status LIKE @SearchLike
                       OR CAST(o.Id AS CHAR) LIKE @SearchLike
                    GROUP BY o.Id
                    ORDER BY o.OrderDate DESC;";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Search",     search ?? "");
                    cmd.Parameters.AddWithValue("@SearchLike", $"%{search}%");
                    using (var adapter = new MySqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        // SFR-20: Advance order status (admin).
        // Returns "Success" or an error message.
        public string UpdateOrderStatus(int orderId, string newStatus)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Read current status with a lock
                        string readSql = "SELECT Status FROM Orders WHERE Id = @Id FOR UPDATE;";
                        string currentStatus;
                        using (var cmd = new MySqlCommand(readSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", orderId);
                            object result = cmd.ExecuteScalar();
                            if (result == null) return "Order not found.";
                            currentStatus = result.ToString();
                        }

                        // Reject same-status change
                        if (currentStatus == newStatus)
                            return $"Order is already {currentStatus}.";

                        // Validate transition per SFR-20
                        string error = ValidateTransition(currentStatus, newStatus);
                        if (error != null) return error;

                        // If cancelling, restore stock exactly once
                        if (newStatus == "Cancelled")
                        {
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
                        }

                        string updateSql = "UPDATE Orders SET Status = @Status WHERE Id = @Id;";
                        using (var cmd = new MySqlCommand(updateSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Status", newStatus);
                            cmd.Parameters.AddWithValue("@Id", orderId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return "Success";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return "Update failed: " + ex.Message;
                    }
                }
            }
        }

        // Returns null if transition is valid, or an error message if not.
        private string ValidateTransition(string from, string to)
        {
            switch (from)
            {
                case "New":
                    if (to == "Confirmed" || to == "Cancelled") return null;
                    return $"Cannot change from New to {to}. Allowed: Confirmed, Cancelled.";

                case "Confirmed":
                    if (to == "Ready" || to == "Cancelled") return null;
                    return $"Cannot change from Confirmed to {to}. Allowed: Ready, Cancelled.";

                case "Ready":
                    if (to == "Completed" || to == "Cancelled") return null;
                    return $"Cannot change from Ready to {to}. Allowed: Completed, Cancelled.";

                case "Completed":
                    return "Order is Completed and cannot be changed.";

                case "Cancelled":
                    return "Order is Cancelled and cannot be changed.";

                default:
                    return $"Unknown status '{from}'.";
            }
        }
    }
}
