using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapper;
using RealEstateApp.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Configuration;

namespace RealEstateApp.Services
{
    /// <summary>
    /// Hesabat və statistikanı idarə edən sinif
    /// </summary>
    public class ReportingService
    {
        private readonly string _reportsFolder;

        public ReportingService()
        {
            _reportsFolder = ConfigurationManager.AppSettings["ReportsFolder"] ?? "Reports";

            // Ensure reports directory exists
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Directory.CreateDirectory(Path.Combine(baseDirectory, _reportsFolder));
        }

        #region Financial Reports
        // (Finansal raporlar ile ilgili metodlar buraya eklenebilir. Örnek kodları yukarıda verilmiştir.)
        #endregion

        #region PDF Export

        /// <summary>
        /// Hesabatı PDF formatında ixrac edir
        /// </summary>
        public string ExportReportToPdf(DataTable reportData, string reportTitle, string[] columnNames = null)
        {
            try
            {
                if (reportData == null || reportData.Rows.Count == 0)
                {
                    throw new ArgumentException("Hesabat məlumatları boşdur");
                }

                // Generate a unique file name
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{reportTitle.Replace(" ", "_")}_{timestamp}.pdf";
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _reportsFolder, fileName);

                // Create a new PDF document
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    Document document = new Document(PageSize.A4, 50, 50, 50, 50);
                    PdfWriter writer = PdfWriter.GetInstance(document, fs);

                    document.Open();

                    // Add title
                    iTextSharp.text.Font titleFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 18);
                    Paragraph title = new Paragraph(reportTitle, titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    document.Add(title);

                    // Add date range if title contains date information
                    if (reportTitle.Contains("Hesabat"))
                    {
                        iTextSharp.text.Font dateFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 12);
                        Paragraph dateRange = new Paragraph($"Tarix: {DateTime.Now.ToString("dd.MM.yyyy")}", dateFont);
                        dateRange.Alignment = Element.ALIGN_CENTER;
                        document.Add(dateRange);
                    }

                    document.Add(new Paragraph(" ")); // Add some space

                    // Create the table
                    PdfPTable table = new PdfPTable(reportData.Columns.Count);
                    table.WidthPercentage = 100;

                    // Add headers
                    iTextSharp.text.Font headerFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 12);
                    for (int i = 0; i < reportData.Columns.Count; i++)
                    {
                        string columnName = columnNames != null && i < columnNames.Length
                            ? columnNames[i]
                            : reportData.Columns[i].ColumnName;

                        PdfPCell cell = new PdfPCell(new Phrase(columnName, headerFont));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.BackgroundColor = new BaseColor(240, 240, 240);
                        cell.Padding = 8;
                        table.AddCell(cell);
                    }

                    // Add data rows
                    iTextSharp.text.Font dataFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 10);
                    foreach (DataRow row in reportData.Rows)
                    {
                        for (int i = 0; i < reportData.Columns.Count; i++)
                        {
                            object value = row[i];
                            string displayValue = value.ToString();

                            // Format decimal values
                            if (value is decimal decimalValue)
                            {
                                displayValue = decimalValue.ToString("N2") + " AZN";
                            }

                            PdfPCell cell = new PdfPCell(new Phrase(displayValue, dataFont));

                            // Align numeric values to right
                            if (value is int || value is decimal || value is double)
                            {
                                cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                            }
                            else
                            {
                                cell.HorizontalAlignment = Element.ALIGN_LEFT;
                            }

                            cell.Padding = 5;
                            table.AddCell(cell);
                        }
                    }

                    document.Add(table);

                    // Add footer with timestamp
                    Paragraph footer = new Paragraph($"Yaradılma tarixi: {DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}");
                    footer.Alignment = Element.ALIGN_RIGHT;
                    document.Add(footer);

                    document.Close();
                }

                return filePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF ixrac edilərkən xəta baş verdi: {ex.Message}",
                    "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        #endregion
    }
}
