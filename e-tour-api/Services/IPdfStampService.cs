using System.Threading.Tasks;

namespace e_tour_api.Services
{
    public interface IPdfStampService
    {
        /// <summary>
        /// Takes an existing PDF and writes a text stamp (e.g. “Approved by: ...”) on page 1,
        /// saving to a new file path.
        /// Returns the path of the stamped PDF.
        /// </summary>
        Task<string> StampAsync(string sourcePdfPath, string destPdfPath, string stampText);
    }
}