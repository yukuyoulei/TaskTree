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
        public async Task<IActionResult> GetTasks(
            [FromQuery] string searchText = null,
            [FromQuery] string status = null,
            [FromQuery] string priority = null,
            [FromQuery] int? assigneeId = null,
            [FromQuery] int? creatorId = null,
            [FromQuery] string sortBy = null,
            [FromQuery] bool ascending = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // 获取当前用户ID
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId))
                {
                    return Unauthorized(new { message = "用户未登录或ID无效" });
                }

                // 构建查询
                var query = _context.Tasks
                    .Include(t => t.Creator)
                    .Include(t => t.Assignees)
                    .ThenInclude(ta => ta.User)
                    .AsQueryable();

                // 应用标题搜索过滤
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(t => t.Title.Contains(searchText));
                }

                // 应用状态过滤
                if (!string.IsNullOrWhiteSpace(priority))
                {
                    query = query.Where(t => t.Priority == priority);
                }

                // 应用优先级过滤
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(t => t.Status == status);
                }

                // 应用负责人过滤
                if (assigneeId.HasValue)
                {
                    query = query.Where(t => t.Assignees.Any(ta => ta.UserId == assigneeId.Value));
                }

                // 应用创建者过滤
                if (creatorId.HasValue)
                {
                    query = query.Where(t => t.CreatorId == creatorId.Value);
                }

                // 应用排序
                query = ApplySorting(query, sortBy, ascending);

                // 获取总记录数
                var totalCount = await query.CountAsync();

                // 应用分页
                var tasks = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // 转换为DTO
                var taskDtos = tasks.Select(t => new
                {
                    id = t.TaskId,
                    title = t.Title,
                    content = t.Content,
                    status = t.Status,
                    priority = t.Priority,
                    createdAt = t.CreatedAt,
                    updatedAt = t.UpdatedAt,
                    dueDate = t.DueDate,
                    completedAt = t.CompletedAt,
                    creator = new
                    {
                        id = t.Creator.UserId,
                        username = t.Creator.Username
                    },
                    assignees = t.Assignees.Select(ta => new
                    {
                        id = ta.User.UserId,
                        username = ta.User.Username
                    }).ToList()
                }).ToList();

                return Ok(new
                {
                    data = taskDtos,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取任务列表失败" });
            }
        }

        private IQueryable<Models.Task> ApplySorting(IQueryable<Models.Task> query, string sortBy, bool ascending)
        {
            switch (sortBy?.ToLower())
            {
                case "duedate":
                    return ascending
                        ? query.OrderBy(t => t.DueDate)
                        : query.OrderByDescending(t => t.DueDate);
                case "priority":
                    return ascending
                        ? query.OrderBy(t => t.Priority)
                        : query.OrderByDescending(t => t.Priority);
                case "createdat":
                    return ascending
                        ? query.OrderBy(t => t.CreatedAt)
                        : query.OrderByDescending(t => t.CreatedAt);
                case "updatedat":
                    return ascending
                        ? query.OrderBy(t => t.UpdatedAt)
                        : query.OrderByDescending(t => t.UpdatedAt);
                case "completedat":
                    return ascending
                        ? query.OrderBy(t => t.CompletedAt)
                        : query.OrderByDescending(t => t.CompletedAt);
                default:
                    // 默认按创建时间降序排序
                    return query.OrderByDescending(t => t.CreatedAt);
            }
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

