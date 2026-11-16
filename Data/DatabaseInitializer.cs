using MySqlConnector;
using System;
using BCrypt.Net; // Needed to encrypt the admin password

namespace CarSparePartsSystem.Data
{
    public class DatabaseInitializer
    {
        // XAMPP MySQL Connection Strings
        private static readonly string serverConnectionString = "Server=localhost;User ID=root;Password=;";
        private static readonly string connectionString = AppConfig.ConnectionString;

        public static void InitializeDatabase()
        {
            // 1. Create the database itself if it was deleted
            using (var serverConn = new MySqlConnection(serverConnectionString))
            {
                serverConn.Open();
                ExecuteSql(serverConn, "CREATE DATABASE IF NOT EXISTS CarSparePartsDB;");
            }

            // 2. Create the tables
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                ExecuteSql(conn, @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Email VARCHAR(100) NOT NULL UNIQUE,
                        PasswordHash VARCHAR(255) NOT NULL,
                        Role VARCHAR(20) NOT NULL
                    );");

                ExecuteSql(conn, @"
                    CREATE TABLE IF NOT EXISTS SpareParts (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Sku VARCHAR(50) NOT NULL UNIQUE,
                        Name VARCHAR(100) NOT NULL,
                        Category VARCHAR(50),
                        Price DECIMAL(18,2) NOT NULL,
                        StockQuantity INT NOT NULL DEFAULT 0
                    );");

                ExecuteSql(conn, @"
                    CREATE TABLE IF NOT EXISTS VehicleCompatibility (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        SparePartId INT NOT NULL,
                        Make VARCHAR(50) NOT NULL,
                        Model VARCHAR(50) NOT NULL,
                        YearStart INT NOT NULL,
                        YearEnd INT NOT NULL,
                        FOREIGN KEY (SparePartId) REFERENCES SpareParts(Id) ON DELETE CASCADE
                    );");

                ExecuteSql(conn, @"
                    CREATE TABLE IF NOT EXISTS Orders (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        CustomerId INT NOT NULL,
                        OrderDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        Status VARCHAR(20) NOT NULL DEFAULT 'New',
                        TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                        FOREIGN KEY (CustomerId) REFERENCES Users(Id)
                    );");

                ExecuteSql(conn, @"
                    CREATE TABLE IF NOT EXISTS OrderItems (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        OrderId INT NOT NULL,
                        SparePartId INT NOT NULL,
                        Quantity INT NOT NULL,
                        UnitPrice DECIMAL(18,2) NOT NULL,
                        FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
                        FOREIGN KEY (SparePartId) REFERENCES SpareParts(Id)
                    );");

                // 3. Seed Data (Admin & Parts)
                SeedDefaultAdmin(conn);
                SeedDefaultSpareParts(conn);
            }
        }

        private static void ExecuteSql(MySqlConnection conn, string sql)
        {
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void SeedDefaultAdmin(MySqlConnection conn)
        {
            string checkSql = "SELECT COUNT(*) FROM Users WHERE Role = 'Administrator';";
            using (var cmd = new MySqlCommand(checkSql, conn))
            {
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                {
                    // FIX: Now properly encrypts the admin password using BCrypt!
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword("admin123");
                    string insertSql = "INSERT INTO Users (Email, PasswordHash, Role) VALUES ('admin@carparts.com', @Hash, 'Administrator');";

                    using (var insertCmd = new MySqlCommand(insertSql, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@Hash", hashedPassword);
                        insertCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // Repair the legacy plaintext seed without changing valid BCrypt hashes.
                    string repairSql = @"
                        UPDATE Users
                        SET PasswordHash = @Hash
                        WHERE Role = 'Administrator' AND PasswordHash = 'admin123';";

                    using (var repairCmd = new MySqlCommand(repairSql, conn))
                    {
                        repairCmd.Parameters.AddWithValue("@Hash", BCrypt.Net.BCrypt.HashPassword("admin123"));
                        repairCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void SeedDefaultSpareParts(MySqlConnection conn)
        {
            string checkSql = "SELECT COUNT(*) FROM SpareParts;";
            using (var cmd = new MySqlCommand(checkSql, conn))
            {
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                {
                    // Automatically adds 12 realistic sample parts
                    string insertSql = @"
                        INSERT INTO SpareParts (Sku, Name, Category, Price, StockQuantity) VALUES 
                        ('BRK-001', 'Ceramic Brake Pads (Front)', 'Brakes', 45.99, 50),
                        ('BRK-002', 'Brake Rotor (Pair)', 'Brakes', 89.50, 20),
                        ('ENG-101', 'Spark Plug (Iridium)', 'Engine', 12.99, 100),
                        ('ENG-102', 'Timing Belt Kit', 'Engine', 125.00, 15),
                        ('FLT-201', 'Oil Filter', 'Filters', 8.50, 200),
                        ('FLT-202', 'Engine Air Filter', 'Filters', 15.00, 80),
                        ('FLT-203', 'Cabin Air Filter', 'Filters', 18.75, 60),
                        ('SUS-301', 'Shock Absorber (Front)', 'Suspension', 65.00, 30),
                        ('SUS-302', 'Control Arm Assembly', 'Suspension', 95.00, 4),
                        ('ELC-401', 'Alternator', 'Electrical', 150.00, 10),
                        ('ELC-402', 'Starter Motor', 'Electrical', 135.00, 8),
                        ('ENG-103', 'Serpentine Belt', 'Engine', 25.50, 45);
                    ";
                    using (var insertCmd = new MySqlCommand(insertSql, conn))
                    {
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}