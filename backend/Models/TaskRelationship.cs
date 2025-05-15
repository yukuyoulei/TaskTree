using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerAPI.Models
{
    public class TaskRelationship
    {
        public int RelationshipId { get; set; }

        public int ParentTaskId { get; set; }
        [ForeignKey("ParentTaskId")]
        public Task? ParentTask { get; set; }

        public int ChildTaskId { get; set; }
        [ForeignKey("ChildTaskId")]
        public Task? ChildTask { get; set; }

        [StringLength(50)]
        public string RelationshipType { get; set; } = "Related"; // e.g., "Subtask", "Dependency", "Related"
    }
}

