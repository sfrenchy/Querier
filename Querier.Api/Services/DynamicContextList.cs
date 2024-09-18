using System.Collections.Generic;
using Querier.Api.Models.Interfaces;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Services
{
    public class DynamicContextList : IDynamicContextList
    {
        private readonly  Dictionary<string, IDynamicContextProceduresServicesResolver> _dynamicContexts;
        private static DynamicContextList _instance;
        private DynamicContextList()
        {
             _dynamicContexts =  new Dictionary<string, IDynamicContextProceduresServicesResolver>();
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
        public  Dictionary<string, IDynamicContextProceduresServicesResolver> DynamicContexts
        {
            get
            {
                return _dynamicContexts;
            }
        }
    }
}