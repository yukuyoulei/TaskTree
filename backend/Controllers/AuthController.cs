using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TaskManagerAPI.Data;
using TaskManagerAPI.Dtos;
using TaskManagerAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;

namespace TaskManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // This endpoint is for the initial administrator setup only.
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if any user already exists. If so, initial setup is complete.
            if (await _context.Users.AnyAsync())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Initial setup already completed. New users must be registered by an administrator." });
            }

            // Check for username conflict (should not happen if DB is empty, but good practice)
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                return BadRequest(new { message = "Username already exists." });
            }
            
            if (!string.IsNullOrEmpty(registerDto.Email) && await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                return BadRequest(new { message = "Email already exists." });
            }

            var user = new User
            {
                Username = registerDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Email = registerDto.Email,
                RealName = registerDto.RealName,
                Role = "Admin", // First user is always Admin
                IsFirstUser = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userResponse = new UserResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                RealName = user.RealName,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
            // It's conventional to return the created resource, possibly with a location header.
            // For an auth endpoint, returning the user DTO or a success message is also common.
            return CreatedAtAction(nameof(GetUserByIdForAuth), new { userId = user.UserId }, userResponse);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }

            var token = GenerateJwtToken(user);
            var userResponse = new UserResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                RealName = user.RealName,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return Ok(new LoginResponseDto { Token = token, User = userResponse });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKeyString = jwtSettings["SecretKey"];
            if (string.IsNullOrEmpty(secretKeyString))
            {
                throw new InvalidOperationException("JWT Secret Key is not configured or is empty.");
            }
            var secretKey = Encoding.ASCII.GetBytes(secretKeyString);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddDays(7), // Token expiration
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // This is a dummy action to satisfy CreatedAtAction. It refers to UsersController.GetUserById.
        // The route name in UsersController's GetUserById should be used if possible, or this can be removed if not strictly needed.
        [HttpGet("dummy/{userId}", Name = "GetUserByIdForAuth")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult GetUserByIdForAuth(int userId)
        {
            // This is just a placeholder to make CreatedAtAction work if GetUserById is in another controller.
            // Ideally, CreatedAtAction should point to the actual GET endpoint for a user.
            // If UsersController.GetUserById is named "GetUserById", that name should be used.
            // For now, this is a local placeholder.
            return Ok(new { message = "This is a dummy endpoint for CreatedAtAction reference." });
        }
    }
}

