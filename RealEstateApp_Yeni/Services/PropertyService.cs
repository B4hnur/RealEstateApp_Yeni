using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapper;
using RealEstateApp.Models;

namespace RealEstateApp.Services
{
    /// <summary>
    /// Əmlak obyektlərinin idarə edilməsi üçün xidmət sinfi
    /// </summary>
    public class PropertyService
    {
        /// <summary>
        /// Bütün aktiv əmlakları qaytarır
        /// </summary>
        public async Task<List<Property>> GetAllPropertiesAsync(bool activeOnly = true)
        {
            try
            {
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    string sql = "SELECT * FROM Properties";
                    if (activeOnly)
                    {
                        sql += " WHERE IsActive = 1";
                    }
                    sql += " ORDER BY CreatedAt DESC";

                    var properties = await connection.QueryAsync<Property>(sql);

                    // Load associated images for each property
                    foreach (var property in properties)
                    {
                        var images = await connection.QueryAsync<PropertyImage>(
                            "SELECT * FROM PropertyImages WHERE PropertyId = @Id ORDER BY IsMainImage DESC",
                            new { Id = property.Id });

                        property.Images = images.ToList();
                    }

                    return properties.ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Əmlaklar yüklənərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<Property>();
            }
        }

        /// <summary>
        /// ID-yə görə əmlak obyektini qaytarır
        /// </summary>
        public async Task<Property> GetPropertyByIdAsync(int id)
        {
            try
            {
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    // Get the property
                    var property = await connection.QueryFirstOrDefaultAsync<Property>(
                        "SELECT * FROM Properties WHERE Id = @Id", new { Id = id });

                    if (property != null)
                    {
                        // Load associated images
                        var images = await connection.QueryAsync<PropertyImage>(
                            "SELECT * FROM PropertyImages WHERE PropertyId = @Id ORDER BY IsMainImage DESC",
                            new { Id = id });

                        property.Images = images.ToList();

                        // Load rental agreements
                        var rentalAgreements = await connection.QueryAsync<RentalAgreement>(
                            "SELECT * FROM RentalAgreements WHERE PropertyId = @Id ORDER BY StartDate DESC",
                            new { Id = id });

                        property.RentalAgreements = rentalAgreements.ToList();
                    }

                    return property;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Əmlak məlumatları yüklənərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// Əmlak obyektini əlavə edir və ya yeniləyir
        /// </summary>
        public async Task<int> SavePropertyAsync(Property property)
        {
            try
            {
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    // Update timestamp
                    property.UpdatedAt = DateTime.Now;

                    if (property.Id == 0)
                    {
                        // New property
                        property.CreatedAt = DateTime.Now;

                        string sql = @"
                            INSERT INTO Properties (
                                Title, Description, Price, Currency, Address, City, District,
                                Bedrooms, Bathrooms, Area, YearBuilt, Floor, TotalFloors,
                                PropertyType, ListingType, OwnerName, OwnerPhone, OwnerEmail,
                                OwnershipType, CommissionRate, IsActive, IsFeatured, IsVerified,
                                HasGarage, HasBalcony, HasElevator, HasFurniture, Source,
                                ExternalId, ExternalUrl, CreatedAt, UpdatedAt
                            ) VALUES (
                                @Title, @Description, @Price, @Currency, @Address, @City, @District,
                                @Bedrooms, @Bathrooms, @Area, @YearBuilt, @Floor, @TotalFloors,
                                @PropertyType, @ListingType, @OwnerName, @OwnerPhone, @OwnerEmail,
                                @OwnershipType, @CommissionRate, @IsActive, @IsFeatured, @IsVerified,
                                @HasGarage, @HasBalcony, @HasElevator, @HasFurniture, @Source,
                                @ExternalId, @ExternalUrl, @CreatedAt, @UpdatedAt
                            );
                            SELECT last_insert_rowid();";

                        property.Id = await connection.ExecuteScalarAsync<int>(sql, property);
                    }
                    else
                    {
                        // Update existing property
                        string sql = @"
                            UPDATE Properties SET
                                Title = @Title,
                                Description = @Description,
                                Price = @Price,
                                Currency = @Currency,
                                Address = @Address,
                                City = @City,
                                District = @District,
                                Bedrooms = @Bedrooms,
                                Bathrooms = @Bathrooms,
                                Area = @Area,
                                YearBuilt = @YearBuilt,
                                Floor = @Floor,
                                TotalFloors = @TotalFloors,
                                PropertyType = @PropertyType,
                                ListingType = @ListingType,
                                OwnerName = @OwnerName,
                                OwnerPhone = @OwnerPhone,
                                OwnerEmail = @OwnerEmail,
                                OwnershipType = @OwnershipType,
                                CommissionRate = @CommissionRate,
                                IsActive = @IsActive,
                                IsFeatured = @IsFeatured,
                                IsVerified = @IsVerified,
                                HasGarage = @HasGarage,
                                HasBalcony = @HasBalcony,
                                HasElevator = @HasElevator,
                                HasFurniture = @HasFurniture,
                                Source = @Source,
                                ExternalId = @ExternalId,
                                ExternalUrl = @ExternalUrl,
                                UpdatedAt = @UpdatedAt
                            WHERE Id = @Id";

                        await connection.ExecuteAsync(sql, property);
                    }

                    return property.Id;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Əmlak məlumatları saxlanılarkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }

        /// <summary>
        /// Əmlak obyektini silir
        /// </summary>
        public async Task<bool> DeletePropertyAsync(int id)
        {
            try
            {
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    // Check if there are any rental agreements
                    int rentalCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM RentalAgreements WHERE PropertyId = @Id", new { Id = id });

                    if (rentalCount > 0)
                    {
                        MessageBox.Show("Bu əmlak üçün mövcud kirayə müqavilələri var və silinə bilməz.",
                            "Xəbərdarlıq", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    // Check if there are any sale agreements
                    int saleCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM SaleAgreements WHERE PropertyId = @Id", new { Id = id });

                    if (saleCount > 0)
                    {
                        MessageBox.Show("Bu əmlak üçün mövcud satış müqavilələri var və silinə bilməz.",
                            "Xəbərdarlıq", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    // Delete the property images first
                    await connection.ExecuteAsync(
                        "DELETE FROM PropertyImages WHERE PropertyId = @Id", new { Id = id });

                    // Delete the property
                    await connection.ExecuteAsync(
                        "DELETE FROM Properties WHERE Id = @Id", new { Id = id });

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Əmlak silinərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Əmlakı arxivləyir (silmir)
        /// </summary>
        public async Task<bool> ArchivePropertyAsync(int id)
        {
            try
            {
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    await connection.ExecuteAsync(
                        "UPDATE Properties SET IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id",
                        new { Id = id, UpdatedAt = DateTime.Now });

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Əmlak arxivləşdirilərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Əmlakları müxtəlif parametrlərə görə axtarır
        /// </summary>
        public async Task<List<Property>> SearchPropertiesAsync(
            string searchTerm = null,
            string propertyType = null,
            string listingType = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? minRooms = null,
            int? maxRooms = null,
            int? minArea = null,
            int? maxArea = null,
            string district = null,
            bool activeOnly = true)
        {
            try
            {
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    var conditions = new List<string>();
                    var parameters = new DynamicParameters();

                    if (activeOnly)
                    {
                        conditions.Add("IsActive = 1");
                    }

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        conditions.Add("(Title LIKE @SearchTerm OR Description LIKE @SearchTerm OR Address LIKE @SearchTerm)");
                        parameters.Add("@SearchTerm", $"%{searchTerm}%");
                    }

                    if (!string.IsNullOrWhiteSpace(propertyType))
                    {
                        conditions.Add("PropertyType = @PropertyType");
                        parameters.Add("@PropertyType", propertyType);
                    }

                    if (!string.IsNullOrWhiteSpace(listingType))
                    {
                        conditions.Add("ListingType = @ListingType");
                        parameters.Add("@ListingType", listingType);
                    }

                    if (minPrice.HasValue)
                    {
                        conditions.Add("Price >= @MinPrice");
                        parameters.Add("@MinPrice", minPrice.Value);
                    }

                    if (maxPrice.HasValue)
                    {
                        conditions.Add("Price <= @MaxPrice");
                        parameters.Add("@MaxPrice", maxPrice.Value);
                    }

                    if (minRooms.HasValue)
                    {
                        conditions.Add("Bedrooms >= @MinRooms");
                        parameters.Add("@MinRooms", minRooms.Value);
                    }

                    if (maxRooms.HasValue)
                    {
                        conditions.Add("Bedrooms <= @MaxRooms");
                        parameters.Add("@MaxRooms", maxRooms.Value);
                    }

                    if (minArea.HasValue)
                    {
                        conditions.Add("Area >= @MinArea");
                        parameters.Add("@MinArea", minArea.Value);
                    }

                    if (maxArea.HasValue)
                    {
                        conditions.Add("Area <= @MaxArea");
                        parameters.Add("@MaxArea", maxArea.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(district))
                    {
                        conditions.Add("District = @District");
                        parameters.Add("@District", district);
                    }

                    string sql = "SELECT * FROM Properties";

                    if (conditions.Any())
                    {
                        sql += " WHERE " + string.Join(" AND ", conditions);
                    }

                    sql += " ORDER BY CreatedAt DESC";

                    var properties = await connection.QueryAsync<Property>(sql, parameters);

                    // Load associated images for each property
                    foreach (var property in properties)
                    {
                        var images = await connection.QueryAsync<PropertyImage>(
                            "SELECT * FROM PropertyImages WHERE PropertyId = @Id ORDER BY IsMainImage DESC",
                            new { Id = property.Id });

                        property.Images = images.ToList();
                    }

                    return properties.ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Əmlaklar axtarılarkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<Property>();
            }
        }

        /// <summary>
        /// Property listing obyektlərini əmlak obyektlərinə çevirir və saxlayır
        /// </summary>
        public async Task<int> ImportPropertyListingsAsync(List<PropertyListing> listings)
        {
            try
            {
                int importedCount = 0;
                ImageService imageService = new ImageService();

                foreach (var listing in listings)
                {
                    // Convert listing to property
                    var property = listing.ToProperty();

                    // Save property to database
                    int propertyId = await SavePropertyAsync(property);

                    if (propertyId > 0)
                    {
                        importedCount++;

                        // Import images
                        if (listing.ImageUrls != null && listing.ImageUrls.Count > 0)
                        {
                            await imageService.DownloadImagesInBulkAsync(propertyId, listing.ImageUrls, true);
                        }
                    }
                }

                return importedCount;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Əmlak elanları idxal edilərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }

        /// <summary>
        /// Əmlak növlərini qaytarır
        /// </summary>
        public List<string> GetPropertyTypes()
        {
            return new List<string>
            {
                "Mənzil",
                "Ev / Villa",
                "Torpaq",
                "Kommersiya",
                "Ofis",
                "Qaraj"
            };
        }

        /// <summary>
        /// Elan növlərini qaytarır
        /// </summary>
        public List<string> GetListingTypes()
        {
            return new List<string>
            {
                "Satış",
                "Kirayə"
            };
        }

        /// <summary>
        /// Mülkiyyət növlərini qaytarır
        /// </summary>
        public List<string> GetOwnershipTypes()
        {
            return new List<string>
            {
                "Şəxsi mülkiyyət",
                "Kupça",
                "Müqavilə",
                "Order",
                "Digər"
            };
        }
    }
}