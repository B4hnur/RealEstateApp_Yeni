using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RealEstateApp.Utils
{
    /// <summary>
    /// Şifrələrin hash edilməsi üçün vasitə
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Şifrəni SHA-256 ilə hash edir
        /// </summary>
        public static string Hash(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// Təsadüfi şifrə yaradır
        /// </summary>
        public static string GenerateRandomPassword(int length = 8)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()-_=+";
            StringBuilder result = new StringBuilder();
            Random random = new Random();

            while (0 < length--)
            {
                result.Append(validChars[random.Next(validChars.Length)]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Şifrənin gücünü qiymətləndirir (1-çox zəif, 5-çox güclü)
        /// </summary>
        public static int CheckPasswordStrength(string password)
        {
            int score = 0;

            if (string.IsNullOrEmpty(password) || password.Length < 4)
                return 1; // Very weak

            // Length check
            if (password.Length >= 8)
                score += 1;
            if (password.Length >= 12)
                score += 1;

            // Complexity checks
            if (password.Any(char.IsDigit))
                score += 1;

            if (password.Any(char.IsUpper) && password.Any(char.IsLower))
                score += 1;

            if (password.Any(c => !char.IsLetterOrDigit(c)))
                score += 1;

            return Math.Min(5, score);
        }

        /// <summary>
        /// Şifrə gücü mətnini göstərir
        /// </summary>
        public static string GetPasswordStrengthText(int strength)
        {
            switch (strength)
            {
                case 1:
                    return "Çox zəif";
                case 2:
                    return "Zəif";
                case 3:
                    return "Orta";
                case 4:
                    return "Güclü";
                case 5:
                    return "Çox güclü";
                default:
                    return "Bilinmir";
            }
        }
    }
}