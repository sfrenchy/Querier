using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Infrastructure;

namespace Querier.Api.Domain.Services
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