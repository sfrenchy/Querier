using System;
using System.Collections.Generic;
using System.Linq;
using Querier.Api.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Querier.Api.Services
{
    public class DynamicContextResolver : IDynamicContextResolver
    {
        public DbContext GetContextByName(string name)
        {
            Type contextType = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(a => a.GetTypes()).First(t => typeof(DbContext).IsAssignableFrom(t) && t.Name.Contains(name));
                        
            var constructor = contextType.GetConstructor(Type.EmptyTypes);
            DbContext dynamicContext = (DbContext)constructor.Invoke(null);
            
            return dynamicContext;
        }
    }
}