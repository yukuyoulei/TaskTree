using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskManagerAPI.Data;
using TaskManagerAPI.Dtos;
using TaskManagerAPI.Models;
using BCrypt.Net;
using System.ComponentModel.DataAnnotations; // Added this line

namespace TaskManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/users/register-by-admin (or similar)
        // This endpoint allows an admin to register a new user.
        [HttpPost("register-by-admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterUserByAdmin([FromBody] UserRegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
                Role = string.IsNullOrEmpty(registerDto.Role) ? "User" : registerDto.Role, 
                IsFirstUser = false, 
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
            return CreatedAtAction(nameof(GetUserById), new { userId = user.UserId }, userResponse);
        }


        [HttpGet]
        [Authorize(Roles = "Admin,User")] 
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var usersQuery = _context.Users
                .OrderBy(u => u.UserId);
            
            var totalUsers = await usersQuery.CountAsync();
            var users = await usersQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserResponseDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    RealName = u.RealName,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();
            
            return Ok(new { Users = users, TotalCount = totalUsers, Page = page, PageSize = pageSize });
        }

        [HttpGet("{userId}", Name = "GetUserById")] 
        [Authorize(Roles = "Admin,User")] 
        public async Task<ActionResult<UserResponseDto>> GetUserById(int userId)
        {
            //var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            //if (currentUserRole != "Admin" && currentUserIdStr != userId.ToString())
            //{
            //    return Forbid(); 
            //}

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(new UserResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                RealName = user.RealName,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            });
        }

        [HttpPut("{userId}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UserUpdateDto userUpdateDto)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            if (!int.TryParse(currentUserIdStr, out int currentUserId))
            {
                return Unauthorized(); 
            }

            if (currentUserRole != "Admin" && currentUserId != userId)
            {
                return Forbid(); 
            }

            var userToUpdate = await _context.Users.FindAsync(userId);
            if (userToUpdate == null)
            {
                return NotFound(new { message = "User not found." });
            }

            if (!string.IsNullOrWhiteSpace(userUpdateDto.Email))
            {
                if (userToUpdate.Email != userUpdateDto.Email && await _context.Users.AnyAsync(u => u.Email == userUpdateDto.Email && u.UserId != userId))
                {
                    return BadRequest(new { message = "Email already in use by another account." });
                }
                userToUpdate.Email = userUpdateDto.Email;
            }
            if (!string.IsNullOrWhiteSpace(userUpdateDto.RealName))
            {
                userToUpdate.RealName = userUpdateDto.RealName;
            }

            if (!string.IsNullOrWhiteSpace(userUpdateDto.Password))
            {
                userToUpdate.PasswordHash = BCrypt.Net.BCrypt.HashPassword(userUpdateDto.Password);
            }

            if (currentUserRole == "Admin" && !string.IsNullOrWhiteSpace(userUpdateDto.Role) && userUpdateDto.Role != userToUpdate.Role)
            {
                userToUpdate.Role = userUpdateDto.Role;
            }
            else if (!string.IsNullOrWhiteSpace(userUpdateDto.Role) && userUpdateDto.Role != userToUpdate.Role && currentUserRole != "Admin")
            {
                 return Forbid("Only administrators can change user roles.");
            }

            userToUpdate.UpdatedAt = DateTime.UtcNow;
            _context.Entry(userToUpdate).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.UserId == userId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return Ok(new UserResponseDto
            {
                UserId = userToUpdate.UserId,
                Username = userToUpdate.Username,
                Email = userToUpdate.Email,
                RealName = userToUpdate.RealName,
                Role = userToUpdate.Role,
                CreatedAt = userToUpdate.CreatedAt
            });
        }

        [HttpDelete("{userId}")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (user.IsFirstUser) 
            {
                return BadRequest(new { message = "Cannot delete the initial administrator account." });
            }
            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) 
            {
                return BadRequest(new { message = "Could not delete user. They might have associated tasks that need to be reassigned or deleted first.", details = ex.Message });
            }
            
            return NoContent();
        }
    }

    public class UserUpdateDto
    {
        [EmailAddress]
        public string? Email { get; set; }
        [StringLength(100)]
        public string? RealName { get; set; }
        [StringLength(100, MinimumLength = 6)]
        public string? Password { get; set; } 
        public string? Role { get; set; } 
    }
}

