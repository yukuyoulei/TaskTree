using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerAPI.Models
{
    public class Task
    {
        public int TaskId { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "ToDo"; // Default status

        [StringLength(20)]
        public string? Priority { get; set; }

        public int CreatorId { get; set; }
        [ForeignKey("CreatorId")]
        public User? Creator { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public ICollection<TaskAssignee>? Assignees { get; set; }
        public ICollection<TaskRelationship>? ParentRelationships { get; set; } // Tasks where this task is the child
        public ICollection<TaskRelationship>? ChildRelationships { get; set; } // Tasks where this task is the parent
    }
}

