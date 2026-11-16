using CarSparePartsSystem.Data;
using MySqlConnector;
using System;
using System.Data;

namespace CarSparePartsSystem.Controllers
{
    /// <summary>
    /// FR-06 / SFR-10 / SFR-11: Vehicle compatibility management and search.
    /// </summary>
    public class VehicleCompatibilityController
    {
        private readonly string connectionString = AppConfig.ConnectionString;

        // SFR-10: Get all fitment records for a given part
        public DataTable GetFitments(int partId)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT Id, Make, Model, YearStart, YearEnd
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

        // SFR-10: Add a fitment record. Validates YearStart ≤ YearEnd; rejects unknown part.
        public string AddFitment(int partId, string make, string model, int yearStart, int yearEnd)
        {
            if (string.IsNullOrWhiteSpace(make))  return "Make is required.";
            if (string.IsNullOrWhiteSpace(model)) return "Model is required.";
            if (yearStart <= 0)                   return "Year Start must be a valid year.";
            if (yearEnd <= 0)                     return "Year End must be a valid year.";
            if (yearStart > yearEnd)              return "Year Start must be less than or equal to Year End.";

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Verify the part exists
                string checkPartSql = "SELECT COUNT(*) FROM SpareParts WHERE Id = @PartId;";
                using (var cmd = new MySqlCommand(checkPartSql, conn))
                {
                    cmd.Parameters.AddWithValue("@PartId", partId);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        return "Part not found.";
                }

                // Prevent exact duplicate fitment
                string checkDupSql = @"SELECT COUNT(*) FROM VehicleCompatibility
                                       WHERE SparePartId = @PartId
                                         AND LOWER(Make) = LOWER(@Make)
                                         AND LOWER(Model) = LOWER(@Model)
                                         AND YearStart = @YearStart
                                         AND YearEnd = @YearEnd;";
                using (var cmd = new MySqlCommand(checkDupSql, conn))
                {
                    cmd.Parameters.AddWithValue("@PartId", partId);
                    cmd.Parameters.AddWithValue("@Make", make);
                    cmd.Parameters.AddWithValue("@Model", model);
                    cmd.Parameters.AddWithValue("@YearStart", yearStart);
                    cmd.Parameters.AddWithValue("@YearEnd", yearEnd);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                        return "This fitment record already exists.";
                }

                string insertSql = @"INSERT INTO VehicleCompatibility (SparePartId, Make, Model, YearStart, YearEnd)
                                     VALUES (@PartId, @Make, @Model, @YearStart, @YearEnd);";
                using (var cmd = new MySqlCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@PartId", partId);
                    cmd.Parameters.AddWithValue("@Make", make.Trim());
                    cmd.Parameters.AddWithValue("@Model", model.Trim());
                    cmd.Parameters.AddWithValue("@YearStart", yearStart);
                    cmd.Parameters.AddWithValue("@YearEnd", yearEnd);
                    cmd.ExecuteNonQuery();
                }
            }
            return "Success";
        }

        // SFR-10: Delete a fitment record by its own Id
        public string DeleteFitment(int fitmentId)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM VehicleCompatibility WHERE Id = @Id;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", fitmentId);
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0 ? "Success" : "Fitment record not found.";
                }
            }
        }

        // SFR-11: Search parts compatible with a given make, model, and year
        public DataTable SearchCompatibleParts(string make, string model, int year)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT sp.Id, sp.Sku, sp.Name, sp.Category, sp.Price, sp.StockQuantity,
                           vc.Make, vc.Model, vc.YearStart, vc.YearEnd
                    FROM SpareParts sp
                    JOIN VehicleCompatibility vc ON sp.Id = vc.SparePartId
                    WHERE (@Make  = '' OR LOWER(vc.Make)  LIKE LOWER(@MakeLike))
                      AND (@Model = '' OR LOWER(vc.Model) LIKE LOWER(@ModelLike))
                      AND (@Year  = 0  OR (@Year BETWEEN vc.YearStart AND vc.YearEnd))
                    ORDER BY sp.Name;";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Make",      make  ?? "");
                    cmd.Parameters.AddWithValue("@MakeLike",  $"%{make}%");
                    cmd.Parameters.AddWithValue("@Model",     model ?? "");
                    cmd.Parameters.AddWithValue("@ModelLike", $"%{model}%");
                    cmd.Parameters.AddWithValue("@Year",      year);

                    using (var adapter = new MySqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }
    }
}
