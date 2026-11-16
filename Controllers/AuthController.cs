using CarSparePartsSystem.Models;
using MySqlConnector;
using BCrypt.Net;
using System;
using System.Data;

namespace CarSparePartsSystem.Controllers
{
    public class AuthController
    {
        private readonly string connectionString = "Server=localhost;Database=CarSparePartsDB;User ID=root;Password=;";

        // SFR-01 & SFR-02: Standardize email (trim spaces, make lowercase)
        private string StandardizeEmail(string email)
        {
            return email.Trim().ToLower();
        }

        // FR-01 & SFR-01: Register a new customer
        public string Register(string email, string password)
        {
            email = StandardizeEmail(email);

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return "Email and Password are required.";

            // Check if email already exists
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string checkSql = "SELECT COUNT(*) FROM Users WHERE Email = @Email;";
                using (var cmd = new MySqlCommand(checkSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    if (count > 0) return "Email is already registered.";
                }

                // NFR-04 & SFR-01: Encrypt password and save
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
                string insertSql = "INSERT INTO Users (Email, PasswordHash, Role) VALUES (@Email, @Password, 'Customer');";

                using (var cmd = new MySqlCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    cmd.ExecuteNonQuery();
                }
            }
            return "Success";
        }

        // FR-02 & SFR-02: Login
        public User Login(string email, string password)
        {
            email = StandardizeEmail(email);
            User user = null;

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT Id, Email, PasswordHash, Role FROM Users WHERE Email = @Email;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash = reader["PasswordHash"].ToString();

                            // NFR-04: Verify encrypted password
                            if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                            {
                                user = new User
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Email = reader["Email"].ToString(),
                                    Role = reader["Role"].ToString()
                                };
                            }
                        }
                    }
                }
            }
            return user; // Returns null if login failed
        }
    }
}