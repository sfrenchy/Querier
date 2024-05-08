using System.Globalization;
using Querier.Api.Models;
using Querier.Api.Models.Common;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Responses;
using Querier.Tools;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Spi;
using static Quartz.Logging.OperationName;

namespace Querier.Api.Services
{
    public interface IHATaskScheduler
    {
        Task<CreateOrUpdateScheduleJobResponse> CreateOrUpdateScheduledJobAsync(JobSchedule jobSchedule);
        Task<bool> DeleteScheduledJobAsync(string jobName);
        Task<ServerSideResponse<Querier.Api.Models.Common.Job>> GetJobsAsync(ServerSideRequest datatableRequest, string clientTimeZone);
        Task<ReadScheduleJobResponse> GetJobAsync(string jobName, string timeZone);
        Task<dynamic> GetAllClassJobs();
        Task<bool> RunJobAsync(string jobName);
    }
    public class HATaskScheduler : IHATaskScheduler
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IJobFactory _jobFactory;


        public HATaskScheduler(ISchedulerFactory schedulerFactory, IJobFactory jobFactory)
        {
            _schedulerFactory = schedulerFactory;
            _jobFactory = jobFactory;
        }
        public IScheduler Scheduler { get; set; }

        public async Task<CreateOrUpdateScheduleJobResponse> CreateOrUpdateScheduledJobAsync(JobSchedule jobSchedule)
        {
            Scheduler = await _schedulerFactory.GetScheduler();
            Scheduler.JobFactory = _jobFactory;

            CreateOrUpdateScheduleJobResponse result = new CreateOrUpdateScheduleJobResponse();

            // Initialise lists of IJobDetail and ITrigger for processing
            List<IJobDetail> jobs = new List<IJobDetail>();
            List<ITrigger> listOftriggers = new List<ITrigger>();

            //Retrieve all existing job keys for a given scheduler
            foreach (JobKey jobKey in await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()))
            {
                jobs.Add(await Scheduler.GetJobDetail(jobKey));
                var triggers = (List<ITrigger>)await Scheduler.GetTriggersOfJob(jobKey);
                foreach (ITrigger trigger in triggers)
                {
                    listOftriggers.Add(trigger);
                }
            }
            //We store the IJobDetail if its name is identical to the task sent in parameter
            var targetJob = jobs.FirstOrDefault(j => j.Key.Name == jobSchedule.JobName);

            //If we find a job with the same ID name as the job passed as a parameter, we update its trigger.
            if (targetJob != null)
            {
                // We delete the previous job to create another.
                await Scheduler.DeleteJob(targetJob.Key);

                IJobDetail updateJob = JobBuilder
                   .Create(jobSchedule.Job.GetType())
                   .WithIdentity(jobSchedule.JobName)
                   .WithDescription(jobSchedule.Description)
                   .UsingJobData("Creator", jobSchedule.Creator)
                   .UsingJobData("Config", JsonConvert.SerializeObject(jobSchedule.Config))
                   .PersistJobDataAfterExecution()
                   .Build();

                var targetTrigger = listOftriggers.First(t => t.JobKey.Name == targetJob.Key.Name);

                ITrigger updateTrigger = TriggerBuilder
                    .Create()
                    .WithIdentity($"{jobSchedule.JobName}.trigger")
                    .WithCronSchedule(jobSchedule.CronExpression, x => x
                        .InTimeZone(TimeZoneInfo.Local))
                    .WithDescription(jobSchedule.CronExpression)
                    .UsingJobData("Creator", jobSchedule.Creator)
                    .UsingJobData("Config", JsonConvert.SerializeObject(jobSchedule.Config))
                    .Build();

                Trigger aTrigger = new Trigger();
                if (updateTrigger is ICronTrigger cronTrigger)
                {
                    aTrigger.CronExpressionString = cronTrigger.CronExpressionString;
                }
                aTrigger.Name = updateTrigger.Key.Name;
                aTrigger.Group = updateTrigger.Key.Group;
                aTrigger.Description = updateTrigger.Description;

                Querier.Api.Models.Common.Job aJob = new Querier.Api.Models.Common.Job();
                aJob.Name = targetJob.Key.Name;
                aJob.Group = targetJob.Key.Group;
                aJob.Description = targetJob.Description;
                aJob.JobType = targetJob.JobType;
                aJob.JobDataMap = targetJob.JobDataMap;
                aJob.PreviousFireTime = null;
                aJob.NextFireTime = null;

                result.Job = aJob;
                result.Job.Trigger = aTrigger;

                //await Scheduler.RescheduleJob(targetTrigger.Key, updateTrigger);
                await Scheduler.ScheduleJob(updateJob, updateTrigger);

                return result;

            }
            //Otherwise we create another job
            else
            {
                IJobDetail newJob = JobBuilder
                   .Create(jobSchedule.Job.GetType())
                   .WithIdentity(jobSchedule.JobName)
                   .WithDescription(jobSchedule.Description)
                   .UsingJobData("Creator", jobSchedule.Creator)
                   .UsingJobData("Config", JsonConvert.SerializeObject(jobSchedule.Config))
                   .PersistJobDataAfterExecution()
                   .Build();

                ITrigger newTrigger = TriggerBuilder
                   .Create()
                   .WithIdentity($"{jobSchedule.JobName}.trigger")
                   .WithCronSchedule(jobSchedule.CronExpression, x => x
                        .InTimeZone(TimeZoneInfo.Local))
                   .WithDescription(jobSchedule.CronExpression)
                   .UsingJobData("Creator", jobSchedule.Creator)
                   .UsingJobData("Config", JsonConvert.SerializeObject(jobSchedule.Config))
                   .Build();

                Trigger aTrigger = new Trigger();
                if (newTrigger is ICronTrigger cronTrigger)
                {
                    aTrigger.CronExpressionString = cronTrigger.CronExpressionString;
                }
                aTrigger.Name = newTrigger.Key.Name;
                aTrigger.Group = newTrigger.Key.Group;
                aTrigger.Description = newTrigger.Description;

                var nextFireTimeUtc = newTrigger.GetNextFireTimeUtc();
                var lastFireTimeUtc = newTrigger.GetPreviousFireTimeUtc();

                Querier.Api.Models.Common.Job aJob = new Querier.Api.Models.Common.Job();
                aJob.Name = newJob.Key.Name;
                aJob.Group = newJob.Key.Group;
                aJob.Description = newJob.Description;
                aJob.JobType = newJob.JobType;
                aJob.JobDataMap = newJob.JobDataMap;
                aJob.PreviousFireTime = null;
                aJob.NextFireTime = null;
                aJob.Trigger = aTrigger;

                result.Job = aJob;
                result.Job.Trigger = aTrigger;

                await Scheduler.ScheduleJob(newJob, newTrigger);
                return result;
            }
        }

        public async Task<bool> DeleteScheduledJobAsync(string jobName)
        {
            Scheduler = await _schedulerFactory.GetScheduler();
            Scheduler.JobFactory = _jobFactory;

            bool result = false;

            List<IJobDetail> jobs = new List<IJobDetail>();

            //Retrieve all existing job keys for a given scheduler
            foreach (JobKey jobKey in await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()))
            {
                jobs.Add(await Scheduler.GetJobDetail(jobKey));
            }
            var targetJob = jobs.FirstOrDefault(j => j.Key.Name == jobName);

            //return true if the job has been found and delete all triggers associated with the job
            if (targetJob != null)
                result = await Scheduler.DeleteJob(targetJob.Key);
            else
                result = false;

            return result;
        }

        public async Task<ServerSideResponse<Querier.Api.Models.Common.Job>> GetJobsAsync(ServerSideRequest datatableRequest, string clientTimeZone)
        {
            Scheduler = await _schedulerFactory.GetScheduler();
            Scheduler.JobFactory = _jobFactory;

            List<Querier.Api.Models.Common.Job> jobs = new List<Querier.Api.Models.Common.Job>();
            ServerSideResponse<Querier.Api.Models.Common.Job> response = new ServerSideResponse<Querier.Api.Models.Common.Job>();

            //Retrieve all existing job keys for a given scheduler
            foreach (JobKey jobKey in await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()))
            {
                DateTime? dLast = new DateTime();
                DateTime? dNext = new DateTime();

                IJobDetail job = await Scheduler.GetJobDetail(jobKey);
                var triggers = (List<ITrigger>)await Scheduler.GetTriggersOfJob(jobKey);

                //a job necessarily has an associated trigger
                ITrigger trigger = triggers[0];

                Trigger aTrigger = new Trigger();
                if (trigger is ICronTrigger cronTrigger)
                {
                    aTrigger.CronExpressionString = cronTrigger.CronExpressionString;
                }
                aTrigger.Name = trigger.Key.Name;
                aTrigger.Group = trigger.Key.Group;
                aTrigger.Description = trigger.Description;

                //We will retrieve the current user's timezone
                string tZoneName = clientTimeZone;

                //we recover the previous and the next execution of a job 
                var nextFireTimeUtc = trigger.GetNextFireTimeUtc();
                var lastFireTimeUtc = trigger.GetPreviousFireTimeUtc();

                //retrieve the previous and next execution of a job and put the dates in the right time zone for the user
                dLast = lastFireTimeUtc == null ? null : DateTime.ParseExact(lastFireTimeUtc.FromTimeZone(tZoneName).ToString("dd/MM/yyyy HH:mm:ss"), "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                dNext = nextFireTimeUtc == null ? null : DateTime.ParseExact(nextFireTimeUtc.FromTimeZone(tZoneName).ToString("dd/MM/yyyy HH:mm:ss"), "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                Querier.Api.Models.Common.Job aJob = new Querier.Api.Models.Common.Job();
                aJob.Name = job.Key.Name;
                aJob.Group = job.Key.Group;
                aJob.Description = job.Description;
                aJob.JobType = job.JobType.Name;
                aJob.JobDataMap = new Dictionary<string, string>() { { "Creator", job.JobDataMap.GetString("Creator")! } };
                aJob.PreviousFireTime = dLast;
                aJob.NextFireTime = dNext;
                aJob.Trigger = aTrigger;

                jobs.Add(aJob);
            }

            response.sums = null;
            response.draw = datatableRequest.draw;
            response.data = jobs;

            response.recordsFiltered = jobs.Count;
            response.recordsTotal = jobs.Count;

            return response;
        }

        public async Task<ReadScheduleJobResponse> GetJobAsync(string jobName, string timeZone)
        {
            Scheduler = await _schedulerFactory.GetScheduler();
            Scheduler.JobFactory = _jobFactory;

            // Initialise lists of IJobDetail and ITrigger for processing
            List<IJobDetail> jobs = new List<IJobDetail>();
            List<ITrigger> listOftriggers = new List<ITrigger>();

            //Retrieve all existing job keys for a given scheduler
            foreach (JobKey jobKey in await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()))
            {
                jobs.Add(await Scheduler.GetJobDetail(jobKey));
                var triggers = (List<ITrigger>)await Scheduler.GetTriggersOfJob(jobKey);
                foreach (ITrigger trigger in triggers)
                {
                    listOftriggers.Add(trigger);
                }
            }
            //We store the IJobDetail if its name is identical to the task sent in parameter
            var targetJob = jobs.FirstOrDefault(j => j.Key.Name == jobName);
            var targetTrigger = listOftriggers.First(t => t.JobKey.Name == targetJob.Key.Name);

            ReadScheduleJobResponse result = new ReadScheduleJobResponse();
            if (targetJob != null)
            {
                DateTime? dLast = new DateTime();
                DateTime? dNext = new DateTime();

                //we recover the previous and the next execution of a job 
                var nextFireTimeUtc = targetTrigger.GetNextFireTimeUtc();
                var lastFireTimeUtc = targetTrigger.GetPreviousFireTimeUtc();

                //retrieve the previous and next execution of a job and put the dates in the right time zone for the user
                dLast = lastFireTimeUtc == null ? null : DateTime.ParseExact(lastFireTimeUtc.FromTimeZone(timeZone).ToString("dd/MM/yyyy HH:mm:ss"), "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                dNext = nextFireTimeUtc == null ? null : DateTime.ParseExact(nextFireTimeUtc.FromTimeZone(timeZone).ToString("dd/MM/yyyy HH:mm:ss"), "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                Trigger aTrigger = new Trigger();
                if (targetTrigger is ICronTrigger cronTrigger)
                {
                    aTrigger.CronExpressionString = cronTrigger.CronExpressionString;
                }
                aTrigger.Name = targetTrigger.Key.Name;
                aTrigger.Group = targetTrigger.Key.Group;
                aTrigger.Description = targetTrigger.Description;

                Querier.Api.Models.Common.Job aJob = new Querier.Api.Models.Common.Job();
                aJob.Name = targetJob.Key.Name;
                aJob.Group = targetJob.Key.Group;
                aJob.Description = targetJob.Description;
                aJob.JobType = targetJob.JobType;
                aJob.JobDataMap = targetJob.JobDataMap;
                aJob.PreviousFireTime = dLast;
                aJob.NextFireTime = dNext;

                result.Job = aJob;
                result.Job.Trigger = aTrigger;

                return result;
            }

            else
            {
                throw new ArgumentException("The job does no exist");
            }
        }

        public async Task<dynamic> GetAllClassJobs()
        {
            //retrieves all classes that have the IJob interface
            List<Type> jobsTypes = AppDomain.CurrentDomain.GetAssemblies()
                       .SelectMany(assembly => assembly.GetTypes())
                       .Where(t => typeof(IJob).IsAssignableFrom(t) && !(t.FullName.StartsWith("Quartz"))).ToList();

            //create a list of objects that have the full name and the name of the classes 
            List<object> ListClass = new List<object>();
            foreach (var type in jobsTypes)
            {
                object namesOfClass = new { FullName = type.FullName, Name = type.Name };
                ListClass.Add(namesOfClass);
            }

            dynamic response = ListClass;
            return response;
        }

        public async Task<bool> RunJobAsync(string jobName)
        {
            Scheduler = await _schedulerFactory.GetScheduler();
            Scheduler.JobFactory = _jobFactory;

            bool result = false;

            List<IJobDetail> jobs = new List<IJobDetail>();

            //Retrieve all existing job keys for a given scheduler
            foreach (JobKey jobKey in await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()))
            {
                jobs.Add(await Scheduler.GetJobDetail(jobKey));
            }
            var targetJob = jobs.FirstOrDefault(j => j.Key.Name == jobName);

            //return true if the job has been found and run it immediately
            if (targetJob != null)
            {
                // Create a trigger that fires immediately
                ITrigger trigger = TriggerBuilder
                    .Create()
                    .WithIdentity("immediateTrigger", "IMMEDIATELY")
                    .ForJob(targetJob)
                    .StartNow()
                    .Build();

                await Scheduler.ScheduleJob(trigger);
                result = true;
            }
            else
                result = false;

            return result;
        }
    }
}
