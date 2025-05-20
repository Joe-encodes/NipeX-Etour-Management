using System.Threading.Tasks;

namespace e_tour_api.Services
{
    /// <summary>
    /// Interface for cleanup operations in the system.
    /// </summary>
    public interface ICleanupService
    {
        /// <summary>
        /// Cleans up orphaned files in the system.
        /// </summary>
        /// <param name="fileNameFilter">Optional filter to specify which files to clean up</param>
        /// <returns>The number of files that were cleaned up</returns>
        Task<int> CleanupOrphanedFilesAsync(string? fileNameFilter = null);
    }
}
