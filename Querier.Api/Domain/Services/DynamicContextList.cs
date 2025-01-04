using System.Collections.Generic;
using Querier.Api.Application.Interfaces.Infrastructure;

namespace Querier.Api.Domain.Services
{
    public class DynamicContextList : IDynamicContextList
    {
        private static DynamicContextList _instance;
        private readonly Dictionary<string, IDynamicContextProceduresServicesResolver> _dynamicContexts;

        private DynamicContextList()
        {
            _dynamicContexts = new Dictionary<string, IDynamicContextProceduresServicesResolver>();
        }

        public static DynamicContextList Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new DynamicContextList();
                return _instance;
            }
        }

        public Dictionary<string, IDynamicContextProceduresServicesResolver> DynamicContexts
        {
            get
            {
                return _dynamicContexts;
            }
        }
    }
}