using e_tour_api.Data;
using e_tour_api.Services;
using e_tour_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using User = e_tour_api.Models.User;  // Use Models.User instead of Data.User
using e_tour_api.Configuration;
using e_tour_api.Utilities;  // Added for HashGenerator

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
        private readonly AppSettings _settings;
        private readonly ILogger<AuthController> _logger;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public AuthController(
            IUserRepository repo, 
            AppSettings settings, 
            ILogger<AuthController> logger,
            AppDbContext context,
            IEmailService emailService)
        {
            _repo = repo;
            _settings = settings;
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        /// <summary>
        /// Logs in a user and returns a JWT token.
        /// </summary>
        /// <param name="model">Login credentials</param>
        /// <returns>JWT token and user role if successful</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ServiceResult<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                var user = await _repo.GetUserByUsernameAsync(model.Username);
                if (user == null || !HashGenerator.VerifyPassword(model.Password, user.PasswordHash))
                {
                    return Unauthorized(ServiceResult<object>.Error("Invalid username or password", 401));
                }

                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                // Store refresh token in database
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow
                };
                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                // Set refresh token in HTTP-only cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("X-Refresh-Token", refreshToken, cookieOptions);

                // Set access token in HTTP-only cookie
                cookieOptions.Expires = DateTime.UtcNow.AddMinutes(15);
                Response.Cookies.Append("X-Access-Token", token, cookieOptions);

                var response = new LoginResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role
                };

                return Ok(ServiceResult<LoginResponse>.Ok(response, "Login successful"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Username}", model.Username);
                return StatusCode(500, ServiceResult<object>.Error("An error occurred during login", 500));
            }
        }

        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(ServiceResult<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                // Get refresh token from cookie only
                var refreshToken = Request.Cookies["X-Refresh-Token"];
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Unauthorized(ServiceResult<object>.Error("Refresh token is required", 401));
                }

                // Find the refresh token in the database
                var tokenEntity = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (tokenEntity == null)
                {
                    return Unauthorized(ServiceResult<object>.Error("Invalid refresh token", 401));
                }

                // Check if token has expired
                if (tokenEntity.ExpiryDate <= DateTime.UtcNow)
                {
                    // Remove expired token
                    _context.RefreshTokens.Remove(tokenEntity);
                    await _context.SaveChangesAsync();
                    return Unauthorized(ServiceResult<object>.Error("Refresh token has expired", 401));
                }

                // Check if token has been revoked
                if (tokenEntity.IsRevoked)
                {
                    return Unauthorized(ServiceResult<object>.Error("Refresh token has been revoked", 401));
                }

                // Check if token is being reused (potential token theft)
                if (tokenEntity.LastUsedAt.HasValue)
                {
                    // If token was used recently, it might be a replay attack
                    if ((DateTime.UtcNow - tokenEntity.LastUsedAt.Value).TotalMinutes < 1)
                    {
                        // Revoke all refresh tokens for this user as a security measure
                        var userTokens = await _context.RefreshTokens
                            .Where(rt => rt.UserId == tokenEntity.UserId)
                            .ToListAsync();
                        _context.RefreshTokens.RemoveRange(userTokens);
                        await _context.SaveChangesAsync();
                        return Unauthorized(ServiceResult<object>.Error("Security violation detected", 401));
                    }
                }

                var user = tokenEntity.User;
                var token = GenerateJwtToken(user);
                var newRefreshToken = GenerateRefreshToken();

                // Mark current token as used
                tokenEntity.LastUsedAt = DateTime.UtcNow;
                tokenEntity.IsRevoked = true;

                // Add new refresh token
                var newTokenEntity = new RefreshToken
                {
                    Token = newRefreshToken,
                    UserId = user.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    LastUsedAt = null,
                    IsRevoked = false
                };
                _context.RefreshTokens.Add(newTokenEntity);
                await _context.SaveChangesAsync();

                // Set new refresh token in HTTP-only cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("X-Refresh-Token", newRefreshToken, cookieOptions);

                // Set new access token in HTTP-only cookie
                cookieOptions.Expires = DateTime.UtcNow.AddMinutes(15);
                Response.Cookies.Append("X-Access-Token", token, cookieOptions);

                var response = new LoginResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role
                };

                return Ok(ServiceResult<LoginResponse>.Ok(response, "Token refreshed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while refreshing the token", 500));
            }
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        public IActionResult Logout()
        {
            try
            {
                // Clear the refresh token cookie
                Response.Cookies.Delete("X-Refresh-Token", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });

                // Clear the access token cookie
                Response.Cookies.Delete("X-Access-Token", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });

                return Ok(ServiceResult<object>.Ok(new { message = "Logged out successfully" }, "Logged out successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred during logout", 500));
            }
        }

        private string GenerateJwtToken(User user)
        {
            var key = Encoding.ASCII.GetBytes(_settings.Jwt.Key);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(_settings.Jwt.ExpiryMinutes),
                Issuer = _settings.Jwt.Issuer,
                Audience = _settings.Jwt.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            // Generate a cryptographically secure random token
            var randomBytes = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            
            // Add timestamp to prevent token reuse
            var timestamp = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            var tokenBytes = new byte[randomBytes.Length + timestamp.Length];
            Buffer.BlockCopy(randomBytes, 0, tokenBytes, 0, randomBytes.Length);
            Buffer.BlockCopy(timestamp, 0, tokenBytes, randomBytes.Length, timestamp.Length);
            
            return Convert.ToBase64String(tokenBytes);
        }

        /// <summary>
        /// Gets the profile of the currently authenticated user.
        /// </summary>
        /// <returns>User profile information</returns>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResult<UserProfile>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ServiceResult<object>.Error("Invalid user ID in token", 401));
                }

                var user = await _repo.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(ServiceResult<object>.Error("User not found", 404));
                }

                var profile = new UserProfile
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role
                };

                return Ok(ServiceResult<UserProfile>.Ok(profile, "Profile retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while retrieving the profile", 500));
            }
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="model">Registration details</param>
        /// <returns>Success message if registration is successful</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ServiceResult<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                var validationErrors = new List<string>();

                // Validate email format
                if (!IsValidEmail(model.Email))
                    validationErrors.Add("Invalid email format");

                // Validate password strength
                if (model.Password.Length < 8)
                    validationErrors.Add("Password must be at least 8 characters");

                // Validate role
                if (!IsValidRole(model.Role))
                    validationErrors.Add("Invalid role. Must be one of: User, Approver, Admin");

                if (validationErrors.Any())
                    return BadRequest(ServiceResult.Error("Validation failed", 400, new { errors = validationErrors }));

                var existingUser = await _repo.GetUserByUsernameAsync(model.Username);
                if (existingUser != null)
                    return BadRequest(ServiceResult.Error("Username already exists", 400));

                // Generate verification token
                var verificationToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                var verificationExpiry = DateTime.UtcNow.AddHours(24);

                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Email = model.Email,
                    Role = model.Role,
                    IsEmailVerified = false,
                    EmailVerificationToken = verificationToken,
                    EmailVerificationExpiry = verificationExpiry
                };

                await _repo.CreateUserAsync(user);

                // Generate tokens for auto-login
                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                // Store refresh token
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow
                };
                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                // Set cookies
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("X-Refresh-Token", refreshToken, cookieOptions);
                Response.Cookies.Append("X-Access-Token", token, cookieOptions);

                // Try to send verification email, but don't fail if it doesn't work
                try
                {
                    await _emailService.SendVerificationEmailAsync(user.Email, verificationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send verification email, but user was created successfully");
                    // Continue with the registration process
                }

                // Return user data with verification status
                return Ok(ServiceResult.Ok(new LoginResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    IsEmailVerified = false,
                    RequiresVerification = true
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user {Username}", model.Username);
                return StatusCode(500, ServiceResult.Error("An error occurred during registration", 500));
            }
        }

        [HttpPost("verify-email")]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == request.Token);
                if (user == null)
                    return NotFound(ServiceResult.Error("Invalid verification token", 404));

                if (user.EmailVerificationExpiry < DateTime.UtcNow)
                    return BadRequest(ServiceResult.Error("Verification token has expired", 400));

                user.IsEmailVerified = true;
                user.EmailVerificationToken = null;
                user.EmailVerificationExpiry = null;
                await _context.SaveChangesAsync();

                return Ok(ServiceResult.Ok(new { message = "Email verified successfully" }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email");
                return StatusCode(500, ServiceResult.Error("An error occurred while verifying email", 500));
            }
        }

        [HttpPost("resend-verification")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendVerification()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);
                
                if (user == null)
                    return NotFound(ServiceResult.Error("User not found", 404));

                if (user.IsEmailVerified)
                    return BadRequest(ServiceResult.Error("Email is already verified", 400));

                // Generate new verification token
                var verificationToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                user.EmailVerificationToken = verificationToken;
                user.EmailVerificationExpiry = DateTime.UtcNow.AddHours(24);
                await _context.SaveChangesAsync();

                // Send new verification email
                await _emailService.SendVerificationEmailAsync(user.Email, verificationToken);

                return Ok(ServiceResult.Ok(new { message = "Verification email sent" }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email");
                return StatusCode(500, ServiceResult.Error("An error occurred while resending verification email", 500));
            }
        }

        [HttpGet("verify-token")]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        public IActionResult VerifyToken()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ServiceResult.Error("Invalid token", 401));
                }

                return Ok(ServiceResult.Ok(new
                {
                    UserId = userId,
                    Username = username,
                    Role = role,
                    IsValid = true
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying token");
                return Unauthorized(ServiceResult.Error("Invalid token", 401));
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
            return role is "User" or "Approver" or "Admin";
        }
    }

    public class RefreshTokenRequest
    {
        public required string RefreshToken { get; set; }
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

    /// <summary>
    /// Login response model
    /// </summary>
    public class LoginResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public bool RequiresVerification { get; set; }
    }

    /// <summary>
    /// User profile model
    /// </summary>
    public class UserProfile
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class VerifyEmailRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
