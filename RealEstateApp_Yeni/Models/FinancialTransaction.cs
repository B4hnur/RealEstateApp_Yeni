using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateApp.Models
{
    /// <summary>
    /// Maliyyə əməliyyatlarını izləmək üçün model
    /// </summary>
    public class FinancialTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Məbləğ")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Valyuta")]
        [StringLength(10)]
        public string Currency { get; set; } = "AZN";

        [Required]
        [Display(Name = "Əməliyyat növü")]
        [StringLength(50)]
        public string TransactionType { get; set; } = "Income"; // Income, Expense, Commission, Salary

        [Required]
        [Display(Name = "Əməliyyat alt-növü")]
        [StringLength(100)]
        public string TransactionSubType { get; set; } = string.Empty; // Rent, Sale, OwnerCommission, ClientCommission, etc.

        [Display(Name = "Əmlak")]
        public int? PropertyId { get; set; }

        [Display(Name = "İşçi")]
        public int? EmployeeId { get; set; }

        [Display(Name = "Kirayə müqaviləsi")]
        public int? RentalAgreementId { get; set; }

        [Display(Name = "Ödəniş üsulu")]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, BankTransfer, Card, etc.

        [Display(Name = "Qəbz nömrəsi")]
        [StringLength(50)]
        public string ReceiptNumber { get; set; } = string.Empty;

        [Display(Name = "Qeyd")]
        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Əməliyyat tarixi")]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Display(Name = "Qeydə alınma tarixi")]
        public DateTime RecordedAt { get; set; } = DateTime.Now;

        [Display(Name = "Qeyd edən istifadəçi")]
        [StringLength(50)]
        public string RecordedByUser { get; set; } = string.Empty;

        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        [ForeignKey("RentalAgreementId")]
        public virtual RentalAgreement RentalAgreement { get; set; }

        [NotMapped]
        public string FormattedAmount => $"{Amount:N2} {Currency}";

        [NotMapped]
        public string TransactionTypeDisplay
        {
            get
            {
                if (TransactionType == "Income")
                {
                    return "Gəlir";
                }
                else if (TransactionType == "Expense")
                {
                    return "Xərc";
                }
                else if (TransactionType == "Commission")
                {
                    return "Komissiya";
                }
                else if (TransactionType == "Salary")
                {
                    return "Əmək haqqı";
                }
                else
                {
                    return TransactionType; // Varsayılan durum
                }
            }
        }


        [NotMapped]
        public string TransactionSubTypeDisplay
        {
            get
            {
                if (TransactionSubType == "Rent")
                {
                    return "Kirayə";
                }
                else if (TransactionSubType == "Sale")
                {
                    return "Satış";
                }
                else if (TransactionSubType == "OwnerCommission")
                {
                    return "Sahibkar komissiyası";
                }
                else if (TransactionSubType == "ClientCommission")
                {
                    return "Müştəri komissiyası";
                }
                else if (TransactionSubType == "BaseSalary")
                {
                    return "Əsas əmək haqqı";
                }
                else if (TransactionSubType == "Bonus")
                {
                    return "Bonus";
                }
                else if (TransactionSubType == "Commission")
                {
                    return "Komissiya";
                }
                else if (TransactionSubType == "OfficeRent")
                {
                    return "Ofis kirayəsi";
                }
                else if (TransactionSubType == "Utilities")
                {
                    return "Kommunal xərclər";
                }
                else if (TransactionSubType == "Marketing")
                {
                    return "Marketinq xərcləri";
                }
                else if (TransactionSubType == "Insurance")
                {
                    return "Sığorta";
                }
                else if (TransactionSubType == "Tax")
                {
                    return "Vergilər";
                }
                else if (TransactionSubType == "Maintenance")
                {
                    return "Texniki xidmət";
                }
                else if (TransactionSubType == "OtherExpense")
                {
                    return "Digər xərclər";
                }
                else
                {
                    return TransactionSubType; // Varsayılan durum
                }
            }
        }
    }
}

