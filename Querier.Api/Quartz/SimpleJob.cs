using Quartz;
using System;
using System.Threading.Tasks;

namespace Querier.Api.Quartz
{
    public class SimpleJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            JobKey key = context.JobDetail.Key;

            var creator = context.MergedJobDataMap.GetString("Creator");

            await Console.Out.WriteLineAsync("Instance " + key + "  --- Greetings from SimpleJob! --- the creator is " + creator);
        }
    }
    public class SimpleJobSecond : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            JobKey key = context.JobDetail.Key;

            var creator = context.MergedJobDataMap.GetString("Creator");

            await Console.Out.WriteLineAsync("Instance " + key + "  --- Greetings from SimpleJobSecond!!!! --- the creator is " + creator);
        }
    }
}
