using System.ComponentModel.DataAnnotations;

namespace TaskManagerAPI.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? RealName { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "User"; // Default role

        public bool IsFirstUser { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Task>? CreatedTasks { get; set; }
        public ICollection<TaskAssignee>? AssignedTasks { get; set; }
    }
}

