using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using e_tour_api.Data;

namespace e_tour_api.Services
{
    public class CleanupService : ICleanupService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CleanupService> _logger;
        private readonly string _uploadsPath;

        public CleanupService(AppDbContext context, ILogger<CleanupService> logger, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env, string? uploadsPath = null)
        {
            _context = context;
            _logger = logger;
            _uploadsPath = uploadsPath ?? Path.Combine(env.WebRootPath, "uploads");
        }

        public async Task<int> CleanupOrphanedFilesAsync(string? filter = null)
            {
            if (!string.IsNullOrEmpty(filter))
                {
                try
                {
                    new System.Text.RegularExpressions.Regex(filter);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException("Invalid filter pattern");
                }
            }

            var deletedCount = 0;
            var files = Directory.GetFiles(_uploadsPath, "*.pdf");
            var referencedFiles = await _context.Documents
                .Where(d => d.SignedFilePath != null)
                .Select(d => Path.GetFileName(d.SignedFilePath))
                .ToListAsync();

            foreach (var file in files)
                {
                var fileName = Path.GetFileName(file);
                if (!referencedFiles.Contains(fileName) && 
                    (string.IsNullOrEmpty(filter) || System.Text.RegularExpressions.Regex.IsMatch(fileName, filter)))
                {
                        try
                        {
                        File.Delete(file);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                        _logger.LogError(ex, "Error deleting file {FileName}", fileName);
                        }
                    }
                }

                return deletedCount;
        }
    }
}
