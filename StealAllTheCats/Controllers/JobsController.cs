using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StealAllTheCats.Data;
using StealAllTheCats.DTOs;
using StealAllTheCats.Services;

namespace StealAllTheCats.Controllers
{
    [ApiController]
    [Route("api/jobs")]
    public class JobsController : ControllerBase
    {
        /// <summary>
        /// Checks the status of a background job by its ID.
        /// </summary>
        /// <param name="id">The job ID returned when the job was enqueued by FetchCatsAsync.</param>
        /// <returns>Returns the job status if found; otherwise returns 404 Not Found.</returns>
        /// <response code="200">Returns the job status.</response>
        /// <response code="404">If the job is not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(JobNotFoundResponse), StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        public IActionResult GetJobStatus(string id)
        {
            var jobDetails = JobStorage.Current.GetMonitoringApi().JobDetails(id);

            if (jobDetails == null)
            {
                var errorResponse = new JobNotFoundResponse
                {
                    Status = StatusCodes.Status404NotFound,
                    Message = $"Job with id {id} not found"
                };
                return NotFound(errorResponse);
            }

            var state = jobDetails.History?.FirstOrDefault()?.StateName;

            return Ok(new { JobId = id, Status = state });
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
    }
}
