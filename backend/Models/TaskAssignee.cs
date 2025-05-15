using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerAPI.Models
{
    // Join table for Many-to-Many relationship between Tasks and Users (Assignees)
    public class TaskAssignee
    {
        public int TaskAssigneeId { get; set; }

        public int TaskId { get; set; }
        [ForeignKey("TaskId")]
        public Task? Task { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}

