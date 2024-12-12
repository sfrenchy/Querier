using System;
using Microsoft.EntityFrameworkCore;

namespace Querier.Api.Models.Interfaces
{
    public interface IDynamicContextResolver
    {
        DbContext GetContextByName(string name);
    }
}