using System;
using System.Drawing;
using System.Windows.Forms;
using CarSparePartsSystem.Controllers;

namespace CarSparePartsSystem.Views
{
    public class LoginForm : Form
    {
        private Label lblEmail, lblPassword, lblMessage, lblTitle;
        private TextBox txtEmail, txtPassword;
        private Button btnLogin, btnGoToRegister;

        public LoginForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Car Spare Parts - Login";
            this.Size = new Size(450, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            lblTitle = new Label()
            {
                Text = "Welcome Back!",
                Location = new Point(150, 20),
                Size = new Size(200, 30),
                Font = new Font("Arial", 14, FontStyle.Bold)
            };

            lblEmail = new Label() { Text = "Email:", Location = new Point(40, 70), Size = new Size(100, 20) };
            txtEmail = new TextBox() { Location = new Point(40, 95), Size = new Size(340, 25) };

            lblPassword = new Label() { Text = "Password:", Location = new Point(40, 130), Size = new Size(100, 20) };
            txtPassword = new TextBox()
            {
                Location = new Point(40, 155),
                Size = new Size(340, 25),
                PasswordChar = '*'
            };

            btnLogin = new Button()
            {
                Text = "Login",
                Location = new Point(40, 200),
                Size = new Size(340, 40),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLogin.Click += btnLogin_Click;

            btnGoToRegister = new Button()
            {
                Text = "Need an account? Register Here",
                Location = new Point(40, 250),
                Size = new Size(340, 35)
            };
            btnGoToRegister.Click += btnGoToRegister_Click;

            lblMessage = new Label()
            {
                Text = "",
                Location = new Point(40, 300),
                Size = new Size(340, 20),
                ForeColor = Color.Red
            };

            this.Controls.AddRange(new Control[] {
                lblTitle, lblEmail, txtEmail, lblPassword, txtPassword,
                btnLogin, btnGoToRegister, lblMessage
            });
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            lblMessage.Text = "";
            try
            {
                AuthController auth = new AuthController();
                var user = auth.Login(txtEmail.Text, txtPassword.Text);

                if (user == null)
                {
                    lblMessage.Text = "Invalid email or password.";
                    return;
                }

                Form dashboard;
                if (string.Equals(user.Role?.Trim(), "Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    dashboard = new AdminDashboardForm(user);
                }
                else
                {
                    dashboard = new CustomerDashboardForm(user);
                }

                dashboard.FormClosed += (s, args) => this.Close();
                this.Hide();
                dashboard.Show();
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Unable to open dashboard.";
                MessageBox.Show(ex.Message, "Dashboard Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnGoToRegister_Click(object sender, EventArgs e)
        {
            RegisterForm regForm = new RegisterForm();
            regForm.Show();
            this.Hide();
        }
    }
}