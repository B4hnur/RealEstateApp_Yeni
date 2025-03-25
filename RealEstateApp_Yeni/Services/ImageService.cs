using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using RealEstateApp.Models;
using RealEstateApp.Utils;
using Dapper;

namespace RealEstateApp.Services
{
    /// <summary>
    /// Şəkillərin yüklənməsi, endirilməsi və emal edilməsi xidməti
    /// </summary>
    public class ImageService
    {
        private readonly string _imagesFolder;
        private readonly string _imageBackupFolder;

        public ImageService()
        {
            _imagesFolder = ConfigurationManager.AppSettings["ImagesFolder"] ?? "Images";
            _imageBackupFolder = ConfigurationManager.AppSettings["ImageBackupFolder"] ?? "ImageBackup";

            // Ensure directories exist
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Directory.CreateDirectory(Path.Combine(baseDirectory, _imagesFolder));
            Directory.CreateDirectory(Path.Combine(baseDirectory, _imageBackupFolder));
        }

        /// <summary>
        /// Verilmiş mülk üçün şəkil əlavə edir
        /// </summary>
        public async Task<PropertyImage> AddImageFromFileAsync(int propertyId, string filePath, bool isMainImage = false)
        {
            try
            {
                // Validate file exists
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Şəkil faylı tapılmadı", filePath);
                }

                // Load the image 
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    return await AddImageFromStreamAsync(propertyId, fileStream, Path.GetFileName(filePath), isMainImage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Şəkil əlavə edilərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        /// <summary>
        /// Verilmiş URL-dən şəkil endirir və mülkə əlavə edir
        /// </summary>
        public async Task<PropertyImage> AddImageFromUrlAsync(int propertyId, string imageUrl, bool isMainImage = false)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    using (Stream stream = await client.OpenReadTaskAsync(imageUrl))
                    {
                        string fileName = Path.GetFileName(imageUrl);
                        if (string.IsNullOrEmpty(fileName) || !fileName.Contains("."))
                        {
                            // Generate a name if URL doesn't contain valid filename
                            fileName = $"image_{DateTime.Now.Ticks}.jpg";
                        }

                        return await AddImageFromStreamAsync(propertyId, stream, fileName, isMainImage, imageUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"URL-dən şəkil endirilərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        /// <summary>
        /// Stream-dən şəkil əlavə edir
        /// </summary>
        public async Task<PropertyImage> AddImageFromStreamAsync(int propertyId, Stream stream, string originalFileName,
            bool isMainImage = false, string externalUrl = null)
        {
            try
            {
                // Load image from stream
                Image originalImage = Image.FromStream(stream);

                // Generate a unique filename
                string extension = Path.GetExtension(originalFileName);
                if (string.IsNullOrEmpty(extension))
                {
                    extension = ".jpg"; // Default to jpg if no extension
                }

                string fileName = $"property_{propertyId}_{DateTime.Now.Ticks}{extension}";
                string contentType = GetContentType(extension);

                // Save the image to disk
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _imagesFolder, fileName);
                originalImage.Save(imagePath, GetImageFormat(extension));

                // Create the image record
                var propertyImage = new PropertyImage
                {
                    PropertyId = propertyId,
                    FileName = fileName,
                    OriginalFileName = originalFileName,
                    ContentType = contentType,
                    FileExtension = extension,
                    IsMainImage = isMainImage,
                    IsWatermarkRemoved = false,
                    Source = externalUrl != null ? "URL" : "Manual",
                    ExternalUrl = externalUrl,
                    UploadedAt = DateTime.Now,
                    ImageData = ImageProcessing.ImageToByteArray(originalImage, GetImageFormat(extension))
                };

                // Save to database
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();

                    // If this is main image, clear other main image flags
                    if (isMainImage)
                    {
                        await connection.ExecuteAsync(
                            "UPDATE PropertyImages SET IsMainImage = 0 WHERE PropertyId = @PropertyId",
                            new { PropertyId = propertyId });
                    }

                    // Insert the new image
                    string sql = @"
                        INSERT INTO PropertyImages (
                            PropertyId, FileName, OriginalFileName, ContentType, FileExtension, 
                            ImageData, IsMainImage, IsWatermarkRemoved, Source, ExternalUrl, UploadedAt
                        ) VALUES (
                            @PropertyId, @FileName, @OriginalFileName, @ContentType, @FileExtension,
                            @ImageData, @IsMainImage, @IsWatermarkRemoved, @Source, @ExternalUrl, @UploadedAt
                        );
                        SELECT last_insert_rowid();";

                    propertyImage.Id = await connection.ExecuteScalarAsync<int>(sql, propertyImage);
                }

                return propertyImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Şəkil əlavə edilərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        /// <summary>
        /// Şəkildən filigranı silir
        /// </summary>
        public async Task<bool> RemoveWatermarkAsync(int imageId)
        {
            try
            {
                PropertyImage image = null;

                // Get the image from database
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();
                    image = await connection.QueryFirstOrDefaultAsync<PropertyImage>(
                        "SELECT * FROM PropertyImages WHERE Id = @Id", new { Id = imageId });
                }

                if (image == null)
                {
                    throw new Exception("Şəkil tapılmadı");
                }

                // Convert byte array to image
                using (var originalImage = (Bitmap)ImageProcessing.ByteArrayToImage(image.ImageData))
                {
                    // Make a backup of the original image
                    string backupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _imageBackupFolder,
                        $"original_{image.FileName}");
                    originalImage.Save(backupPath, GetImageFormat(image.FileExtension));

                    // Process the image to remove watermark
                    using (var processedImage = ImageProcessing.RemoveWatermark(originalImage))
                    {
                        // Save the processed image back to disk
                        string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _imagesFolder, image.FileName);
                        processedImage.Save(imagePath, GetImageFormat(image.FileExtension));

                        // Update the database record
                        byte[] newImageData = ImageProcessing.ImageToByteArray(processedImage, GetImageFormat(image.FileExtension));

                        using (var connection = DatabaseService.GetConnection())
                        {
                            connection.Open();
                            await connection.ExecuteAsync(
                                "UPDATE PropertyImages SET ImageData = @ImageData, IsWatermarkRemoved = 1 WHERE Id = @Id",
                                new { ImageData = newImageData, Id = imageId });
                        }

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Filigran silinərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Verilmiş mülk üçün bütün şəkillərə filigran silmə tətbiq edir
        /// </summary>
        public async Task<int> RemoveWatermarksFromPropertyAsync(int propertyId)
        {
            try
            {
                List<PropertyImage> images;

                // Get all images for the property
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();
                    images = (await connection.QueryAsync<PropertyImage>(
                        "SELECT * FROM PropertyImages WHERE PropertyId = @PropertyId AND IsWatermarkRemoved = 0",
                        new { PropertyId = propertyId })).ToList();
                }

                int successCount = 0;

                // Process each image
                foreach (var image in images)
                {
                    if (await RemoveWatermarkAsync(image.Id))
                    {
                        successCount++;
                    }
                }

                return successCount;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Əmlakın şəkillərindən filigran silinərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }

        /// <summary>
        /// Verilmiş list-dəki URL-lərdən şəkilləri endirir və mülkə əlavə edir
        /// </summary>
        public async Task<int> DownloadImagesInBulkAsync(int propertyId, List<string> imageUrls, bool removeWatermarks = true)
        {
            try
            {
                int successCount = 0;
                bool firstImage = true;

                foreach (string url in imageUrls)
                {
                    try
                    {
                        // Download and add the image
                        var image = await AddImageFromUrlAsync(propertyId, url, firstImage);

                        // Remove watermark if requested
                        if (removeWatermarks)
                        {
                            await RemoveWatermarkAsync(image.Id);
                        }

                        successCount++;
                        firstImage = false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"URL-dən şəkil endirilərkən xəta: {url}, Xəta: {ex.Message}");
                        // Continue with the next URL even if one fails
                    }
                }

                return successCount;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Şəkillər kütləvi endirilərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }

        /// <summary>
        /// Şəkil ID-sinə əsasən şəkli silir
        /// </summary>
        public async Task<bool> DeleteImageAsync(int imageId)
        {
            try
            {
                PropertyImage image;

                // Get the image details
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();
                    image = await connection.QueryFirstOrDefaultAsync<PropertyImage>(
                        "SELECT * FROM PropertyImages WHERE Id = @Id", new { Id = imageId });
                }

                if (image == null)
                {
                    return false;
                }

                // Delete the physical file
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _imagesFolder, image.FileName);
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }

                // Delete from database
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();
                    await connection.ExecuteAsync(
                        "DELETE FROM PropertyImages WHERE Id = @Id", new { Id = imageId });
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Şəkil silinərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Uzantıya görə content type təyin edir
        /// </summary>
        private string GetContentType(string extension)
        {
            switch (extension.ToLower())
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                case ".webp":
                    return "image/webp";
                default:
                    return "application/octet-stream";
            }
        }

        /// <summary>
        /// Uzantıya görə şəkil formatını təyin edir
        /// </summary>
        private ImageFormat GetImageFormat(string extension)
        {
            switch (extension.ToLower())
            {
                case ".jpg":
                case ".jpeg":
                    return ImageFormat.Jpeg;
                case ".png":
                    return ImageFormat.Png;
                case ".gif":
                    return ImageFormat.Gif;
                case ".bmp":
                    return ImageFormat.Bmp;
                default:
                    return ImageFormat.Jpeg; // Default to JPEG
            }
        }
    }
}