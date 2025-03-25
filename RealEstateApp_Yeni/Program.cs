using System;
using System.IO;
using System.Windows.Forms;
using RealEstateApp.Forms;
using RealEstateApp.Services;
using System.Threading;
using System.Globalization;

namespace RealEstateApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Set app culture to Azerbaijani
            Thread.CurrentThread.CurrentCulture = new CultureInfo("az-AZ");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("az-AZ");

            // Set DataDirectory for SQLite connection string
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string dataDirectory = Path.Combine(baseDirectory, "Data");

            // Create Data directory if it doesn't exist
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }

            AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);

            // Create other required directories
            string imagesFolder = System.Configuration.ConfigurationManager.AppSettings["ImagesFolder"] ?? "Images";
            string imageBackupFolder = System.Configuration.ConfigurationManager.AppSettings["ImageBackupFolder"] ?? "ImageBackup";
            string reportsFolder = System.Configuration.ConfigurationManager.AppSettings["ReportsFolder"] ?? "Reports";
            string logFolder = System.Configuration.ConfigurationManager.AppSettings["LogFolder"] ?? "Logs";

            CreateDirectoryIfNotExists(Path.Combine(baseDirectory, imagesFolder));
            CreateDirectoryIfNotExists(Path.Combine(baseDirectory, imageBackupFolder));
            CreateDirectoryIfNotExists(Path.Combine(baseDirectory, reportsFolder));
            CreateDirectoryIfNotExists(Path.Combine(baseDirectory, logFolder));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Initialize the database
                DatabaseService.InitializeDatabase();

                // Start the application with the login form
                Application.Run(new LoginForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Proqram başlayarkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Log the error
                string logFilePath = Path.Combine(baseDirectory, logFolder, "error_log.txt");
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"[{DateTime.Now}] Error: {ex.Message}");
                    writer.WriteLine($"StackTrace: {ex.StackTrace}");
                    writer.WriteLine(new string('-', 80));
                }
            }
        }

        private static void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}