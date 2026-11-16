using System;
using System.Drawing;
using System.Windows.Forms;
using CarSparePartsSystem.Controllers;

namespace CarSparePartsSystem.Views
{
    // Notice we removed the "partial" keyword since we deleted the designer file
    public class LoginForm : Form
    {
        // 1. Declare all UI controls
        private Label lblEmail, lblPassword, lblMessage, lblTitle;
        private TextBox txtEmail, txtPassword;
        private Button btnLogin, btnGoToRegister;

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // LoginForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "LoginForm";
            this.Load += new System.EventHandler(this.LoginForm_Load);
            this.ResumeLayout(false);

        }

        public LoginForm()
        {
            InitializeComponents();
        }

        // 2. Create and arrange the UI controls
        private void InitializeComponents()
        {
            // Form Settings
            this.Text = "Car Spare Parts - Login";
            this.Size = new Size(450, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Title Label
            lblTitle = new Label()
            {
                Text = "Welcome Back!",
                Location = new Point(150, 20),
                Size = new Size(200, 30),
                Font = new Font("Arial", 14, FontStyle.Bold)
            };

            // Email Label & TextBox
            lblEmail = new Label() { Text = "Email:", Location = new Point(40, 70), Size = new Size(100, 20) };
            txtEmail = new TextBox() { Location = new Point(40, 95), Size = new Size(340, 25) };

            // Password Label & TextBox
            lblPassword = new Label() { Text = "Password:", Location = new Point(40, 130), Size = new Size(100, 20) };
            txtPassword = new TextBox()
            {
                Location = new Point(40, 155),
                Size = new Size(340, 25),
                PasswordChar = '*' // Hides password with dots
            };

            // Login Button
            btnLogin = new Button()
            {
                Text = "Login",
                Location = new Point(40, 200),
                Size = new Size(340, 40),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLogin.Click += btnLogin_Click; // Attach the click event

            // Register Button
            btnGoToRegister = new Button()
            {
                Text = "Need an account? Register Here",
                Location = new Point(40, 250),
                Size = new Size(340, 35)
            };
            btnGoToRegister.Click += btnGoToRegister_Click;

            // Message Label (for errors)
            lblMessage = new Label()
            {
                Text = "",
                Location = new Point(40, 300),
                Size = new Size(340, 20),
                ForeColor = Color.Red
            };

            // Add all controls to the Form
            this.Controls.AddRange(new Control[] {
                lblTitle, lblEmail, txtEmail, lblPassword, txtPassword,
                btnLogin, btnGoToRegister, lblMessage
            });
        }

        // 3. Button Click Events (The Logic)
        private void btnLogin_Click(object sender, EventArgs e)
        {
            lblMessage.Text = ""; // Clear previous errors
            AuthController auth = new AuthController();
            var user = auth.Login(txtEmail.Text, txtPassword.Text);

            if (user != null)
            {
                MessageBox.Show($"Login Successful!\nWelcome, {user.Email}\nRole: {user.Role}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // TODO: In the next step, we will open the Dashboard here
                // this.Hide();
                // DashboardForm dashboard = new DashboardForm(user);
                // dashboard.Show();
            }
            else
            {
                lblMessage.Text = "Invalid email or password.";
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