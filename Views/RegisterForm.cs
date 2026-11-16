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

        private void RegisterForm_Load(object sender, EventArgs e)
        {

        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // RegisterForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "RegisterForm";
            this.Load += new System.EventHandler(this.RegisterForm_Load);
            this.ResumeLayout(false);

        }

        public RegisterForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Form Settings
            this.Text = "Car Spare Parts - Register";
            this.Size = new Size(450, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Title Label
            lblTitle = new Label()
            {
                Text = "Create Account",
                Location = new Point(140, 20),
                Size = new Size(200, 30),
                Font = new Font("Arial", 14, FontStyle.Bold)
            };

            // Email Label & TextBox
            lblEmail = new Label() { Text = "Email:", Location = new Point(40, 70), Size = new Size(100, 20) };
            txtRegEmail = new TextBox() { Location = new Point(40, 95), Size = new Size(340, 25) };

            // Password Label & TextBox
            lblPassword = new Label() { Text = "Password:", Location = new Point(40, 130), Size = new Size(100, 20) };
            txtRegPassword = new TextBox()
            {
                Location = new Point(40, 155),
                Size = new Size(340, 25),
                PasswordChar = '*'
            };

            // Register Button
            btnRegister = new Button()
            {
                Text = "Register",
                Location = new Point(40, 200),
                Size = new Size(340, 40),
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRegister.Click += btnRegister_Click;

            // Back to Login Button
            btnBackToLogin = new Button()
            {
                Text = "Already have an account? Login",
                Location = new Point(40, 250),
                Size = new Size(340, 35)
            };
            btnBackToLogin.Click += btnBackToLogin_Click;

            // Message Label
            lblMessage = new Label()
            {
                Text = "",
                Location = new Point(40, 300),
                Size = new Size(340, 20),
                ForeColor = Color.Red
            };

            // Add all controls to the Form
            this.Controls.AddRange(new Control[] {
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
                MessageBox.Show("Registration successful! Please login.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoginForm loginForm = new LoginForm();
                loginForm.Show();
                this.Close();
            }
            else
            {
                lblMessage.Text = result;
            }
        }

        private void btnBackToLogin_Click(object sender, EventArgs e)
        {
            LoginForm loginForm = new LoginForm();
            loginForm.Show();
            this.Close();
        }
    }
}