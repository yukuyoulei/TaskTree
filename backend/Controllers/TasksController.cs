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
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All actions require authorization
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetTasks(
            [FromQuery] string? status, 
            [FromQuery] int? assigneeId, 
            [FromQuery] int? creatorId,
            [FromQuery] string? sortBy, // e.g., "dueDate", "priority", "createdAt"
            [FromQuery] bool ascending = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            IQueryable<Models.Task> query = _context.Tasks
                                                .Include(t => t.Creator)
                                                .Include(t => t.Assignees)!
                                                .ThenInclude(ta => ta.User);

            // 允许所有已登录用户查看任务列表，但非管理员只能看到与自己相关的任务
            // 如果需要限制普通用户只能看到与自己相关的任务，可以取消下面的注释
            // if (currentUserRole != "Admin")
            // {
            //     query = query.Where(t => t.CreatorId == currentUserId || t.Assignees!.Any(ta => ta.UserId == currentUserId));
            // }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }
            if (assigneeId.HasValue)
            {
                query = query.Where(t => t.Assignees!.Any(ta => ta.UserId == assigneeId.Value));
            }
            if (creatorId.HasValue)
            {
                query = query.Where(t => t.CreatorId == creatorId.Value);
            }

            // Sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "duedate":
                        query = ascending ? query.OrderBy(t => t.DueDate) : query.OrderByDescending(t => t.DueDate);
                        break;
                    case "priority": // Assuming priority can be ordered (e.g. High=1, Medium=2, Low=3)
                        // This requires a way to map priority string to an orderable value or a more complex sort
                        // For simplicity, we'll sort by string value, which might not be ideal for High/Medium/Low
                        query = ascending ? query.OrderBy(t => t.Priority) : query.OrderByDescending(t => t.Priority);
                        break;
                    case "createdat":
                    default:
                        query = ascending ? query.OrderBy(t => t.CreatedAt) : query.OrderByDescending(t => t.CreatedAt);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(t => t.CreatedAt); // Default sort
            }
            
            var totalTasks = await query.CountAsync();
            var tasks = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TaskResponseDto
                {
                    TaskId = t.TaskId,
                    Title = t.Title,
                    Content = t.Content,
                    Status = t.Status,
                    Priority = t.Priority,
                    Creator = t.Creator == null ? null : new UserResponseDto { UserId = t.Creator.UserId, Username = t.Creator.Username, Email = t.Creator.Email, RealName = t.Creator.RealName, Role = t.Creator.Role, CreatedAt = t.Creator.CreatedAt },
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    DueDate = t.DueDate,
                    CompletedAt = t.CompletedAt,
                    Assignees = t.Assignees == null ? new List<UserResponseDto>() : t.Assignees.Select(a => new UserResponseDto { UserId = a.User!.UserId, Username = a.User.Username, Email = a.User.Email, RealName = a.User.RealName, Role = a.User.Role, CreatedAt = a.User.CreatedAt }).ToList()
                })
                .ToListAsync();

            return Ok(new { Tasks = tasks, TotalCount = totalTasks, Page = page, PageSize = pageSize });
        }

        // GET: api/tasks/{taskId}
        [HttpGet("{taskId}")]
        public async Task<ActionResult<TaskResponseDto>> GetTaskById(int taskId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            var task = await _context.Tasks
                .Include(t => t.Creator)
                .Include(t => t.Assignees)!
                .ThenInclude(ta => ta.User)
                .FirstOrDefaultAsync(t => t.TaskId == taskId);

            if (task == null)
            {
                return NotFound();
            }

            // 允许所有已登录用户查看任务详情
            // 原来的权限检查已被移除，现在所有用户都可以查看任务详情

            return Ok(new TaskResponseDto
            {
                TaskId = task.TaskId,
                Title = task.Title,
                Content = task.Content,
                Status = task.Status,
                Priority = task.Priority,
                Creator = task.Creator == null ? null : new UserResponseDto { UserId = task.Creator.UserId, Username = task.Creator.Username, Email = task.Creator.Email, RealName = task.Creator.RealName, Role = task.Creator.Role, CreatedAt = task.Creator.CreatedAt },
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt,
                Assignees = task.Assignees == null ? new List<UserResponseDto>() : task.Assignees.Select(a => new UserResponseDto { UserId = a.User!.UserId, Username = a.User.Username, Email = a.User.Email, RealName = a.User.RealName, Role = a.User.Role, CreatedAt = a.User.CreatedAt }).ToList()
            });
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<ActionResult<TaskResponseDto>> CreateTask([FromBody] TaskCreateDto taskCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var creatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (creatorId == 0) return Unauthorized("User ID not found in token.");

            var task = new Models.Task
            {
                Title = taskCreateDto.Title,
                Content = taskCreateDto.Content,
                Status = taskCreateDto.Status,
                Priority = taskCreateDto.Priority,
                DueDate = taskCreateDto.DueDate,
                CreatorId = creatorId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync(); // Save to get TaskId

            if (taskCreateDto.AssigneeIds != null && taskCreateDto.AssigneeIds.Any())
            {
                foreach (var assigneeId in taskCreateDto.AssigneeIds)
                {
                    // Check if user exists before assigning
                    if (await _context.Users.AnyAsync(u => u.UserId == assigneeId))
                    {
                        _context.TaskAssignees.Add(new TaskAssignee { TaskId = task.TaskId, UserId = assigneeId });
                    }
                    // else: log warning or ignore? For now, ignore if user doesn't exist.
                }
                await _context.SaveChangesAsync();
            }
            
            // Reload task with includes for response
            var createdTask = await _context.Tasks
                .Include(t => t.Creator)
                .Include(t => t.Assignees)!
                .ThenInclude(ta => ta.User)
                .FirstOrDefaultAsync(t => t.TaskId == task.TaskId);

            return CreatedAtAction(nameof(GetTaskById), new { taskId = createdTask!.TaskId }, new TaskResponseDto
            {
                TaskId = createdTask.TaskId,
                Title = createdTask.Title,
                Content = createdTask.Content,
                Status = createdTask.Status,
                Priority = createdTask.Priority,
                Creator = createdTask.Creator == null ? null : new UserResponseDto { UserId = createdTask.Creator.UserId, Username = createdTask.Creator.Username },
                CreatedAt = createdTask.CreatedAt,
                UpdatedAt = createdTask.UpdatedAt,
                DueDate = createdTask.DueDate,
                Assignees = createdTask.Assignees == null ? new List<UserResponseDto>() : createdTask.Assignees.Select(a => new UserResponseDto { UserId = a.User!.UserId, Username = a.User.Username }).ToList()
            });
        }

        // PUT: api/tasks/{taskId}
        [HttpPut("{taskId}")]
        public async Task<IActionResult> UpdateTask(int taskId, [FromBody] TaskUpdateDto taskUpdateDto)
        {
            var taskToUpdate = await _context.Tasks.Include(t => t.Assignees).FirstOrDefaultAsync(t => t.TaskId == taskId);

            if (taskToUpdate == null)
            {
                return NotFound();
            }

            // 允许所有已登录用户更新任务
            // 获取当前用户ID仅用于记录
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            if (taskUpdateDto.Title != null) taskToUpdate.Title = taskUpdateDto.Title;
            if (taskUpdateDto.Content != null) taskToUpdate.Content = taskUpdateDto.Content;
            if (taskUpdateDto.Status != null) taskToUpdate.Status = taskUpdateDto.Status;
            if (taskUpdateDto.Priority != null) taskToUpdate.Priority = taskUpdateDto.Priority;
            if (taskUpdateDto.DueDate.HasValue) taskToUpdate.DueDate = taskUpdateDto.DueDate;
            if (taskUpdateDto.CompletedAt.HasValue) taskToUpdate.CompletedAt = taskUpdateDto.CompletedAt;
            
            taskToUpdate.UpdatedAt = DateTime.UtcNow;

            // Update assignees
            if (taskUpdateDto.AssigneeIds != null)
            {
                // Remove existing assignees not in the new list
                var assigneesToRemove = taskToUpdate.Assignees?.Where(a => !taskUpdateDto.AssigneeIds.Contains(a.UserId)).ToList();
                if (assigneesToRemove != null && assigneesToRemove.Any()) _context.TaskAssignees.RemoveRange(assigneesToRemove);

                // Add new assignees
                foreach (var assigneeId in taskUpdateDto.AssigneeIds)
                {
                    if (taskToUpdate.Assignees == null || !taskToUpdate.Assignees.Any(a => a.UserId == assigneeId))
                    { // If not already assigned
                        if (await _context.Users.AnyAsync(u => u.UserId == assigneeId))
                        {
                             _context.TaskAssignees.Add(new TaskAssignee { TaskId = taskId, UserId = assigneeId });
                        }
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Tasks.Any(e => e.TaskId == taskId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return NoContent();
        }

        // DELETE: api/tasks/{taskId}
        [HttpDelete("{taskId}")]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            var taskToDelete = await _context.Tasks.FindAsync(taskId);
            if (taskToDelete == null)
            {
                return NotFound();
            }

            // 允许所有已登录用户删除任务
            // 获取当前用户ID仅用于记录
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            bool isCreator = taskToDelete.CreatorId == currentUserId;
            // For delete, usually only creator or admin.
            if (currentUserRole != "Admin" && !isCreator)
            {
                return Forbid();
            }
            
            // TaskAssignees and TaskRelationships are set to Cascade delete in DbContext, so they will be removed automatically.
            _context.Tasks.Remove(taskToDelete);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

