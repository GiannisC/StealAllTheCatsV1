using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StealAllTheCats.Data;
using StealAllTheCats.DTOs;
using StealAllTheCats.Services;

namespace StealAllTheCats.Controllers
{
    [ApiController]
    [Route("api/cats")]
    public class CatsController : ControllerBase
    {
        private readonly ILogger<CatsController> _logger;
        private readonly DBContext _context;
        private readonly IBackgroundJobClient _backgroundJobs;

        public CatsController(DBContext context, IBackgroundJobClient backgroundJobs, ILogger<CatsController> logger)
        {
            _context = context;
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        /// <summary>
        /// Fetches cats from TheCatAPI and stores them in the database.
        /// </summary>
        /// <remarks>
        /// This endpoint enqueues a background job to fetch and store cats.
        /// </remarks>
        /// <returns>Returns 202 Accepted with the job ID.</returns>
        /// <response code="202">Job accepted and enqueued.</response>
        [HttpPost("fetch")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(JobAcceptedResponse), StatusCodes.Status202Accepted)]
        public IActionResult FetchCatsAsync()
        {
            var jobId = _backgroundJobs.Enqueue<CatFetcherJob>(job => job.FetchAndStoreCatsAsync());
            return Accepted(new JobAcceptedResponse { JobId = jobId });

        }

        /// <summary>
        /// Retrieves a specific cat by its internal database ID.
        /// </summary>
        /// <param name="id">The internal ID of the cat.</param>
        /// <returns>Returns 200 OK with the cat's details if found; otherwise returns 404 Not Found.</returns>
        /// <response code="200">Returns the requested cat.</response>
        /// <response code="404">If the cat is not found.</response>
        [HttpGet("{id:int}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(CatDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CatNotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCatById(int id)
        {
            var cat = await _context.Cats
             .Where(c => c.Id == id)
             .Select(c => new CatDTO
             {
                 Id = c.Id,
                 CatId = c.CatId,
                 Width = c.Width,
                 Height = c.Height,
                 ImagePath = c.ImagePath,
                 Created = c.Created,
                 Tags = c.Tags.Select(t => t.Name).ToList()
             })
             .FirstOrDefaultAsync();

            if (cat == null)
            {
                var errorResponse = new CatNotFoundResponse
                {
                    Status = StatusCodes.Status404NotFound,
                    Message = $"Cat with id {id} not found"
                };
                return NotFound(errorResponse);
            }

            return Ok(cat);
        }

        /// <summary>
        /// Returns a paginated list of cats filtered by tag.
        /// </summary>
        /// <param name="tag">Optional tag to filter results by.</param>
        /// <param name="page">Page number (default: 1).</param>
        /// <param name="pageSize">Number of items per page (default: 10).</param>
        /// <returns>A paginated list of cat objects.</returns>
        /// <response code="200">Returns paginated list of cats.</response>
        /// <response code="400">If page or pageSize is invalid, or tag is too long.</response>
        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(GetCatsResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse400), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCats(string tag = "", int page = 1, int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
            {
                var errorResponse = new ErrorResponse400
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Page and PageSize must be greater than 0"
                };
                return BadRequest(errorResponse);
            }

            var query = _context.Cats.AsQueryable();

            if (!string.IsNullOrWhiteSpace(tag))
            {
                tag = tag.Trim();

                if (tag.Length > 50)
                {
                    var errorResponse = new ErrorResponse400
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Message = "Tag parameter too long (max 50 chars)"
                    };
                    return BadRequest(errorResponse);
                }

                query = query.Where(c => c.Tags.Any(t => t.Name.ToLower() == tag.ToLower()));
            }

            var total = await query.CountAsync();

            var cats = await query
                .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CatDTO
                {
                    Id = c.Id,
                    CatId = c.CatId,
                    Width = c.Width,
                    Height = c.Height,
                    ImagePath = c.ImagePath,
                    Created = c.Created,
                    Tags = c.Tags.Select(t => t.Name).ToList()
                })
                .ToListAsync();

            GetCatsResponseDTO response = new()
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Data = cats
            };

            return Ok(response);
        }


        public class JobStatusResponse
        {
            /// <summary>
            /// The unique job identifier.
            /// </summary>
            /// <example>1</example>
            public string JobId { get; set; } = string.Empty;

            /// <summary>
            /// The current status of the job (e.g., Enqueued, Processing, Succeeded, Failed).
            /// </summary>
            /// <example>Succeeded</example>
            public string Status { get; set; } = string.Empty;
        }

        public class JobNotFoundResponse
        {
            /// <summary>
            /// HTTP status code of the error.
            /// </summary>
            /// <example>404</example>
            public int Status { get; set; }
            /// <summary>
            /// Error message describing the issue.
            /// </summary>
            /// <example>Job with id 1 not found.</example>
            public string Message { get; set; } = string.Empty;
        }

        public class CatNotFoundResponse
        {
            /// <summary>
            /// HTTP status code of the error.
            /// </summary>
            /// <example>404</example>
            public int Status { get; set; }
            /// <summary>
            /// Error message describing the issue.
            /// </summary>
            /// <example>Cat with id 1 not found.</example>
            public string Message { get; set; } = string.Empty;
        }

        public class ErrorResponse400
        {
            /// <summary>
            /// HTTP status code of the error.
            /// </summary>
            /// <example>400</example>
            public int Status { get; set; }
            /// <summary>
            /// Error message describing the issue.
            /// </summary>
            /// <example>Page and PageSize must be greater than 0</example>
            public string Message { get; set; } = string.Empty;
        }

        public class JobAcceptedResponse
        {
            /// <example>1</example>
            public string JobId { get; set; } = string.Empty;
        }
    }
}
