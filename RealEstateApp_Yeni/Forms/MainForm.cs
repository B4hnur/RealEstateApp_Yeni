using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RealEstateApp.Models;
using RealEstateApp.Services;
using RealEstateApp_Yeni.Forms;

namespace RealEstateApp.Forms
{
    public partial class MainForm : Form
    {
        private readonly AuthService _authService;
        private readonly PropertyService _propertyService;
        private readonly ImageService _imageService;
        private readonly ReportingService _reportingService;

        private Form _activeForm = null;

        public MainForm()
        {
            InitializeComponent();

            // Initialize services
            _authService = new AuthService();
            _propertyService = new PropertyService();
            _imageService = new ImageService();
            _reportingService = new ReportingService();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Set user info
            if (AuthService.CurrentUser != null)
            {
                lblUserName.Text = AuthService.CurrentUser.FullName;
                lblUserRole.Text = AuthService.CurrentUser.RoleDisplay;

                // Set permissions based on role
                SetupMenu();
            }
            else
            {
                // No user is logged in, return to login form
                MessageBox.Show("Sessiya məlumatları tapılmadı. Zəhmət olmasa yenidən daxil olun.",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);

                this.Close();
                Application.Restart();
            }

            // Load dashboard by default
            OpenChildForm(new DashboardForm());
        }

        private void SetupMenu()
        {
            // Setup menu based on user role
            bool isAdmin = AuthService.CurrentUser.Role == "Admin";
            bool isManager = AuthService.CurrentUser.Role == "Manager" || isAdmin;
            bool isAccountant = AuthService.CurrentUser.Role == "Accountant" || isAdmin;

            // Admin menu items
            btnUsers.Visible = isAdmin;
            btnSettings.Visible = isAdmin;

            // Manager menu items
            btnEmployees.Visible = isManager;

            // Accountant menu items
            btnReports.Visible = isAccountant || isManager;
            btnFinancial.Visible = isAccountant || isManager;
        }

        private void OpenChildForm(Form childForm)
        {
            if (_activeForm != null)
            {
                _activeForm.Close();
            }

            _activeForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;

            pnlContent.Controls.Clear();
            pnlContent.Controls.Add(childForm);

            childForm.BringToFront();
            childForm.Show();

            lblTitle.Text = childForm.Text;
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            if (_activeForm is DashboardForm)
                return;

            OpenChildForm(new DashboardForm());
            SetActiveButton(btnDashboard);
        }

        private void btnProperties_Click(object sender, EventArgs e)
        {
            if (_activeForm is PropertyListForm)
                return;

            OpenChildForm(new PropertyListForm());
            SetActiveButton(btnProperties);
        }

        private void btnRental_Click(object sender, EventArgs e)
        {
            if (_activeForm is RentalAgreementListForm)
                return;

            OpenChildForm(new RentalAgreementListForm());
            SetActiveButton(btnRental);
        }

        private void btnSales_Click(object sender, EventArgs e)
        {
            if (_activeForm is SaleAgreementListForm)
                return;

            OpenChildForm(new SaleAgreementListForm());
            SetActiveButton(btnSales);
        }

        private void btnEmployees_Click(object sender, EventArgs e)
        {
            if (_activeForm is EmployeeListForm)
                return;

            OpenChildForm(new EmployeeListForm());
            SetActiveButton(btnEmployees);
        }

        private void btnFinancial_Click(object sender, EventArgs e)
        {
            if (_activeForm is FinancialTransactionsForm)
                return;

            OpenChildForm(new FinancialTransactionsForm());
            SetActiveButton(btnFinancial);
        }

        private void btnReports_Click(object sender, EventArgs e)
        {
            if (_activeForm is ReportsForm)
                return;

            OpenChildForm(new ReportsForm());
            SetActiveButton(btnReports);
        }

        private void btnUsers_Click(object sender, EventArgs e)
        {
            if (_activeForm is UserListForm)
                return;

            OpenChildForm(new UserListForm());
            SetActiveButton(btnUsers);
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            if (_activeForm is SettingsForm)
                return;

            OpenChildForm(new SettingsForm());
            SetActiveButton(btnSettings);
        }

        private void SetActiveButton(Button button)
        {
            // Reset all buttons
            foreach (Control ctrl in pnlMenu.Controls)
            {
                if (ctrl is Button btn)
                {
                    btn.BackColor = Color.FromArgb(0, 71, 160);
                    btn.ForeColor = Color.White;
                }
            }

            // Set active button
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(0, 71, 160);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Sistemdən çıxmaq istədiyinizə əminsiniz?",
                "Çıxış", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _authService.Logout();

                this.Hide();
                LoginForm loginForm = new LoginForm();
                loginForm.Show();
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Proqramdan çıxmaq istədiyinizə əminsiniz?",
                "Çıxış", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.WindowState = FormWindowState.Maximized;
                btnMaximize.Text = "1";
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                btnMaximize.Text = "2";
            }
        }

        private void lblTitle_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Release capture
                ReleaseCapture();

                // Send command to allow form dragging
                SendMessage(this.Handle, 0x112, 0xf012, 0);
            }
        }

        // Import WinAPI function for form dragging
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private void timerDateTime_Tick(object sender, EventArgs e)
        {
            // Update time and date
            lblTime.Text = DateTime.Now.ToString("HH:mm:ss");
            lblDate.Text = DateTime.Now.ToString("dd MMMM yyyy");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Application.OpenForms.Count == 1 && e.CloseReason == CloseReason.UserClosing)
            {
                // If this is the last form and user is closing it, show login form
                if (AuthService.CurrentUser != null)
                {
                    _authService.Logout();
                    LoginForm loginForm = new LoginForm();
                    loginForm.Show();
                }
            }
        }
    }
}