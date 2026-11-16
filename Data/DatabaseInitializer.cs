using MySqlConnector;
using System;

namespace CarSparePartsSystem.Data
{
    public class DatabaseInitializer
    {
        // XAMPP MySQL Connection String
        private static readonly string connectionString = "Server=localhost;Database=CarSparePartsDB;User ID=root;Password=;";

        public static void InitializeDatabase()
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // 1. Create Users Table (FR-01, FR-02, FR-03)
                string createUsersTable = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Email VARCHAR(100) NOT NULL UNIQUE,
                        PasswordHash VARCHAR(255) NOT NULL,
                        Role VARCHAR(20) NOT NULL
                    );";
                ExecuteSql(conn, createUsersTable);

                // 2. Create SpareParts Table (FR-04, FR-05, FR-07)
                string createPartsTable = @"
                    CREATE TABLE IF NOT EXISTS SpareParts (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Sku VARCHAR(50) NOT NULL UNIQUE,
                        Name VARCHAR(100) NOT NULL,
                        Category VARCHAR(50),
                        Price DECIMAL(18,2) NOT NULL,
                        StockQuantity INT NOT NULL DEFAULT 0
                    );";
                ExecuteSql(conn, createPartsTable);

                // 3. Create VehicleCompatibility Table (FR-06)
                string createCompatTable = @"
                    CREATE TABLE IF NOT EXISTS VehicleCompatibility (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        SparePartId INT NOT NULL,
                        Make VARCHAR(50) NOT NULL,
                        Model VARCHAR(50) NOT NULL,
                        YearStart INT NOT NULL,
                        YearEnd INT NOT NULL,
                        FOREIGN KEY (SparePartId) REFERENCES SpareParts(Id) ON DELETE CASCADE
                    );";
                ExecuteSql(conn, createCompatTable);

                // 4. Create Orders Table (FR-08, FR-09, FR-10)
                string createOrdersTable = @"
                    CREATE TABLE IF NOT EXISTS Orders (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        CustomerId INT NOT NULL,
                        OrderDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        Status VARCHAR(20) NOT NULL DEFAULT 'New',
                        TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                        FOREIGN KEY (CustomerId) REFERENCES Users(Id)
                    );";
                ExecuteSql(conn, createOrdersTable);

                // 5. Create OrderItems Table (FR-08, FR-16)
                string createOrderItemsTable = @"
                    CREATE TABLE IF NOT EXISTS OrderItems (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        OrderId INT NOT NULL,
                        SparePartId INT NOT NULL,
                        Quantity INT NOT NULL,
                        UnitPrice DECIMAL(18,2) NOT NULL,
                        FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
                        FOREIGN KEY (SparePartId) REFERENCES SpareParts(Id)
                    );";
                ExecuteSql(conn, createOrderItemsTable);

                // 6. Seed Default Administrator (Optional but highly recommended for testing)
                // Since public registration only creates Customers (SFR-01), we need an Admin to manage the system.
                SeedDefaultAdmin(conn);
            }
        }

        // Helper method to run the SQL commands
        private static void ExecuteSql(MySqlConnection conn, string sql)
        {
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        // Helper method to create a default Admin if the table is empty
        private static void SeedDefaultAdmin(MySqlConnection conn)
        {
            // Check if any users exist
            string checkUsers = "SELECT COUNT(*) FROM Users;";
            using (var cmd = new MySqlCommand(checkUsers, conn))
            {
                int count = Convert.ToInt32(cmd.ExecuteScalar());

                // If no users exist, create a default Admin
                if (count == 0)
                {
                    // Note: In a real app, this password would be hashed using BCrypt (NFR-04).
                    // For now, we insert a plain text placeholder. We will handle hashing in the Controller later.
                    string insertAdmin = "INSERT INTO Users (Email, PasswordHash, Role) VALUES ('admin@carparts.com', 'admin123', 'Administrator');";
                    using (var insertCmd = new MySqlCommand(insertAdmin, conn))
                    {
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}