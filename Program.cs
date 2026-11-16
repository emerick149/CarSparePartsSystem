using System;
using System.Windows.Forms;
using CarSparePartsSystem.Data;
using CarSparePartsSystem.Views;

namespace CarSparePartsSystem
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Auto-create database tables
            DatabaseInitializer.InitializeDatabase();

            // Start the app with the Login Form
            Application.Run(new LoginForm());
        }
    }
}