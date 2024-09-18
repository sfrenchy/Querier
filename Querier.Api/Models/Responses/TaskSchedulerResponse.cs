using Quartz;
using System;
using System.Collections.Generic;
using Querier.Api.Models.Common;

namespace Querier.Api.Models.Responses
{
    public class CreateOrUpdateScheduleJobResponse
    {
        public Job Job { get; set; }
    }

    public class ReadScheduleJobResponse
    {
        public Job Job { get; set; }
    }
}