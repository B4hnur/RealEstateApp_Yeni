using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateApp.Models
{
    /// <summary>
    /// Satış müqavilələrini idarə etmək üçün model
    /// </summary>
    public class SaleAgreement
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
        [Display(Name = "Alıcı adı")]
        public string BuyerName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Alıcı soyadı")]
        public string BuyerLastName { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Alıcı telefonu")]
        public string BuyerPhone { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Alıcı email")]
        public string BuyerEmail { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Alıcı şəxsiyyət vəsiqəsi")]
        public string BuyerIdNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Satış məbləği")]
        [DataType(DataType.Currency)]
        public decimal SalePrice { get; set; }

        [Display(Name = "Valyuta")]
        [StringLength(10)]
        public string Currency { get; set; } = "AZN";

        [Display(Name = "Komissiya dərəcəsi (%)")]
        [Range(0, 100)]
        public decimal CommissionRate { get; set; } = 30;

        [Display(Name = "Satıcı komissiya məbləği")]
        [DataType(DataType.Currency)]
        public decimal SellerCommissionAmount { get; set; }

        [Display(Name = "Alıcı komissiya məbləği")]
        [DataType(DataType.Currency)]
        public decimal BuyerCommissionAmount { get; set; }

        [Display(Name = "Agent komissiya məbləği")]
        [DataType(DataType.Currency)]
        public decimal AgentCommissionAmount { get; set; }

        [Required]
        [Display(Name = "Satış tarixi")]
        public DateTime SaleDate { get; set; } = DateTime.Now;

        [Display(Name = "Təhvil tarixi")]
        public DateTime? HandoverDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Müqavilə nömrəsi")]
        public string AgreementNumber { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Xüsusi şərtlər")]
        public string SpecialConditions { get; set; } = string.Empty;

        [Display(Name = "Satış növü")]
        [StringLength(50)]
        public string SaleType { get; set; } = "Standard"; // Standard, Installment, Mortgage

        [Display(Name = "Ödəniş üsulu")]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, BankTransfer, Mortgage

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "İmzalanıb")]
        public bool IsSigned { get; set; } = false;

        [Display(Name = "İmzalanma tarixi")]
        public DateTime? SignedDate { get; set; }

        [Display(Name = "Tamamlanıb")]
        public bool IsCompleted { get; set; } = false;

        [Display(Name = "Tamamlanma tarixi")]
        public DateTime? CompletionDate { get; set; }

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
        public string BuyerFullName => $"{BuyerName} {BuyerLastName}";

        [NotMapped]
        public string FormattedSalePrice => $"{SalePrice:N2} {Currency}";

        [NotMapped]
        public string StatusDisplay
        {
            get
            {
                if (IsCompleted)
                    return "Tamamlanmış";

                if (!IsActive)
                    return "Ləğv edilmiş";

                if (IsSigned)
                    return "İmzalanmış";

                return "Gözləmədə";
            }
        }

        [NotMapped]
        public string SaleTypeDisplay
        {
            get
            {
                if (SaleType == "Standard")
                {
                    return "Standart";
                }
                else if (SaleType == "Installment")
                {
                    return "Hissəli ödəniş";
                }
                else if (SaleType == "Mortgage")
                {
                    return "İpoteka";
                }
                else
                {
                    return SaleType; // Varsayılan durum
                }
            }
        }


        public void CalculateCommissions()
        {
            // Satıcı komissiyası
            SellerCommissionAmount = SalePrice * (CommissionRate / 100m);

            // Alıcı komissiyası
            BuyerCommissionAmount = SalePrice * (CommissionRate / 100m);

            // Agent komissiyası (ümumi komissiyanın 10%-i qədər)
            decimal totalCommission = SellerCommissionAmount + BuyerCommissionAmount;
            AgentCommissionAmount = totalCommission * 0.1m;
        }
    }
}