using System.Threading.Tasks;

namespace e_tour_api.Services
{
    /// <summary>
    /// Interface for PDF document stamping operations.
    /// </summary>
    public interface IPdfStampService
    {
        /// <summary>
        /// Takes an existing PDF and writes a text stamp (e.g. "Approved by: ...") on page 1,
        /// saving to a new file path.
        /// Returns the path of the stamped PDF.
        /// </summary>
        /// <param name="sourcePdfPath">Path to the source PDF file</param>
        /// <param name="destPdfPath">Path where the stamped PDF will be saved</param>
        /// <param name="stampText">Text to be stamped on the PDF</param>
        /// <param name="password">Optional password for the PDF file</param>
        /// <returns>The path of the stamped PDF file</returns>
        Task<string> StampAsync(string sourcePdfPath, string destPdfPath, string stampText, string? password = null);
    }
}
