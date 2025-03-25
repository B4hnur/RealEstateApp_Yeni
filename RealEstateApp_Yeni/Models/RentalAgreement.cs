using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateApp.Models
{
    /// <summary>
    /// Kirayə müqavilələrini idarə etmək üçün model
    /// </summary>
    public class RentalAgreement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Əmlak")]
        public int PropertyId { get; set; }

        [Required]
        [Display(Name = "Məsul agent")]
        public int EmployeeId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Kirayəçi adı")]
        public string TenantName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Kirayəçi soyadı")]
        public string TenantLastName { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Kirayəçi telefonu")]
        public string TenantPhone { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Kirayəçi email")]
        public string TenantEmail { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Kirayəçi şəxsiyyət vəsiqəsi")]
        public string TenantIdNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Başlama tarixi")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Bitmə tarixi")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddYears(1);

        [Required]
        [Display(Name = "Aylıq kirayə məbləği")]
        [DataType(DataType.Currency)]
        public decimal MonthlyRent { get; set; }

        [Display(Name = "Valyuta")]
        [StringLength(10)]
        public string Currency { get; set; } = "AZN";

        [Display(Name = "Depozit məbləği")]
        [DataType(DataType.Currency)]
        public decimal? DepositAmount { get; set; }

        [Display(Name = "Komissiya dərəcəsi (%)")]
        [Range(0, 100)]
        public decimal CommissionRate { get; set; } = 30;

        [Display(Name = "Əmlak sahibi komissiya məbləği")]
        [DataType(DataType.Currency)]
        public decimal OwnerCommissionAmount { get; set; }

        [Display(Name = "Kirayəçi komissiya məbləği")]
        [DataType(DataType.Currency)]
        public decimal TenantCommissionAmount { get; set; }

        [Display(Name = "Agent komissiya məbləği")]
        [DataType(DataType.Currency)]
        public decimal AgentCommissionAmount { get; set; }

        [StringLength(100)]
        [Display(Name = "Müqavilə nömrəsi")]
        public string AgreementNumber { get; set; } = string.Empty;

        [Display(Name = "Ödəniş günü")]
        [Range(1, 31)]
        public int PaymentDay { get; set; } = 1;

        [Display(Name = "Ödəniş müddəti (aylıq)")]
        public int PaymentTerm { get; set; } = 1;

        [StringLength(1000)]
        [Display(Name = "Xüsusi şərtlər")]
        public string SpecialConditions { get; set; } = string.Empty;

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "İmzalanıb")]
        public bool IsSigned { get; set; } = false;

        [Display(Name = "İmzalanma tarixi")]
        public DateTime? SignedDate { get; set; }

        [Display(Name = "Kommunal xərclər daxildir")]
        public bool UtilitiesIncluded { get; set; } = false;

        [Display(Name = "İnternet daxildir")]
        public bool InternetIncluded { get; set; } = false;

        [Display(Name = "Yaradılma tarixi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Yenilənmə tarixi")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        public virtual ICollection<FinancialTransaction> FinancialTransactions { get; set; } = new List<FinancialTransaction>();

        [NotMapped]
        public string TenantFullName => $"{TenantName} {TenantLastName}";

        [NotMapped]
        public string FormattedRent => $"{MonthlyRent:N2} {Currency}";

        [NotMapped]
        public string FormattedDeposit => DepositAmount.HasValue ? $"{DepositAmount.Value:N2} {Currency}" : "Depozit yoxdur";

        [NotMapped]
        public string FormattedDuration => $"{(EndDate - StartDate).Days / 30} ay";

        [NotMapped]
        public string StatusDisplay
        {
            get
            {
                if (!IsActive)
                    return "Bitmiş";

                if (DateTime.Now > EndDate)
                    return "Vaxtı keçmiş";

                if (DateTime.Now < StartDate)
                    return "Başlanmamış";

                return "Aktiv";
            }
        }

        [NotMapped]
        public int RemainingDays => Math.Max(0, (EndDate - DateTime.Now).Days);

        public void CalculateCommissions()
        {
            // Sahibkar komissiyası
            OwnerCommissionAmount = MonthlyRent * (CommissionRate / 100m);

            // Kirayəçi komissiyası
            TenantCommissionAmount = MonthlyRent * (CommissionRate / 100m);

            // Agent komissiyası (ümumi komissiyanın 10%-i qədər)
            decimal totalCommission = OwnerCommissionAmount + TenantCommissionAmount;
            AgentCommissionAmount = totalCommission * 0.1m;
        }
    }
}