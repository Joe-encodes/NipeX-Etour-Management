using e_tour_api.Data;
using e_tour_api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using System;


namespace e_tour_api.Controllers
{
    /// <summary>
    /// Controller for authentication operations such as login and registration.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _repo;
        private readonly IConfiguration _configuration;

        public AuthController(IUserRepository repo, IConfiguration configuration)
        {
            _repo = repo;
            _configuration = configuration;
        }

        /// <summary>
        /// Logs in a user and returns a JWT token.
        /// </summary>
        /// <param name="model">Login credentials</param>
        /// <returns>JWT token and user role if successful</returns>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                var user = await _repo.GetUserByUsernameAsync(model.Username);

                if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                    return Unauthorized(new ProblemDetails { Detail = "Invalid username or password" });

                var token = GenerateJwtToken(user);
                return Ok(new Dictionary<string, object>
                {
                    { "token", token },
                    { "username", user.Username },
                    { "role", user.Role }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ProblemDetails { Detail = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="model">Registration details</param>
        /// <returns>Success message if registration is successful</returns>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                // Validate email format
                if (!IsValidEmail(model.Email))
                    return BadRequest(new ProblemDetails { Detail = "Invalid email format" });

                // Validate password strength
                if (model.Password.Length < 8)
                    return BadRequest(new ProblemDetails { Detail = "Password must be at least 8 characters" });

                // Validate role
                if (!IsValidRole(model.Role))
                    return BadRequest(new ProblemDetails { Detail = "Invalid role. Must be one of: User, Approver, Admin" });

                var existingUser = await _repo.GetUserByUsernameAsync(model.Username);
                if (existingUser != null)
                    return BadRequest(new ProblemDetails { Detail = "Username already exists" });

                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Email = model.Email,
                    Role = model.Role
                };

                await _repo.CreateUserAsync(user);

                return Ok("User registered successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ProblemDetails { Detail = $"Internal server error: {ex.Message}" });
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidRole(string role)
        {
            return role == "User" || role == "Approver" || role == "Admin";
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing"));
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    /// <summary>
    /// Login request model
    /// </summary>
    public class LoginModel
    {
        /// <example>john.doe</example>
        public required string Username { get; set; }

        /// <example>SecurePassword123!</example>
        public required string Password { get; set; }
    }

    /// <summary>
    /// Registration request model
    /// </summary>
    public class RegisterModel
    {
        /// <example>john.doe</example>
        public required string Username { get; set; }

        /// <example>SecurePassword123!</example>
        public required string Password { get; set; }

        /// <example>john.doe@example.com</example>
        public required string Email { get; set; }

        /// <example>User</example>
        public required string Role { get; set; }
    }
}
