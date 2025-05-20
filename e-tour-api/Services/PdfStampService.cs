// PdfStampService.cs
// <summary>
// Service responsible for stamping (overlaying text) on existing PDF documents.
// </summary>

using System;
using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using Microsoft.Extensions.Logging;

namespace e_tour_api.Services
{
    /// <summary>
    /// Service responsible for stamping (overlaying text) on existing PDF documents.
    /// Provides functionality to add text stamps to PDF files while preserving the original content.
    /// </summary>
    public class PdfStampService : IPdfStampService
    {
        private readonly ILogger<PdfStampService> _logger;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        /// <summary>
        /// Initializes a new instance of the PdfStampService.
        /// </summary>
        /// <param name="logger">Logger for tracking PDF operations</param>
        /// <param name="env">Web hosting environment for file operations</param>
        public PdfStampService(ILogger<PdfStampService> logger, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Stamps text onto a PDF document and saves it to a new location.
        /// </summary>
        /// <param name="sourcePdfPath">Path to the source PDF file</param>
        /// <param name="destPdfPath">Path where the stamped PDF will be saved</param>
        /// <param name="stampText">Text to be stamped on the PDF</param>
        /// <param name="password">Optional password for the PDF file</param>
        /// <returns>The path to the stamped PDF file</returns>
        /// <exception cref="ArgumentNullException">Thrown when source or destination path is null or empty</exception>
        /// <exception cref="FileNotFoundException">Thrown when source PDF file is not found</exception>
        /// <exception cref="InvalidOperationException">Thrown when PDF has no pages or destination directory is null</exception>
        public async Task<string> StampAsync(string sourcePdfPath, string destPdfPath, string stampText, string? password = null)
        {
            try
            {
                return await Task.Run(() =>
                {
                    // Validate input paths
                    if (string.IsNullOrEmpty(sourcePdfPath))
                        throw new ArgumentNullException(nameof(sourcePdfPath));
                    if (string.IsNullOrEmpty(destPdfPath))
                        throw new ArgumentNullException(nameof(destPdfPath));

                    if (!File.Exists(sourcePdfPath))
                        throw new FileNotFoundException("Source PDF not found", sourcePdfPath);

                    // Ensure destination directory exists
                    var outputDir = Path.GetDirectoryName(destPdfPath);
                    if (!Directory.Exists(outputDir))
                    {
                        if (outputDir != null)
                        {
                            Directory.CreateDirectory(outputDir);
                        }
                        else
                        {
                            throw new InvalidOperationException("Destination directory is null");
                        }
                    }

                    // Use temporary file to prevent partial writes
                    var tempPath = Path.GetTempFileName();

                    try
                    {
                        // Prepare ReaderProperties with password if provided
                        var readerProperties = new iText.Kernel.Pdf.ReaderProperties();
                        if (!string.IsNullOrEmpty(password))
                        {
                            readerProperties.SetPassword(System.Text.Encoding.UTF8.GetBytes(password));
                        }

                        // Read source PDF with ReaderProperties
                        using (var reader = new PdfReader(sourcePdfPath, readerProperties))
                        {
                            // Write to temporary file first
                            using (var writer = new PdfWriter(tempPath))
                            using (var pdfDoc = new PdfDocument(reader, writer))
                            {
                                var page = pdfDoc.GetFirstPage();
                                if (page == null)
                                    throw new InvalidOperationException("PDF has no pages");

                                // Add stamp text
                                var canvas = new PdfCanvas(page);
                                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                                canvas.BeginText()
                                    .SetFontAndSize(font, 12)
                                    .MoveText(50, 50)
                                    .ShowText(stampText)
                                    .EndText()
                                    .Release();
                            }
                        }

                        // Move temporary file to final destination
                        if (File.Exists(destPdfPath))
                            File.Delete(destPdfPath);

                        File.Move(tempPath, destPdfPath);

                        _logger.LogInformation("Successfully stamped PDF: {Path}", destPdfPath);
                        return destPdfPath;
                    }
                    finally
                    {
                        // Clean up temporary file if it exists
                        if (File.Exists(tempPath))
                            File.Delete(tempPath);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stamping PDF from {Source} to {Destination}",
                    sourcePdfPath, destPdfPath);
                throw;
            }
        }
    }
}