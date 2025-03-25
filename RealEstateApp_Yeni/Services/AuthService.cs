using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapper;
using RealEstateApp.Models;
using RealEstateApp.Utils;

namespace RealEstateApp.Services
{
    /// <summary>
    /// Autentifikasiya və istifadəçi idarəetməsi xidməti
    /// </summary>
    public class AuthService
    {
        // Current logged in user
        private static User _currentUser;

        /// <summary>
        /// Hal-hazırda sistemə daxil olmuş istifadəçini qaytarır
        /// </summary>
        public static User CurrentUser
        {
            get { return _currentUser; }
            private set { _currentUser = value; }
        }

        /// <summary>
        /// Sistemə giriş
        /// </summary>
        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    // Get user by username
                    var user = await connection.QueryFirstOrDefaultAsync<User>(
                        "SELECT * FROM Users WHERE Username = @Username",
                        new { Username = username });

                    if (user == null)
                    {
                        return false; // User not found
                    }

                    // Check if user is active
                    if (!user.IsActive)
                    {
                        MessageBox.Show("Bu istifadəçi hesabı deaktiv edilib.",
                            "Giriş xətası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    // Check if user is locked
                    if (user.IsLocked)
                    {
                        MessageBox.Show("Bu istifadəçi hesabı bloklanıb. Administrator ilə əlaqə saxlayın.",
                            "Giriş xətası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    // Verify password
                    string hashedPassword = PasswordHasher.Hash(password);
                    if (user.PasswordHash != hashedPassword)
                    {
                        return false; // Invalid password
                    }

                    // If user has EmployeeId, load employee data
                    if (user.EmployeeId.HasValue)
                    {
                        user.Employee = await connection.QueryFirstOrDefaultAsync<Employee>(
                            "SELECT * FROM Employees WHERE Id = @Id",
                            new { Id = user.EmployeeId.Value });
                    }

                    // Update last login
                    await connection.ExecuteAsync(
                        "UPDATE Users SET LastLogin = @LastLogin WHERE Id = @Id",
                        new { Id = user.Id, LastLogin = DateTime.Now });

                    // Set current user
                    CurrentUser = user;

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Giriş zamanı xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Sistemdən çıxış
        /// </summary>
        public void Logout()
        {
            CurrentUser = null;
        }

        /// <summary>
        /// Yeni istifadəçi yaradır
        /// </summary>
        public async Task<bool> CreateUserAsync(User user, string password)
        {
            try
            {
                // Hash the password
                user.PasswordHash = PasswordHasher.Hash(password);
                user.CreatedAt = DateTime.Now;
                user.UpdatedAt = DateTime.Now;

                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    // Check if username already exists
                    int existingCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Users WHERE Username = @Username",
                        new { user.Username });

                    if (existingCount > 0)
                    {
                        MessageBox.Show("Bu istifadəçi adı artıq mövcuddur.",
                            "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    // Insert the new user
                    string sql = @"
                        INSERT INTO Users (
                            Username, PasswordHash, FirstName, LastName, Email, Phone,
                            EmployeeId, Role, IsActive, IsLocked, CreatedAt, UpdatedAt, Notes
                        ) VALUES (
                            @Username, @PasswordHash, @FirstName, @LastName, @Email, @Phone,
                            @EmployeeId, @Role, @IsActive, @IsLocked, @CreatedAt, @UpdatedAt, @Notes
                        );
                        SELECT last_insert_rowid();";

                    user.Id = await connection.ExecuteScalarAsync<int>(sql, user);

                    return user.Id > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İstifadəçi yaradılarkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Mövcud istifadəçini yeniləyir
        /// </summary>
        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                user.UpdatedAt = DateTime.Now;

                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    // Update the user
                    string sql = @"
                        UPDATE Users SET
                            FirstName = @FirstName,
                            LastName = @LastName,
                            Email = @Email,
                            Phone = @Phone,
                            EmployeeId = @EmployeeId,
                            Role = @Role,
                            IsActive = @IsActive,
                            IsLocked = @IsLocked,
                            UpdatedAt = @UpdatedAt,
                            Notes = @Notes
                        WHERE Id = @Id";

                    int rowsAffected = await connection.ExecuteAsync(sql, user);

                    // If the updated user is the current user, update CurrentUser
                    if (CurrentUser != null && CurrentUser.Id == user.Id)
                    {
                        // Reload the user data
                        CurrentUser = await connection.QueryFirstOrDefaultAsync<User>(
                            "SELECT * FROM Users WHERE Id = @Id",
                            new { Id = user.Id });

                        // If user has EmployeeId, load employee data
                        if (CurrentUser.EmployeeId.HasValue)
                        {
                            CurrentUser.Employee = await connection.QueryFirstOrDefaultAsync<Employee>(
                                "SELECT * FROM Employees WHERE Id = @Id",
                                new { Id = CurrentUser.EmployeeId.Value });
                        }
                    }

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İstifadəçi yenilənərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// İstifadəçi şifrəsini dəyişir
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    // Get the user
                    var user = await connection.QueryFirstOrDefaultAsync<User>(
                        "SELECT * FROM Users WHERE Id = @Id",
                        new { Id = userId });

                    if (user == null)
                    {
                        return false;
                    }

                    // Verify current password
                    string hashedCurrentPassword = PasswordHasher.Hash(currentPassword);
                    if (user.PasswordHash != hashedCurrentPassword)
                    {
                        MessageBox.Show("Cari şifrə düzgün deyil.",
                            "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    // Update password
                    string hashedNewPassword = PasswordHasher.Hash(newPassword);
                    await connection.ExecuteAsync(
                        "UPDATE Users SET PasswordHash = @PasswordHash, UpdatedAt = @UpdatedAt WHERE Id = @Id",
                        new
                        {
                            Id = userId,
                            PasswordHash = hashedNewPassword,
                            UpdatedAt = DateTime.Now
                        });

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Şifrə dəyişdirilirkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Administrator tərəfindən istifadəçi şifrəsini dəyişir
        /// </summary>
        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            try
            {
                // Check if current user is admin
                if (CurrentUser == null || CurrentUser.Role != "Admin")
                {
                    MessageBox.Show("Bu əməliyyat üçün administrator hüquqları tələb olunur.",
                        "İcazə yoxdur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    // Hash the new password
                    string hashedPassword = PasswordHasher.Hash(newPassword);

                    // Update the password
                    int rowsAffected = await connection.ExecuteAsync(
                        "UPDATE Users SET PasswordHash = @PasswordHash, UpdatedAt = @UpdatedAt WHERE Id = @Id",
                        new
                        {
                            Id = userId,
                            PasswordHash = hashedPassword,
                            UpdatedAt = DateTime.Now
                        });

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Şifrə yenilənərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Bütün istifadəçiləri qaytarır
        /// </summary>
        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    var users = await connection.QueryAsync<User>("SELECT * FROM Users ORDER BY Username");

                    return users.ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İstifadəçilər yüklənərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<User>();
            }
        }

        /// <summary>
        /// ID-yə görə istifadəçi qaytarır
        /// </summary>
        public async Task<User> GetUserByIdAsync(int id)
        {
            try
            {
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    var user = await connection.QueryFirstOrDefaultAsync<User>(
                        "SELECT * FROM Users WHERE Id = @Id", new { Id = id });

                    if (user != null && user.EmployeeId.HasValue)
                    {
                        user.Employee = await connection.QueryFirstOrDefaultAsync<Employee>(
                            "SELECT * FROM Employees WHERE Id = @Id",
                            new { Id = user.EmployeeId.Value });
                    }

                    return user;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İstifadəçi yüklənərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// İstifadəçini silir
        /// </summary>
        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                // Check if current user is admin
                if (CurrentUser == null || CurrentUser.Role != "Admin")
                {
                    MessageBox.Show("Bu əməliyyat üçün administrator hüquqları tələb olunur.",
                        "İcazə yoxdur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                // Prevent deleting own account
                if (CurrentUser.Id == id)
                {
                    MessageBox.Show("Öz hesabınızı silə bilməzsiniz.",
                        "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    int rowsAffected = await connection.ExecuteAsync(
                        "DELETE FROM Users WHERE Id = @Id", new { Id = id });

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İstifadəçi silinərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Mövcud istifadəçinin verilmiş əməliyyatı icra etmək hüququ olub-olmadığını yoxlayır
        /// </summary>
        public static bool HasPermission(string requiredRole)
        {
            if (CurrentUser == null)
                return false;

            // Admin has all permissions
            if (CurrentUser.Role == "Admin")
                return true;

            // Check specific role permissions
            switch (requiredRole)
            {
                case "Admin":
                    return CurrentUser.Role == "Admin";

                case "Manager":
                    return CurrentUser.Role == "Admin" || CurrentUser.Role == "Manager";

                case "Agent":
                    return CurrentUser.Role == "Admin" || CurrentUser.Role == "Manager" || CurrentUser.Role == "Agent";

                case "Accountant":
                    return CurrentUser.Role == "Admin" || CurrentUser.Role == "Accountant";

                default:
                    return false;
            }
        }

        /// <summary>
        /// İstifadəçi rollarını qaytarır
        /// </summary>
        public List<string> GetUserRoles()
        {
            return new List<string>
            {
                "Admin",
                "Manager",
                "Agent",
                "Accountant"
            };
        }
    }
}