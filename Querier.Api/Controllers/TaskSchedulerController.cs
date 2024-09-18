using Querier.Api.Models;
using Querier.Api.Models.Requests;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Models.Common;

namespace Querier.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class TaskSchedulerController : ControllerBase
    {
        private readonly IQTaskScheduler _taskScheduler;
        private readonly ILogger<TaskSchedulerController> _logger;
        private ApiDbContext _apidbContext;


        public TaskSchedulerController(IQTaskScheduler taskScheduler, ILogger<TaskSchedulerController> logger, ApiDbContext apidbContext)
        {
            _logger = logger;
            _apidbContext = apidbContext;
            _taskScheduler = taskScheduler;
        }

        [HttpPost("CreateOrUpdate")]
        public async Task<IActionResult> CreateOrUpdtateAsync(CreateOrUpdateScheduleJobRequest model)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(assembly => assembly.GetTypes())
                        .FirstOrDefault(t =>
                            typeof(IJob).IsAssignableFrom(t) &&
                            t.FullName == model.JobClass);

            try
            {
                //create a instance of the classe (model.JobClass)
                var instanceOfClass = Activator.CreateInstance(type);

                JobSchedule j = new JobSchedule((IJob)instanceOfClass, model.CronExpression, model.JobName, model.Description, model.Creator, model.Config);
                return new OkObjectResult(await _taskScheduler.CreateOrUpdateScheduledJobAsync(j));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "An error occurred while creating or updating the job.");
            }
        }

        [HttpDelete("Delete")]
        public async Task<IActionResult> DeleteAsync(DeleteScheduleJobRequest model)
        {
            return new JsonResult(new
            {
                Success = await _taskScheduler.DeleteScheduledJobAsync(model.JobName)
            }
            );
        }

        [HttpPost("GetJobs")]
        public async Task<IActionResult> GetJobsAsync([FromBody] GetAllJobsRequest request)
        {
            return new OkObjectResult(await _taskScheduler.GetJobsAsync(request.datatableRequest, request.ClientTimeZone));
        }

        [HttpGet("GetJob")]
        public async Task<IActionResult> GetJobAsync(string jobName, string timeZone)
        {
            return new OkObjectResult(await _taskScheduler.GetJobAsync(jobName, timeZone));
        }

        /// <summary>
        /// Used to get all the name of classes which have the interface IJob 
        /// </summary>
        /// <returns>Return a list of object which have the full name and the name of the classes </returns>
        [HttpGet("GetAllClassJobs")]
        public async Task<dynamic> GetAllClassJobs()
        {
            return Ok(await _taskScheduler.GetAllClassJobs());
        }

        [HttpGet("RunJob/{jobName}")]
        public async Task<IActionResult> RunJobAsync(string jobName)
        {
            return new JsonResult(new
            {
                Success = await _taskScheduler.RunJobAsync(jobName)
            }
            );
        }
    }
}
