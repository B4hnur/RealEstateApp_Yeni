using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using AForge.Imaging;
using AForge.Imaging.Filters;

namespace RealEstateApp.Utils
{
    /// <summary>
    /// Şəkillərin emalı və filigranların silinməsi üçün sinif
    /// </summary>
    public static class ImageProcessing
    {
        // Predefined watermark colors - common in real estate websites
        private static readonly Color[] WatermarkColors = new Color[]
        {
            Color.FromArgb(255, 255, 255),    // White
            Color.FromArgb(255, 0, 0),        // Red
            Color.FromArgb(0, 0, 0),          // Black
            Color.FromArgb(128, 128, 128),    // Gray
            Color.FromArgb(0, 0, 255),        // Blue
            Color.FromArgb(0, 128, 0)         // Green
        };

        /// <summary>
        /// Verilmiş şəkildən filigranı silir
        /// </summary>
        public static Bitmap RemoveWatermark(System.Drawing.Bitmap image)
        {
            try
            {
                // Create a copy of the image to work with
                Bitmap processedImage = new Bitmap(image);

                // Try to detect watermark areas
                List<Rectangle> watermarkAreas = DetectWatermarkAreas(processedImage);

                if (watermarkAreas.Count > 0)
                {
                    // Process each detected watermark area
                    foreach (var area in watermarkAreas)
                    {
                        // Use inpainting to remove the watermark
                        InpaintArea(processedImage, area);
                    }

                    return processedImage;
                }

                // If no specific watermark areas detected, try color filtering
                return RemoveWatermarkByColorFiltering(processedImage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing watermark: {ex.Message}");
                return image; // Return original image if processing fails
            }
        }

        /// <summary>
        /// Şəkildə filigran ola biləcək bölgələri aşkar edir
        /// </summary>
        private static List<Rectangle> DetectWatermarkAreas(System.Drawing.Bitmap image)
        {
            List<Rectangle> watermarkAreas = new List<Rectangle>();

            try
            {
                // Convert to grayscale for processing
                Grayscale grayscaleFilter = new Grayscale(0.2125, 0.7154, 0.0721);
                Bitmap grayscaleImage = grayscaleFilter.Apply(image);

                // Try to find text-like regions that could be watermarks
                // First, apply threshold to highlight potential watermark areas
                Threshold thresholdFilter = new Threshold(180);
                thresholdFilter.ApplyInPlace(grayscaleImage);

                // Use blob counter to find connected regions
                BlobCounter blobCounter = new BlobCounter
                {
                    MinHeight = 10,
                    MinWidth = 10,
                    FilterBlobs = true,
                    ObjectsOrder = ObjectsOrder.Size
                };

                blobCounter.ProcessImage(grayscaleImage);
                Blob[] blobs = blobCounter.GetObjectsInformation();

                // Get the largest blobs that might be watermarks
                foreach (var blob in blobs.Take(5)) // Consider top 5 largest blobs
                {
                    if (IsLikelyWatermark(image, blob.Rectangle))
                    {
                        // Add some padding around the detected area
                        Rectangle expandedRect = new Rectangle(
                            Math.Max(0, blob.Rectangle.X - 5),
                            Math.Max(0, blob.Rectangle.Y - 5),
                            Math.Min(image.Width - blob.Rectangle.X, blob.Rectangle.Width + 10),
                            Math.Min(image.Height - blob.Rectangle.Y, blob.Rectangle.Height + 10)
                        );

                        watermarkAreas.Add(expandedRect);
                    }
                }

                // Also check common watermark locations (corners)
                CheckCommonWatermarkLocations(image, watermarkAreas);

                // Dispose the temporary bitmap
                grayscaleImage.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error detecting watermark areas: {ex.Message}");
            }

            return watermarkAreas;
        }

        /// <summary>
        /// Ümumi filigran yerlərini (küncləri) yoxlayır
        /// </summary>
        private static void CheckCommonWatermarkLocations(System.Drawing.Bitmap image, List<Rectangle> watermarkAreas)
        {
            int cornerSize = Math.Min(image.Width, image.Height) / 6; // Typical corner watermark size

            // Define common watermark locations (corners)
            Rectangle[] commonLocations = new Rectangle[]
            {
                new Rectangle(0, 0, cornerSize, cornerSize), // Top-left
                new Rectangle(image.Width - cornerSize, 0, cornerSize, cornerSize), // Top-right
                new Rectangle(0, image.Height - cornerSize, cornerSize, cornerSize), // Bottom-left
                new Rectangle(image.Width - cornerSize, image.Height - cornerSize, cornerSize, cornerSize) // Bottom-right
            };

            foreach (var location in commonLocations)
            {
                if (IsLikelyWatermark(image, location))
                {
                    watermarkAreas.Add(location);
                }
            }
        }

        /// <summary>
        /// Bölgənin filigran olması ehtimalını yoxlayır
        /// </summary>
        private static bool IsLikelyWatermark(System.Drawing.Bitmap image, Rectangle area)
        {
            try
            {
                // Count pixels of common watermark colors
                int watermarkColorPixels = 0;
                int totalPixels = area.Width * area.Height;

                // Skip if the area is too large (more than 25% of the image)
                if (totalPixels > (image.Width * image.Height * 0.25))
                    return false;

                // Sample pixels in the area (check every 3rd pixel to improve performance)
                for (int y = area.Y; y < area.Y + area.Height; y += 3)
                {
                    for (int x = area.X; x < area.X + area.Width; x += 3)
                    {
                        if (x < 0 || y < 0 || x >= image.Width || y >= image.Height)
                            continue;

                        Color pixelColor = image.GetPixel(x, y);

                        // Check if the pixel color is close to any of our predefined watermark colors
                        if (WatermarkColors.Any(c => IsColorSimilar(pixelColor, c, 30)))
                        {
                            watermarkColorPixels++;
                        }
                    }
                }

                // Calculate the ratio of watermark-colored pixels
                double watermarkColorRatio = (double)watermarkColorPixels / (totalPixels / 9.0); // Divide by 9 because we sampled every 3rd pixel

                // If more than 15% of pixels match watermark colors, it's likely a watermark
                return watermarkColorRatio > 0.15;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// İki rəngin oxşar olmasını yoxlayır
        /// </summary>
        private static bool IsColorSimilar(Color c1, Color c2, int tolerance)
        {
            return Math.Abs(c1.R - c2.R) <= tolerance &&
                   Math.Abs(c1.G - c2.G) <= tolerance &&
                   Math.Abs(c1.B - c2.B) <= tolerance;
        }

        /// <summary>
        /// Təyin edilmiş bölgəni "inpainting" metodu ilə bərpa edir (filigranı silir)
        /// </summary>
        private static void InpaintArea(System.Drawing.Bitmap image, Rectangle area)
        {
            try
            {
                // Create a mask for the area to inpaint
                Bitmap mask = new Bitmap(image.Width, image.Height);
                using (Graphics g = Graphics.FromImage(mask))
                {
                    g.Clear(Color.Black); // Black means no inpainting
                    using (SolidBrush brush = new SolidBrush(Color.White)) // White means inpaint
                    {
                        g.FillRectangle(brush, area);
                    }
                }

                // Apply simple content-aware fill by averaging surrounding pixels
                for (int y = area.Y; y < area.Y + area.Height; y++)
                {
                    for (int x = area.X; x < area.X + area.Width; x++)
                    {
                        if (x < 0 || y < 0 || x >= image.Width || y >= image.Height)
                            continue;

                        // Get a sample of surrounding pixels
                        List<Color> surroundingColors = new List<Color>();
                        int sampleRadius = 5;

                        for (int sy = Math.Max(0, y - sampleRadius); sy <= Math.Min(image.Height - 1, y + sampleRadius); sy++)
                        {
                            for (int sx = Math.Max(0, x - sampleRadius); sx <= Math.Min(image.Width - 1, x + sampleRadius); sx++)
                            {
                                // Only sample pixels outside the watermark area
                                if (!area.Contains(sx, sy) || (sx == x && sy == y))
                                {
                                    surroundingColors.Add(image.GetPixel(sx, sy));
                                }
                            }
                        }

                        if (surroundingColors.Count > 0)
                        {
                            // Calculate the average color of surrounding pixels
                            int avgR = (int)surroundingColors.Average(c => (int)c.R);
                            int avgG = (int)surroundingColors.Average(c => (int)c.G);
                            int avgB = (int)surroundingColors.Average(c => (int)c.B);

                            // Set the pixel to the average color
                            image.SetPixel(x, y, Color.FromArgb(avgR, avgG, avgB));
                        }
                    }
                }

                mask.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inpainting area: {ex.Message}");
            }
        }

        /// <summary>
        /// Rəng filterləmə yolu ilə şəkildən filigranları silir
        /// </summary>
        private static Bitmap RemoveWatermarkByColorFiltering(System.Drawing.Bitmap image)
        {
            try
            {
                Bitmap resultImage = new Bitmap(image);

                // For each common watermark color, try to remove it
                foreach (Color watermarkColor in WatermarkColors)
                {
                    // Create a color filter
                    EuclideanColorFiltering filter = new EuclideanColorFiltering
                    {
                        CenterColor = new AForge.Imaging.RGB(watermarkColor.R, watermarkColor.G, watermarkColor.B),
                        Radius = 30, // Tolerance for color detection
                        FillColor = new AForge.Imaging.RGB(0, 0, 0), // Black for replacement
                        FillOutside = false // Fill inside the color range (the watermark)
                    };

                    // Apply the filter to find watermark colors
                    Bitmap filteredImage = filter.Apply(new Bitmap(resultImage));

                    // Convert filtered image to binary (black and white)
                    Threshold threshold = new Threshold(1);
                    threshold.ApplyInPlace(filteredImage);

                    // If any watermark pixels were detected
                    if (GetNonBlackPixelCount(filteredImage) > 0)
                    {
                        // Dilate to ensure we cover the full watermark
                        Dilatation dilatation = new Dilatation();
                        dilatation.ApplyInPlace(filteredImage);

                        // Use filtered image as a mask for inpainting
                        for (int y = 0; y < image.Height; y++)
                        {
                            for (int x = 0; x < image.Width; x++)
                            {
                                Color maskPixel = filteredImage.GetPixel(x, y);

                                // If the pixel is white in the mask, it's a watermark pixel
                                if (maskPixel.R > 0 || maskPixel.G > 0 || maskPixel.B > 0)
                                {
                                    // Average surrounding non-watermark pixels
                                    Color avgColor = GetAveragePixelColor(resultImage, x, y, filteredImage);
                                    resultImage.SetPixel(x, y, avgColor);
                                }
                            }
                        }
                    }

                    filteredImage.Dispose();
                }

                return resultImage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in color filtering: {ex.Message}");
                return image;
            }
        }

        /// <summary>
        /// Şəkildəki qara olmayan piksellərin sayını qaytarır
        /// </summary>
        private static int GetNonBlackPixelCount(System.Drawing.Bitmap image)
        {
            int count = 0;

            // Sample every 5th pixel to improve performance
            for (int y = 0; y < image.Height; y += 5)
            {
                for (int x = 0; x < image.Width; x += 5)
                {
                    Color pixel = image.GetPixel(x, y);
                    if (pixel.R > 0 || pixel.G > 0 || pixel.B > 0)
                    {
                        count++;
                    }
                }
            }

            return count * 25; // Approximate total by multiplying by sampling factor
        }

        /// <summary>
        /// Ətraf piksellərin orta rəngini hesablayır (filigran olmayan)
        /// </summary>
        private static Color GetAveragePixelColor(System.Drawing.Bitmap image, int x, int y, System.Drawing.Bitmap maskImage)
        {
            List<Color> colors = new List<Color>();
            int radius = 5;

            for (int sy = Math.Max(0, y - radius); sy <= Math.Min(image.Height - 1, y + radius); sy++)
            {
                for (int sx = Math.Max(0, x - radius); sx <= Math.Min(image.Width - 1, x + radius); sx++)
                {
                    // Only consider pixels that are not watermark pixels
                    Color maskPixel = maskImage.GetPixel(sx, sy);
                    if (maskPixel.R == 0 && maskPixel.G == 0 && maskPixel.B == 0) // Black = not a watermark pixel
                    {
                        colors.Add(image.GetPixel(sx, sy));
                    }
                }
            }

            if (colors.Count == 0)
            {
                return image.GetPixel(x, y); // If no surrounding non-watermark pixels, keep original
            }

            // Calculate average color
            int avgR = (int)colors.Average(c => (int)c.R);
            int avgG = (int)colors.Average(c => (int)c.G);
            int avgB = (int)colors.Average(c => (int)c.B);

            return Color.FromArgb(avgR, avgG, avgB);
        }

        /// <summary>
        /// Şəkildən byte array yaradır
        /// </summary>
        public static byte[] ImageToByteArray(System.Drawing.Image image, ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, format);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Byte array'dan şəkil yaradır
        /// </summary>
        public static System.Drawing.Image ByteArrayToImage(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return System.Drawing.Image.FromStream(ms);
            }
        }

        /// <summary>
        /// Şəkili yenidən ölçüləndirir
        /// </summary>
        public static Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                using (var wrapMode = new System.Drawing.Imaging.ImageAttributes())
                {
                    wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
