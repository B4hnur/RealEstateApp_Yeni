using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Configuration;
using Dapper;
using System.Linq;
using RealEstateApp.Models;
using System.Windows.Forms;

namespace RealEstateApp.Services
{
    public static class DatabaseService
    {
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        /// <summary>
        /// Verilənlər bazasını ilkin qurur
        /// </summary>
        public static void InitializeDatabase()
        {
            try
            {
                string dataDir = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
                string dbFilePath = Path.Combine(dataDir, "RealEstateApp.db");

                bool createTables = !File.Exists(dbFilePath);

                if (createTables)
                {
                    // Ensure the directory exists
                    Directory.CreateDirectory(dataDir);

                    // Create the database file
                    SQLiteConnection.CreateFile(dbFilePath);

                    // Create tables
                    CreateTables();

                    // Add initial data
                    SeedInitialData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Verilənlər bazası yaradılarkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        /// <summary>
        /// Verilənlər bazasını yedəkləyir
        /// </summary>
        public static bool BackupDatabase()
        {
            try
            {
                string dataDir = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
                string dbFilePath = Path.Combine(dataDir, "RealEstateApp.db");
                string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFilePath = Path.Combine(backupDir, $"RealEstateApp_Backup_{timestamp}.db");

                File.Copy(dbFilePath, backupFilePath, true);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Verilənlər bazası yedəklənərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Verilənlər bazasında cədvəlləri yaradır
        /// </summary>
        private static void CreateTables()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                // Create tables with SQL
                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL UNIQUE,
                        PasswordHash TEXT NOT NULL,
                        FirstName TEXT,
                        LastName TEXT,
                        Email TEXT,
                        Phone TEXT,
                        EmployeeId INTEGER,
                        Role TEXT NOT NULL,
                        LastLogin TEXT,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        IsLocked INTEGER NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        Notes TEXT,
                        FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
                    );
                ");

                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS Employees (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FirstName TEXT NOT NULL,
                        LastName TEXT NOT NULL,
                        Phone TEXT,
                        Email TEXT,
                        Position TEXT NOT NULL,
                        BaseSalary REAL NOT NULL,
                        CommissionRate REAL NOT NULL,
                        HireDate TEXT NOT NULL,
                        TerminationDate TEXT,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                ");

                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS Properties (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Description TEXT,
                        Price REAL NOT NULL,
                        Currency TEXT NOT NULL,
                        Address TEXT NOT NULL,
                        City TEXT,
                        District TEXT,
                        Bedrooms INTEGER,
                        Bathrooms INTEGER,
                        Area INTEGER,
                        YearBuilt INTEGER,
                        Floor INTEGER,
                        TotalFloors INTEGER,
                        PropertyType TEXT NOT NULL,
                        ListingType TEXT NOT NULL,
                        OwnerName TEXT,
                        OwnerPhone TEXT,
                        OwnerEmail TEXT,
                        OwnershipType TEXT,
                        CommissionRate REAL,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        IsFeatured INTEGER NOT NULL DEFAULT 0,
                        IsVerified INTEGER NOT NULL DEFAULT 0,
                        HasGarage INTEGER NOT NULL DEFAULT 0,
                        HasBalcony INTEGER NOT NULL DEFAULT 0,
                        HasElevator INTEGER NOT NULL DEFAULT 0,
                        HasFurniture INTEGER NOT NULL DEFAULT 0,
                        Source TEXT,
                        ExternalId TEXT,
                        ExternalUrl TEXT,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL
                    );
                ");

                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS PropertyImages (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PropertyId INTEGER NOT NULL,
                        FileName TEXT NOT NULL,
                        OriginalFileName TEXT,
                        ContentType TEXT,
                        FileExtension TEXT,
                        ImageData BLOB,
                        IsMainImage INTEGER NOT NULL DEFAULT 0,
                        IsWatermarkRemoved INTEGER NOT NULL DEFAULT 0,
                        Source TEXT,
                        ExternalUrl TEXT,
                        UploadedAt TEXT NOT NULL,
                        FOREIGN KEY (PropertyId) REFERENCES Properties(Id)
                    );
                ");

                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS RentalAgreements (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PropertyId INTEGER NOT NULL,
                        EmployeeId INTEGER NOT NULL,
                        TenantName TEXT NOT NULL,
                        TenantLastName TEXT,
                        TenantPhone TEXT,
                        TenantEmail TEXT,
                        TenantIdNumber TEXT,
                        StartDate TEXT NOT NULL,
                        EndDate TEXT NOT NULL,
                        MonthlyRent REAL NOT NULL,
                        Currency TEXT NOT NULL,
                        DepositAmount REAL,
                        CommissionRate REAL NOT NULL,
                        OwnerCommissionAmount REAL NOT NULL,
                        TenantCommissionAmount REAL NOT NULL,
                        AgentCommissionAmount REAL NOT NULL,
                        AgreementNumber TEXT,
                        PaymentDay INTEGER NOT NULL DEFAULT 1,
                        PaymentTerm INTEGER NOT NULL DEFAULT 1,
                        SpecialConditions TEXT,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        IsSigned INTEGER NOT NULL DEFAULT 0,
                        SignedDate TEXT,
                        UtilitiesIncluded INTEGER NOT NULL DEFAULT 0,
                        InternetIncluded INTEGER NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        FOREIGN KEY (PropertyId) REFERENCES Properties(Id),
                        FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
                    );
                ");

                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS SaleAgreements (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PropertyId INTEGER NOT NULL,
                        EmployeeId INTEGER NOT NULL,
                        BuyerName TEXT NOT NULL,
                        BuyerLastName TEXT,
                        BuyerPhone TEXT,
                        BuyerEmail TEXT,
                        BuyerIdNumber TEXT,
                        SalePrice REAL NOT NULL,
                        Currency TEXT NOT NULL,
                        CommissionRate REAL NOT NULL,
                        SellerCommissionAmount REAL NOT NULL,
                        BuyerCommissionAmount REAL NOT NULL,
                        AgentCommissionAmount REAL NOT NULL,
                        SaleDate TEXT NOT NULL,
                        HandoverDate TEXT,
                        AgreementNumber TEXT,
                        SpecialConditions TEXT,
                        SaleType TEXT NOT NULL,
                        PaymentMethod TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        IsSigned INTEGER NOT NULL DEFAULT 0,
                        SignedDate TEXT,
                        IsCompleted INTEGER NOT NULL DEFAULT 0,
                        CompletionDate TEXT,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        FOREIGN KEY (PropertyId) REFERENCES Properties(Id),
                        FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
                    );
                ");

                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS FinancialTransactions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Amount REAL NOT NULL,
                        Currency TEXT NOT NULL,
                        TransactionType TEXT NOT NULL,
                        TransactionSubType TEXT NOT NULL,
                        PropertyId INTEGER,
                        EmployeeId INTEGER,
                        RentalAgreementId INTEGER,
                        PaymentMethod TEXT,
                        ReceiptNumber TEXT,
                        Notes TEXT,
                        TransactionDate TEXT NOT NULL,
                        RecordedAt TEXT NOT NULL,
                        RecordedByUser TEXT,
                        FOREIGN KEY (PropertyId) REFERENCES Properties(Id),
                        FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
                        FOREIGN KEY (RentalAgreementId) REFERENCES RentalAgreements(Id)
                    );
                ");

                // Create indexes for performance
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_properties_listing_type ON Properties(ListingType);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_properties_property_type ON Properties(PropertyType);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_properties_is_active ON Properties(IsActive);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_property_images_property_id ON PropertyImages(PropertyId);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_rental_agreements_property_id ON RentalAgreements(PropertyId);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_rental_agreements_employee_id ON RentalAgreements(EmployeeId);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_sale_agreements_property_id ON SaleAgreements(PropertyId);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_sale_agreements_employee_id ON SaleAgreements(EmployeeId);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_financial_transactions_transaction_type ON FinancialTransactions(TransactionType);");
            }
        }

        /// <summary>
        /// İlkin məlumatları verilənlər bazasına yerləşdirir
        /// </summary>
        private static void SeedInitialData()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                // Add admin user and employee
                var employee = new Employee
                {
                    FirstName = "Admin",
                    LastName = "User",
                    Position = "Manager",
                    BaseSalary = 1500,
                    CommissionRate = 10,
                    HireDate = DateTime.Now,
                    IsActive = true
                };

                int employeeId = ExecuteInsert(connection, @"
                    INSERT INTO Employees (FirstName, LastName, Position, BaseSalary, CommissionRate, HireDate, IsActive)
                    VALUES (@FirstName, @LastName, @Position, @BaseSalary, @CommissionRate, @HireDate, @IsActive);
                    SELECT last_insert_rowid();
                ", employee);

                var user = new User
                {
                    Username = "admin",
                    PasswordHash = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918", // sha256('admin')
                    FirstName = "Admin",
                    LastName = "User",
                    Role = "Admin",
                    EmployeeId = employeeId,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                ExecuteNonQuery(connection, @"
                    INSERT INTO Users (Username, PasswordHash, FirstName, LastName, EmployeeId, Role, IsActive, CreatedAt, UpdatedAt)
                    VALUES (@Username, @PasswordHash, @FirstName, @LastName, @EmployeeId, @Role, @IsActive, @CreatedAt, @UpdatedAt);
                ", user);
            }
        }

        /// <summary>
        /// Sadə sorğu icra edir
        /// </summary>
        private static void ExecuteNonQuery(SQLiteConnection connection, string commandText, object parameters = null)
        {
            using (var command = new SQLiteCommand(commandText, connection))
            {
                if (parameters != null)
                {
                    var props = parameters.GetType().GetProperties();
                    foreach (var prop in props)
                    {
                        var value = prop.GetValue(parameters);
                        if (value is bool)
                            value = (bool)value ? 1 : 0;
                        else if (value is DateTime)
                            value = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");

                        command.Parameters.AddWithValue($"@{prop.Name}", value ?? DBNull.Value);
                    }
                }

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Sorğu icra edir və son əlavə edilmiş sətrin ID-sini qaytarır
        /// </summary>
        private static int ExecuteInsert(SQLiteConnection connection, string commandText, object parameters = null)
        {
            using (var command = new SQLiteCommand(commandText, connection))
            {
                if (parameters != null)
                {
                    var props = parameters.GetType().GetProperties();
                    foreach (var prop in props)
                    {
                        var value = prop.GetValue(parameters);
                        if (value is bool)
                            value = (bool)value ? 1 : 0;
                        else if (value is DateTime)
                            value = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");

                        command.Parameters.AddWithValue($"@{prop.Name}", value ?? DBNull.Value);
                    }
                }

                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        /// <summary>
        /// Dapper vasitəsi ilə bağlantı obyekti qaytarır
        /// </summary>
        public static IDbConnection GetConnection()
        {
            return new SQLiteConnection(ConnectionString);
        }
    }
}