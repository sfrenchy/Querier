using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Querier.Api.Models.Common
{
    public class Trigger
    {
        public string CronExpressionString { get; set; }
        public string Name { get; set;}
        public string Group { get; set; }
        public string Description { get; set; }
    }

    public class Job
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public string Description { get; set; }
        public dynamic JobType { get; set; }
        public dynamic JobDataMap { get; set; }
        public DateTime? PreviousFireTime { get; set; }
        public DateTime? NextFireTime { get; set; }
        public Trigger Trigger { get; set; }
        public string JobClass { get; set; }
    }
}
