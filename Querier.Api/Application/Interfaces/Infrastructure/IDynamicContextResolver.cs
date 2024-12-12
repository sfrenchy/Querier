using System;
using Microsoft.EntityFrameworkCore;

namespace Querier.Api.Application.Interfaces.Infrastructure
{
    public interface IDynamicContextResolver
    {
        DbContext GetContextByName(string name);
    }
}