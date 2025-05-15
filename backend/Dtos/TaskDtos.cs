// DTOs for Task Operations
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace TaskManagerAPI.Dtos
{
    public class TaskCreateDto
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "ToDo";

        [StringLength(20)]
        public string? Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public List<int> AssigneeIds { get; set; } = new List<int>();
    }

    public class TaskUpdateDto
    {
        [StringLength(255)]
        public string? Title { get; set; }

        public string? Content { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        [StringLength(20)]
        public string? Priority { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<int>? AssigneeIds { get; set; }
    }

    public class TaskResponseDto
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Priority { get; set; }
        public UserResponseDto? Creator { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<UserResponseDto> Assignees { get; set; } = new List<UserResponseDto>();
        // Add later: public List<TaskRelationshipDto> Relationships { get; set; } = new List<TaskRelationshipDto>();
    }

    // DTOs for Task Relationships
    public class TaskRelationshipDto
    {
        public int RelationshipId { get; set; }
        public int ParentTaskId { get; set; }
        public string ParentTaskTitle { get; set; } = string.Empty;
        public int ChildTaskId { get; set; }
        public string ChildTaskTitle { get; set; } = string.Empty;
        public string RelationshipType { get; set; } = string.Empty;
    }

    public class TaskRelationshipCreateDto
    {
        [Required]
        public int RelatedTaskId { get; set; }
        [Required]
        public string RelationshipType { get; set; } = "Related"; // e.g. "parent", "child", "related" - to be interpreted by API
                                                        // Or more specific: "Subtask", "Dependency"
    }
    
    public class TaskTreeDto : TaskResponseDto
    {
        public List<TaskTreeDto> Children { get; set; } = new List<TaskTreeDto>();
        public List<TaskTreeDto> Parents { get; set; } = new List<TaskTreeDto>(); // For context, if needed
    }

    public class MetadataDto
    {
        public List<string> TaskStatuses { get; set; } = new List<string>();
        public List<string> TaskPriorities { get; set; } = new List<string>();
    }
}

