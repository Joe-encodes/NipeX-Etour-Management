//User.cs

using System.Collections.Generic;

namespace e_tour_api.Data
{
    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique identifier for the user.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Username used for login.
        /// </summary>
        public required string Username { get; set; }

        /// <summary>
        /// Hashed password for security.
        /// </summary>
        public required string PasswordHash { get; set; }

        /// <summary>
        /// Email address of the user.
        /// </summary>
        public required string Email { get; set; }

        /// <summary>
        /// Role of the user (e.g., Admin, User).
        /// </summary>
        public required string Role { get; set; }

        /// <summary>
        /// Collection of documents associated with the user.
        /// </summary>
        public List<Document> Documents { get; set; } = new List<Document>(); // Add this to support the relationship
    }
}