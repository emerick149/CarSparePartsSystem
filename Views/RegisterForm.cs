using System;
using System.Drawing;
using System.Windows.Forms;
using CarSparePartsSystem.Controllers;

namespace CarSparePartsSystem.Views
{
    public class RegisterForm : Form
    {
        private Label lblEmail, lblPassword, lblMessage, lblTitle;
        private TextBox txtRegEmail, txtRegPassword;
        private Button btnRegister, btnBackToLogin;

        public RegisterForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Car Spare Parts - Register";
            this.Size = new Size(450, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            lblTitle = new Label()
            {
                Text = "Create Account",
                Location = new Point(140, 20), Size = new Size(200, 30),
                Font = new Font("Arial", 14, FontStyle.Bold)
            };

            lblEmail = new Label() { Text = "Email:", Location = new Point(40, 70), Size = new Size(100, 20) };
            txtRegEmail = new TextBox() { Location = new Point(40, 95), Size = new Size(340, 25) };

            lblPassword = new Label() { Text = "Password (max 72 chars):", Location = new Point(40, 130), Size = new Size(200, 20) };
            txtRegPassword = new TextBox()
            {
                Location = new Point(40, 155), Size = new Size(340, 25),
                PasswordChar = '*',
                MaxLength = 72   // SFR-01: enforce at the UI level too
            };

            btnRegister = new Button()
            {
                Text = "Register",
                Location = new Point(40, 200), Size = new Size(340, 40),
                BackColor = Color.Green, ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            btnRegister.Click += btnRegister_Click;

            btnBackToLogin = new Button()
            {
                Text = "Already have an account? Login",
                Location = new Point(40, 255), Size = new Size(340, 35)
            };
            btnBackToLogin.Click += btnBackToLogin_Click;

            lblMessage = new Label()
            {
                Text = "", Location = new Point(40, 305), Size = new Size(340, 40),
                ForeColor = Color.Red
            };

            this.Controls.AddRange(new Control[]
            {
                lblTitle, lblEmail, txtRegEmail, lblPassword, txtRegPassword,
                btnRegister, btnBackToLogin, lblMessage
            });
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            lblMessage.Text = "";
            AuthController auth = new AuthController();
            string result = auth.Register(txtRegEmail.Text, txtRegPassword.Text);

            if (result == "Success")
            {
                MessageBox.Show("Registration successful! Please login.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                new LoginForm().Show();
                this.Close();
            }
            else
            {
                lblMessage.Text = result;
            }
        }

        private void btnBackToLogin_Click(object sender, EventArgs e)
        {
            new LoginForm().Show();
            this.Close();
        }
    }
}
