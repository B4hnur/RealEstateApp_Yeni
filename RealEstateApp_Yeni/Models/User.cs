using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateApp.Models
{
    /// <summary>
    /// Proqramdan istifadə edəcək istifadəçiləri təmsil edən sinif
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "İstifadəçi adı")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Şifrə")]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Telefon")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "İşçi")]
        public int? EmployeeId { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Rol")]
        public string Role { get; set; } = "Agent"; // Admin, Manager, Agent, Accountant

        [Display(Name = "Son giriş tarixi")]
        public DateTime? LastLogin { get; set; }

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Kilitli")]
        public bool IsLocked { get; set; } = false;

        [Display(Name = "Yaradılma tarixi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Yenilənmə tarixi")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Qeyd")]
        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [NotMapped]
        public string RoleDisplay
        {
            get
            {
                if (Role == "Admin")
                {
                    return "Administrator";
                }
                else if (Role == "Manager")
                {
                    return "Menecer";
                }
                else if (Role == "Agent")
                {
                    return "Əmlak agenti";
                }
                else if (Role == "Accountant")
                {
                    return "Mühasib";
                }
                else
                {
                    return Role; // Varsayılan durum
                }
            }
        }
    }
}
