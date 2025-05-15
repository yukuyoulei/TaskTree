using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskManagerAPI.Data;
using TaskManagerAPI.Dtos;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Controllers
{
    [Route("api/tasks/{taskId}/relationships")] // Nested route under tasks
    [ApiController]
    [Authorize]
    public class TaskRelationshipsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TaskRelationshipsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/tasks/{taskId}/relationships
        [HttpPost]
        public async Task<ActionResult<TaskRelationshipDto>> CreateTaskRelationship(int taskId, [FromBody] TaskRelationshipCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var task1 = await _context.Tasks.FindAsync(taskId);
            var task2 = await _context.Tasks.FindAsync(dto.RelatedTaskId);

            if (task1 == null || task2 == null)
            {
                return NotFound(new { message = "One or both tasks not found." });
            }

            if (taskId == dto.RelatedTaskId)
            {
                return BadRequest(new { message = "Cannot relate a task to itself." });
            }

            int parentId, childId;
            string relationshipTypeForDb = dto.RelationshipType;

            if (dto.RelationshipType.Equals("parent", StringComparison.OrdinalIgnoreCase))
            {
                parentId = dto.RelatedTaskId;
                childId = taskId;
                relationshipTypeForDb = "Subtask";
            }
            else if (dto.RelationshipType.Equals("child", StringComparison.OrdinalIgnoreCase))
            {
                parentId = taskId;
                childId = dto.RelatedTaskId;
                relationshipTypeForDb = "Subtask";
            }
            else
            {
                parentId = taskId;
                childId = dto.RelatedTaskId;
            }
            
            bool relationshipExists = await _context.TaskRelationships
                .AnyAsync(r => (r.ParentTaskId == parentId && r.ChildTaskId == childId) || 
                               (r.ParentTaskId == childId && r.ChildTaskId == parentId && relationshipTypeForDb == "Subtask"));
            
            if (relationshipExists)
            {
                return Conflict(new { message = "This relationship already exists." });
            }

            var relationship = new TaskRelationship
            {
                ParentTaskId = parentId,
                ChildTaskId = childId,
                RelationshipType = relationshipTypeForDb
            };

            _context.TaskRelationships.Add(relationship);
            await _context.SaveChangesAsync();

            await _context.Entry(relationship).Reference(r => r.ParentTask).LoadAsync();
            await _context.Entry(relationship).Reference(r => r.ChildTask).LoadAsync();

            var responseDto = new TaskRelationshipDto
            {
                RelationshipId = relationship.RelationshipId,
                ParentTaskId = relationship.ParentTaskId,
                ParentTaskTitle = relationship.ParentTask?.Title ?? "N/A",
                ChildTaskId = relationship.ChildTaskId,
                ChildTaskTitle = relationship.ChildTask?.Title ?? "N/A",
                RelationshipType = relationship.RelationshipType
            };

            return CreatedAtAction(nameof(GetTaskRelationshipById), new { taskId = taskId, relationshipId = relationship.RelationshipId }, responseDto);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskRelationshipDto>>> GetTaskRelationships(int taskId)
        {
            var taskExists = await _context.Tasks.AnyAsync(t => t.TaskId == taskId);
            if (!taskExists)
            {
                return NotFound(new { message = "Task not found." });
            }

            var relationships = await _context.TaskRelationships
                .Include(r => r.ParentTask)
                .Include(r => r.ChildTask)
                .Where(r => r.ParentTaskId == taskId || r.ChildTaskId == taskId)
                .Select(r => new TaskRelationshipDto
                {
                    RelationshipId = r.RelationshipId,
                    ParentTaskId = r.ParentTaskId,
                    ParentTaskTitle = r.ParentTask != null ? r.ParentTask.Title : "N/A",
                    ChildTaskId = r.ChildTaskId,
                    ChildTaskTitle = r.ChildTask != null ? r.ChildTask.Title : "N/A",
                    RelationshipType = r.RelationshipType
                })
                .ToListAsync();

            return Ok(relationships);
        }

        [HttpGet("{relationshipId}", Name = "GetTaskRelationshipById")]
        public async Task<ActionResult<TaskRelationshipDto>> GetTaskRelationshipById(int taskId, int relationshipId)
        {
            var relationship = await _context.TaskRelationships
                .Include(r => r.ParentTask)
                .Include(r => r.ChildTask)
                .FirstOrDefaultAsync(r => r.RelationshipId == relationshipId && (r.ParentTaskId == taskId || r.ChildTaskId == taskId));

            if (relationship == null)
            {
                return NotFound(new { message = "Relationship not found for this task." });
            }

            return Ok(new TaskRelationshipDto
            {
                RelationshipId = relationship.RelationshipId,
                ParentTaskId = relationship.ParentTaskId,
                ParentTaskTitle = relationship.ParentTask?.Title ?? "N/A",
                ChildTaskId = relationship.ChildTaskId,
                ChildTaskTitle = relationship.ChildTask?.Title ?? "N/A",
                RelationshipType = relationship.RelationshipType
            });
        }

        [HttpDelete("{relationshipId}")]
        public async Task<IActionResult> DeleteTaskRelationship(int taskId, int relationshipId)
        {
            var relationship = await _context.TaskRelationships.FindAsync(relationshipId);
            if (relationship == null)
            {
                return NotFound(new { message = "Relationship not found." });
            }

            if (relationship.ParentTaskId != taskId && relationship.ChildTaskId != taskId)
            {
                 return BadRequest(new { message = "Relationship does not involve the specified task."} );
            }

            _context.TaskRelationships.Remove(relationship);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        
        [HttpGet("tree")]
        public async Task<ActionResult<TaskTreeDto>> GetTaskTree(int taskId)
        {
            var task = await _context.Tasks
                .Include(t => t.Creator)
                .Include(t => t.Assignees)!.ThenInclude(a => a.User)
                .FirstOrDefaultAsync(t => t.TaskId == taskId);

            if (task == null)
            {
                return NotFound(new { message = "Task not found." });
            }

            var treeRoot = MapTaskToTreeDto(task);
            await LoadChildrenRecursive(treeRoot, new HashSet<int>());
            return Ok(treeRoot);
        }

        private TaskTreeDto MapTaskToTreeDto(Models.Task task) // Changed Task to Models.Task
        {
            return new TaskTreeDto
            {
                TaskId = task.TaskId,
                Title = task.Title,
                Content = task.Content,
                Status = task.Status,
                Priority = task.Priority,
                Creator = task.Creator == null ? null : new UserResponseDto { UserId = task.Creator.UserId, Username = task.Creator.Username },
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt,
                Assignees = task.Assignees?.Select(a => new UserResponseDto { UserId = a.User!.UserId, Username = a.User.Username }).ToList() ?? new List<UserResponseDto>(),
                Children = new List<TaskTreeDto>(),
                Parents = new List<TaskTreeDto>()
            };
        }

        private async System.Threading.Tasks.Task LoadChildrenRecursive(TaskTreeDto parentNode, HashSet<int> visited) // Changed Task to System.Threading.Tasks.Task for return type
        {
            if (visited.Contains(parentNode.TaskId)) return;
            visited.Add(parentNode.TaskId);

            var childRelations = await _context.TaskRelationships
                .Include(r => r.ChildTask).ThenInclude(ct => ct!.Creator)
                .Include(r => r.ChildTask).ThenInclude(ct => ct!.Assignees)!.ThenInclude(a => a.User)
                .Where(r => r.ParentTaskId == parentNode.TaskId && r.RelationshipType == "Subtask")
                .ToListAsync();

            foreach (var rel in childRelations)
            {
                if (rel.ChildTask != null && !visited.Contains(rel.ChildTask.TaskId))
                {
                    var childNode = MapTaskToTreeDto(rel.ChildTask); // Models.Task is inferred here due to rel.ChildTask type
                    parentNode.Children.Add(childNode);
                    await LoadChildrenRecursive(childNode, new HashSet<int>(visited));
                }
            }
        }
        
        private async System.Threading.Tasks.Task LoadParentsRecursive(TaskTreeDto childNode, HashSet<int> visited) // Changed Task to System.Threading.Tasks.Task for return type
        {
            if (visited.Contains(childNode.TaskId)) return;
            visited.Add(childNode.TaskId);

            var parentRelations = await _context.TaskRelationships
                .Include(r => r.ParentTask).ThenInclude(pt => pt!.Creator)
                .Include(r => r.ParentTask).ThenInclude(pt => pt!.Assignees)!.ThenInclude(a => a.User)
                .Where(r => r.ChildTaskId == childNode.TaskId && r.RelationshipType == "Subtask")
                .ToListAsync();

            foreach (var rel in parentRelations)
            {
                if (rel.ParentTask != null && !visited.Contains(rel.ParentTask.TaskId))
                {
                    var parentNode = MapTaskToTreeDto(rel.ParentTask); // Models.Task is inferred here
                    childNode.Parents.Add(parentNode);
                    await LoadParentsRecursive(parentNode, new HashSet<int>(visited)); 
                }
            }
        }
    }
}

