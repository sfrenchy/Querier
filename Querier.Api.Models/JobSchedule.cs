using Quartz;
using System;

namespace Querier.Api.Models
{
    public class JobSchedule
    {
        public JobSchedule(IJob job, string cronExpression, string jobName, string description, string creator, dynamic config)
        {
            Job = job;
            CronExpression = cronExpression;
            JobName = jobName;
            Description = description;
            Creator = creator;
            Config = config;
        }

        public IJob Job { get; }
        public string CronExpression { get; }
        public string JobName { get; }
        public string Description { get; }
        public string Creator { get; }
        public dynamic Config { get; }
    }
}
