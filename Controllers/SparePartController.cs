using CarSparePartsSystem.Data;
using CarSparePartsSystem.Models;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;

namespace CarSparePartsSystem.Controllers
{
    public class SparePartController
    {
        private readonly string connectionString = AppConfig.ConnectionString;

        // FR-07 / SFR-12: Dashboard stats — includes fitment count and sorted category list
        public Dictionary<string, string> GetDashboardStats()
        {
            var stats = new Dictionary<string, string>();
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Parts + stock stats
                string partsSql = @"
                    SELECT COUNT(*) AS TotalParts,
                           COALESCE(SUM(StockQuantity), 0) AS TotalStock,
                           SUM(CASE WHEN StockQuantity < 5 THEN 1 ELSE 0 END) AS LowStock,
                           COALESCE(SUM(Price * StockQuantity), 0) AS InventoryValue
                    FROM SpareParts;";
                using (var cmd = new MySqlCommand(partsSql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        stats["TotalParts"]      = reader["TotalParts"].ToString();
                        stats["TotalStock"]      = reader["TotalStock"].ToString();
                        stats["LowStock"]        = reader["LowStock"].ToString();
                        stats["InventoryValue"]  = "$" + Convert.ToDecimal(reader["InventoryValue"]).ToString("N2");
                    }
                }

                // SFR-12: Total fitment records
                string fitmentSql = "SELECT COUNT(*) FROM VehicleCompatibility;";
                using (var cmd = new MySqlCommand(fitmentSql, conn))
                    stats["TotalFitments"] = cmd.ExecuteScalar().ToString();

                // SFR-12: Sorted distinct category list (comma-separated)
                string catSql = "SELECT DISTINCT Category FROM SpareParts WHERE Category IS NOT NULL AND Category <> '' ORDER BY Category ASC;";
                using (var cmd = new MySqlCommand(catSql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var cats = new List<string>();
                    while (reader.Read()) cats.Add(reader["Category"].ToString());
                    stats["Categories"] = string.Join(", ", cats);
                }
            }
            return stats;
        }

        // FR-04 / SFR-05: Get all parts with optional search and sort
        public DataTable GetAllParts(string search = "", string sortColumn = "Name", string sortDirection = "ASC")
        {
            // Whitelist columns to prevent SQL injection via sort parameter
            var allowedColumns = new HashSet<string> { "Id", "Sku", "Name", "Category", "Price", "StockQuantity" };
            if (!allowedColumns.Contains(sortColumn)) sortColumn = "Name";
            if (sortDirection != "ASC" && sortDirection != "DESC") sortDirection = "ASC";

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = $@"SELECT Id, Sku, Name, Category, Price, StockQuantity
                                FROM SpareParts
                                WHERE Name LIKE @Search OR Sku LIKE @Search
                                ORDER BY {sortColumn} {sortDirection};";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Search", $"%{search}%");
                    using (var adapter = new MySqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        // SFR-07: Add a new part (unique SKU, case-insensitive)
        public string AddPart(string sku, string name, string category, decimal price, int stock)
        {
            if (string.IsNullOrWhiteSpace(sku))   return "SKU is required.";
            if (string.IsNullOrWhiteSpace(name))  return "Name is required.";
            if (price < 0)                         return "Price cannot be negative.";
            if (stock < 0)                         return "Stock cannot be negative.";

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string checkSql = "SELECT COUNT(*) FROM SpareParts WHERE LOWER(Sku) = LOWER(@Sku);";
                using (var checkCmd = new MySqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Sku", sku);
                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0) return "SKU already exists!";
                }

                string insertSql = "INSERT INTO SpareParts (Sku, Name, Category, Price, StockQuantity) VALUES (@Sku, @Name, @Category, @Price, @Stock);";
                using (var cmd = new MySqlCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Sku", sku.Trim());
                    cmd.Parameters.AddWithValue("@Name", name.Trim());
                    cmd.Parameters.AddWithValue("@Category", category?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.Parameters.AddWithValue("@Stock", stock);
                    cmd.ExecuteNonQuery();
                }
            }
            return "Success";
        }

        // SFR-08: Update a part — allows keeping own SKU; rejects SKU owned by a different part
        public string UpdatePart(int id, string sku, string name, string category, decimal price, int stock)
        {
            if (string.IsNullOrWhiteSpace(sku))   return "SKU is required.";
            if (string.IsNullOrWhiteSpace(name))  return "Name is required.";
            if (price < 0)                         return "Price cannot be negative.";
            if (stock < 0)                         return "Stock cannot be negative.";

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                // Check if another part (different Id) already owns this SKU
                string checkSql = "SELECT COUNT(*) FROM SpareParts WHERE LOWER(Sku) = LOWER(@Sku) AND Id <> @Id;";
                using (var checkCmd = new MySqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Sku", sku);
                    checkCmd.Parameters.AddWithValue("@Id", id);
                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                        return "SKU is already used by another part!";
                }

                string sql = "UPDATE SpareParts SET Sku=@Sku, Name=@Name, Category=@Category, Price=@Price, StockQuantity=@Stock WHERE Id=@Id;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Sku", sku.Trim());
                    cmd.Parameters.AddWithValue("@Name", name.Trim());
                    cmd.Parameters.AddWithValue("@Category", category?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.Parameters.AddWithValue("@Stock", stock);
                    cmd.ExecuteNonQuery();
                }
            }
            return "Success";
        }

        // SFR-09: Delete a part — blocked if referenced by VehicleCompatibility OR OrderItems
        public string DeletePart(int id)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string checkVehicleSql = "SELECT COUNT(*) FROM VehicleCompatibility WHERE SparePartId = @Id;";
                using (var cmd = new MySqlCommand(checkVehicleSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                        return "Cannot delete: part is linked to vehicle compatibility records!";
                }

                string checkOrderSql = "SELECT COUNT(*) FROM OrderItems WHERE SparePartId = @Id;";
                using (var cmd = new MySqlCommand(checkOrderSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                        return "Cannot delete: part is referenced in existing orders!";
                }

                string deleteSql = "DELETE FROM SpareParts WHERE Id = @Id;";
                using (var cmd = new MySqlCommand(deleteSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            return "Success";
        }

        // Helper: get distinct categories sorted alphabetically
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
    }
}
