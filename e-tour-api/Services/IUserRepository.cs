using System.Threading.Tasks;
using e_tour_api.Data;

namespace e_tour_api.Services
{
    /// <summary>
    /// Interface for user repository operations.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Creates a new user asynchronously.
        /// </summary>
        /// <param name="user">User entity to create</param>
        /// <returns>True if creation succeeded, otherwise false</returns>
        Task<bool> CreateUserAsync(User user);

        /// <summary>
        /// Retrieves a user by username asynchronously.
        /// </summary>
        /// <param name="username">Username to search for</param>
        /// <returns>User entity if found, otherwise null</returns>
        Task<User?> GetUserByUsernameAsync(string username);
    }
}
