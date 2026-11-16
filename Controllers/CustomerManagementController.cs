using CarSparePartsSystem.Data;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;

namespace CarSparePartsSystem.Controllers
{
    /// <summary>
    /// FR-11: Admin customer management — filterable list, per-customer order counts.
    /// </summary>
    public class CustomerManagementController
    {
        private readonly string connectionString = AppConfig.ConnectionString;

        // FR-11: Customer list with order statistics, optionally filtered by email
        public DataTable GetCustomerList(string search = "")
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT u.Id,
                           u.Email,
                           COUNT(o.Id)                                           AS 'Total Orders',
                           SUM(CASE WHEN o.Status = 'New'       THEN 1 ELSE 0 END) AS 'New',
                           SUM(CASE WHEN o.Status = 'Confirmed' THEN 1 ELSE 0 END) AS 'Confirmed',
                           SUM(CASE WHEN o.Status = 'Ready'     THEN 1 ELSE 0 END) AS 'Ready',
                           SUM(CASE WHEN o.Status = 'Completed' THEN 1 ELSE 0 END) AS 'Completed',
                           SUM(CASE WHEN o.Status = 'Cancelled' THEN 1 ELSE 0 END) AS 'Cancelled',
                           COALESCE(SUM(o.TotalAmount), 0)                       AS 'Lifetime Spend ($)'
                    FROM Users u
                    LEFT JOIN Orders o ON u.Id = o.CustomerId
                    WHERE u.Role = 'Customer'
                      AND (@Search = '' OR u.Email LIKE @SearchLike)
                    GROUP BY u.Id
                    ORDER BY u.Email;";

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

        // FR-11: Summary stats shown at top of CustomerManagementForm
        public Dictionary<string, string> GetCustomerStats()
        {
            var stats = new Dictionary<string, string>();
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT COUNT(DISTINCT u.Id)                                     AS TotalCustomers,
                           COUNT(o.Id)                                               AS TotalOrders,
                           SUM(CASE WHEN o.Status = 'New' THEN 1 ELSE 0 END)        AS NewOrders
                    FROM Users u
                    LEFT JOIN Orders o ON u.Id = o.CustomerId
                    WHERE u.Role = 'Customer';";

                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        stats["TotalCustomers"] = reader["TotalCustomers"].ToString();
                        stats["TotalOrders"]    = reader["TotalOrders"].ToString();
                        stats["NewOrders"]      = reader["NewOrders"].ToString();
                    }
                }
            }
            return stats;
        }
    }
}
