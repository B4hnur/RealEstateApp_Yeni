using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateApp.Models
{
    /// <summary>
    /// Daşınmaz əmlak obyekti modelidir
    /// </summary>
    public class Property
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Başlıq")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Təsvir")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, double.MaxValue)]
        [Display(Name = "Qiymət")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Display(Name = "Valyuta")]
        [StringLength(10)]
        public string Currency { get; set; } = "AZN";

        [Required]
        [StringLength(200)]
        [Display(Name = "Ünvan")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Şəhər")]
        [StringLength(50)]
        public string City { get; set; } = "Bakı";

        [Display(Name = "Rayon")]
        [StringLength(50)]
        public string District { get; set; } = string.Empty;

        [Range(0, 50)]
        [Display(Name = "Yataq otaqları")]
        public int? Bedrooms { get; set; }

        [Range(0, 20)]
        [Display(Name = "Hamam otaqları")]
        public int? Bathrooms { get; set; }

        [Range(1, 10000)]
        [Display(Name = "Sahə (m²)")]
        public int? Area { get; set; }

        [Display(Name = "Tikinti ili")]
        [Range(1800, 2100)]
        public int? YearBuilt { get; set; }

        [Display(Name = "Mərtəbə")]
        [Range(0, 50)]
        public int? Floor { get; set; }

        [Display(Name = "Ümumi mərtəbə")]
        [Range(0, 50)]
        public int? TotalFloors { get; set; }

        [Display(Name = "Əmlak növü")]
        [Required]
        public string PropertyType { get; set; } = "Mənzil";

        [Display(Name = "Elanın növü")]
        [Required]
        public string ListingType { get; set; } = "Satış";

        [Display(Name = "Sahibi")]
        [StringLength(100)]
        public string OwnerName { get; set; } = string.Empty;

        [Display(Name = "Sahibin telefonu")]
        [StringLength(20)]
        public string OwnerPhone { get; set; } = string.Empty;

        [Display(Name = "Sahibin emaili")]
        [StringLength(100)]
        [DataType(DataType.EmailAddress)]
        public string OwnerEmail { get; set; } = string.Empty;

        [Display(Name = "Mülkiyyət növü")]
        [StringLength(50)]
        public string OwnershipType { get; set; } = "Şəxsi mülkiyyət";

        [Display(Name = "Komissiya dərəcəsi (%)")]
        [Range(0, 100)]
        public decimal CommissionRate { get; set; } = 30;

        [Display(Name = "Aktiv elan")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Vip elan")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Verified")]
        public bool IsVerified { get; set; } = false;

        [Display(Name = "Qaraj")]
        public bool HasGarage { get; set; } = false;

        [Display(Name = "Balkon")]
        public bool HasBalcony { get; set; } = false;

        [Display(Name = "Lift")]
        public bool HasElevator { get; set; } = false;

        [Display(Name = "Mebel")]
        public bool HasFurniture { get; set; } = false;

        [Display(Name = "Elanın mənbəyi")]
        [StringLength(100)]
        public string Source { get; set; } = "Manual";

        [Display(Name = "Xarici ID")]
        [StringLength(100)]
        public string ExternalId { get; set; } = string.Empty;

        [Display(Name = "Xarici URL")]
        [StringLength(500)]
        public string ExternalUrl { get; set; } = string.Empty;

        public virtual ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();

        public virtual ICollection<RentalAgreement> RentalAgreements { get; set; } = new List<RentalAgreement>();

        [Display(Name = "Yaradılma tarixi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Yenilənmə tarixi")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string MainImageUrl
        {
            get
            {
                foreach (var image in Images)
                {
                    if (image.IsMainImage)
                        return image.FileName;
                }

                if (Images.Count > 0)
                {
                    var firstImage = Images as List<PropertyImage>;
                    return firstImage[0].FileName;
                }

                return "/Resources/no-image.png";
            }
        }

        [NotMapped]
        public string PropertyTypeDisplay
        {
            get
            {
                if (PropertyType == "Apartment")
                {
                    return "Mənzil";
                }
                else if (PropertyType == "House")
                {
                    return "Ev / Villa";
                }
                else if (PropertyType == "Land")
                {
                    return "Torpaq";
                }
                else if (PropertyType == "Commercial")
                {
                    return "Kommersiya";
                }
                else if (PropertyType == "Office")
                {
                    return "Ofis";
                }
                else if (PropertyType == "Garage")
                {
                    return "Qaraj";
                }
                else
                {
                    return PropertyType; // Varsayılan durum
                }
            }
        }
    }
}
