using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateApp.Models
{
    /// <summary>
    /// Veb saytdan çəkilmiş əmlak elanı modelini təmsil edir
    /// </summary>
    [NotMapped]
    public class PropertyListing
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = "Bakı";
        public string District { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "AZN";
        public int? Rooms { get; set; }
        public decimal? Area { get; set; }
        public int? Floor { get; set; }
        public int? TotalFloors { get; set; }
        public string PropertyType { get; set; } = "Mənzil";
        public string ListingType { get; set; } = "Satış"; // Satış və ya Kirayə
        public string OwnerType { get; set; } = "Sahibkar";
        public string OwnerPhone { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new List<string>();
        public string MainImageUrl { get; set; } = string.Empty;
        public string DetailUrl { get; set; } = string.Empty;
        public string Source { get; set; } = "kub.az";
        public DateTime ListingDate { get; set; } = DateTime.Now;
        public bool IsFeatured { get; set; } = false;
        public bool IsVerified { get; set; } = false;
        public Dictionary<string, string> AdditionalFeatures { get; set; } = new Dictionary<string, string>();

        // Local only properties
        [NotMapped]
        public bool IsFavorite { get; set; }

        [NotMapped]
        public bool IsNew
        {
            get
            {
                return (DateTime.Now - ListingDate).TotalDays <= 3;
            }
        }

        [NotMapped]
        public string FormattedPrice
        {
            get
            {
                return string.Format("{0:N0} {1}", Price, Currency);
            }
        }

        [NotMapped]
        public string FormattedRooms
        {
            get
            {
                return Rooms.HasValue ? $"{Rooms} otaq" : "Otaq məlumatı yoxdur";
            }
        }

        [NotMapped]
        public string FormattedArea
        {
            get
            {
                return Area.HasValue ? $"{Area} m²" : "Sahə məlumatı yoxdur";
            }
        }

        [NotMapped]
        public string FormattedFloor
        {
            get
            {
                if (Floor.HasValue && TotalFloors.HasValue)
                {
                    return $"{Floor}/{TotalFloors}";
                }
                else if (Floor.HasValue)
                {
                    return $"{Floor}";
                }
                return "Mərtəbə məlumatı yoxdur";
            }
        }

        // Converting from database Property to PropertyListing
        public static PropertyListing FromProperty(Property property)
        {
            var listing = new PropertyListing
            {
                Id = property.Id.ToString(),
                Title = property.Title,
                Description = property.Description,
                Address = property.Address,
                City = property.City,
                District = property.District,
                Price = property.Price,
                Currency = property.Currency,
                Rooms = property.Bedrooms,
                Area = property.Area.HasValue ? (decimal?)property.Area.Value : null,
                Floor = property.Floor,
                TotalFloors = property.TotalFloors,
                PropertyType = property.PropertyType,
                ListingType = property.ListingType,
                OwnerType = property.OwnershipType,
                OwnerPhone = property.OwnerPhone,
                Source = "Database",
                ListingDate = property.CreatedAt,
                IsFeatured = property.IsFeatured,
                IsVerified = property.IsVerified,
                DetailUrl = $"local://{property.Id}"
            };

            // Add images
            foreach (var image in property.Images)
            {
                listing.ImageUrls.Add(image.FileName);
                if (image.IsMainImage)
                {
                    listing.MainImageUrl = image.FileName;
                }
            }

            if (string.IsNullOrEmpty(listing.MainImageUrl) && listing.ImageUrls.Count > 0)
            {
                listing.MainImageUrl = listing.ImageUrls[0];
            }

            // Add additional features
            if (property.HasGarage) listing.AdditionalFeatures["Qaraj"] = "Var";
            if (property.HasBalcony) listing.AdditionalFeatures["Balkon"] = "Var";
            if (property.HasElevator) listing.AdditionalFeatures["Lift"] = "Var";
            if (property.HasFurniture) listing.AdditionalFeatures["Mebel"] = "Var";

            return listing;
        }

        // Converting from PropertyListing to database Property
        public Property ToProperty()
        {
            var property = new Property
            {
                Title = Title,
                Description = Description,
                Price = Price,
                Currency = Currency,
                Address = Address,
                City = City,
                District = District,
                Bedrooms = Rooms,
                Area = Area.HasValue ? (int?)Area.Value : null,
                Floor = Floor,
                TotalFloors = TotalFloors,
                PropertyType = PropertyType,
                ListingType = ListingType,
                OwnershipType = OwnerType,
                OwnerPhone = OwnerPhone,
                Source = Source,
                ExternalId = Id,
                ExternalUrl = DetailUrl,
                CreatedAt = ListingDate,
                UpdatedAt = DateTime.Now,
                IsActive = true,
                IsFeatured = IsFeatured,
                IsVerified = IsVerified
            };

            // Setting additional features
            if (AdditionalFeatures.ContainsKey("Qaraj") && AdditionalFeatures["Qaraj"] == "Var")
                property.HasGarage = true;

            if (AdditionalFeatures.ContainsKey("Balkon") && AdditionalFeatures["Balkon"] == "Var")
                property.HasBalcony = true;

            if (AdditionalFeatures.ContainsKey("Lift") && AdditionalFeatures["Lift"] == "Var")
                property.HasElevator = true;

            if (AdditionalFeatures.ContainsKey("Mebel") && AdditionalFeatures["Mebel"] == "Var")
                property.HasFurniture = true;

            return property;
        }
    }
}