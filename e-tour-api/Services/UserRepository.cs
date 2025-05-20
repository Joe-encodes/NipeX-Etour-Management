using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using e_tour_api.Data;

namespace e_tour_api.Services
{
    /// <summary>
    /// Implementation of IUserRepository for user data operations.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes a new instance of the UserRepository class.
        /// </summary>
        /// <param name="context">Database context</param>
        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a new user asynchronously.
        /// </summary>
        /// <param name="user">User entity to create</param>
        /// <returns>True if creation succeeded</returns>
        public async Task<bool> CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Retrieves a user by username asynchronously.
        /// </summary>
        /// <param name="username">Username to search for</param>
        /// <returns>User entity if found, otherwise null</returns>
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }
    }
}
