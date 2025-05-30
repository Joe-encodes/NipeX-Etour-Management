using System;

namespace e_tour_api.Models
{
    /// <summary>
    /// Document file data
    /// </summary>
    public class DocumentFile
    {
        public byte[] Contents { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/pdf";
    }
} 