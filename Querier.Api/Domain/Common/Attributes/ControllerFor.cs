using System;

namespace Querier.Api.Domain.Common.Attributes
{
    public class ControllerFor : Attribute
    {
        public string Table { get; set; }
    }
}