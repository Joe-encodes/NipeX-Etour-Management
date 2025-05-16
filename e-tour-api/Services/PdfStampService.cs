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
    public class PdfStampService : IPdfStampService
    {
        private readonly ILogger<PdfStampService> _logger;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public PdfStampService(ILogger<PdfStampService> logger, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async Task<string> StampAsync(string sourcePdfPath, string destPdfPath, string stampText)
        {
            try
            {
				return await Task.Run(() => { 
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
						// Read source PDF
						using (var reader = new PdfReader(sourcePdfPath))
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