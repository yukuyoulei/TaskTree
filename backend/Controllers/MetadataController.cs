using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using TaskManagerAPI.Dtos;

namespace TaskManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires authentication to access metadata
    public class MetadataController : ControllerBase
    {
        // These could be read from a configuration file or a database in a more complex scenario
        private static readonly List<string> TaskStatuses = new List<string>
        {
            "待办",
            "进行中",
            "已完成",
            "已取消"
        };

        private static readonly List<string> TaskPriorities = new List<string>
        {
            "高",
            "中",
            "低"
        };

        [HttpGet("task-statuses")]
        public ActionResult<IEnumerable<string>> GetTaskStatuses()
        {
            return Ok(TaskStatuses);
        }

        [HttpGet("task-priorities")]
        public ActionResult<IEnumerable<string>> GetTaskPriorities()
        {
            return Ok(TaskPriorities);
        }

        [HttpGet] // GET api/metadata (combines both)
        public ActionResult<MetadataDto> GetMetadata()
        {
            return Ok(new MetadataDto
            {
                TaskStatuses = TaskStatuses,
                TaskPriorities = TaskPriorities
            });
        }
    }
}

