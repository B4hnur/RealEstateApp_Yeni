using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using RealEstateApp.Services;

namespace RealEstateApp.Forms
{
    public partial class LoginForm : Form
    {
        private readonly AuthService _authService;

        public LoginForm()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            // Set initial focus to username textbox
            txtUsername.Focus();
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            await LoginAsync();
        }

        private async void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await LoginAsync();
            }
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                lblError.Text = "İstifadəçi adını daxil edin.";
                lblError.Visible = true;
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                lblError.Text = "Şifrəni daxil edin.";
                lblError.Visible = true;
                txtPassword.Focus();
                return;
            }

            // Disable controls during login
            SetControlsEnabled(false);
            lblError.Visible = false;

            try
            {
                bool success = await _authService.LoginAsync(txtUsername.Text, txtPassword.Text);

                if (success)
                {
                    // Open main form and hide login form
                    var mainForm = new MainForm();
                    mainForm.Show();
                    this.Hide();
                }
                else
                {
                    lblError.Text = "Yanlış istifadəçi adı və ya şifrə.";
                    lblError.Visible = true;
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                lblError.Text = $"Giriş xətası: {ex.Message}";
                lblError.Visible = true;
            }
            finally
            {
                // Re-enable controls
                SetControlsEnabled(true);
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            txtUsername.Enabled = enabled;
            txtPassword.Enabled = enabled;
            btnLogin.Enabled = enabled;
            lblStatus.Visible = !enabled;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}