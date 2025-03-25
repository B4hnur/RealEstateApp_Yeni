using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateApp.Models
{
    /// <summary>
    /// Şirkət işçilərini təmsil edən sinif
    /// </summary>
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Telefon")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Vəzifə")]
        public string Position { get; set; } = "Agent";

        [Display(Name = "Əmək haqqı")]
        [Range(0, double.MaxValue)]
        public decimal BaseSalary { get; set; } = 500;

        [Display(Name = "Komissiya dərəcəsi (%)")]
        [Range(0, 100)]
        public decimal CommissionRate { get; set; } = 10;

        [Display(Name = "İşə başlama tarixi")]
        public DateTime HireDate { get; set; } = DateTime.Now;

        [Display(Name = "İşdən çıxma tarixi")]
        public DateTime? TerminationDate { get; set; } = null;

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; } = true;

        public virtual ICollection<RentalAgreement> RentalAgreements { get; set; } = new List<RentalAgreement>();
        public virtual ICollection<FinancialTransaction> FinancialTransactions { get; set; } = new List<FinancialTransaction>();

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [NotMapped]
        public string PositionDisplay
        {
            get
            {
                if (Position == "Agent")
                {
                    return "Əmlak agenti";
                }
                else if (Position == "Manager")
                {
                    return "Menecer";
                }
                else if (Position == "Director")
                {
                    return "Direktor";
                }
                else if (Position == "Accountant")
                {
                    return "Mühasib";
                }
                else if (Position == "Secretary")
                {
                    return "Katib";
                }
                else
                {
                    return Position; // Default case
                }
            }
        }

    }
}
