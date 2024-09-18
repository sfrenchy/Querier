namespace Querier.Api.Models.Requests
{
    public class CreateOrUpdateScheduleJobRequest
    {
        public string CronExpression { get; set; }
        public string JobName { get; set; }
        public string Description { get; set; }
        public string Creator { get; set; }
        public string JobClass { get; set; }
        public dynamic Config { get; set; }
    }

    public class DeleteScheduleJobRequest
    {
        public string JobName { get; set; }
    }
}
