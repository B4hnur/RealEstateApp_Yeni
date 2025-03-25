using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateApp.Models
{
    /// <summary>
    /// Daşınmaz əmlak şəkillərini idarə edən sinif
    /// </summary>
    public class PropertyImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PropertyId { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Fayl adı")]
        public string FileName { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Orijinal fayl adı")]
        public string OriginalFileName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Content növü")]
        public string ContentType { get; set; } = "image/jpeg";

        [StringLength(50)]
        [Display(Name = "Fayl genişlənməsi")]
        public string FileExtension { get; set; } = ".jpg";

        [Display(Name = "Şəkil məlumatları")]
        public byte[] ImageData { get; set; }

        [Display(Name = "Əsas şəkil")]
        public bool IsMainImage { get; set; } = false;

        [Display(Name = "Filigran silinib")]
        public bool IsWatermarkRemoved { get; set; } = false;

        [Display(Name = "Şəkil mənbəyi")]
        [StringLength(255)]
        public string Source { get; set; } = "Manual";

        [Display(Name = "Xarici URL")]
        [StringLength(500)]
        public string ExternalUrl { get; set; } = string.Empty;

        [Display(Name = "Yüklənmə tarixi")]
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }

        [NotMapped]
        public string ImageUrl => $"/Images/{FileName}";
    }
}