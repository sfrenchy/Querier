using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Services.User;
using Querier.Api.Domain.Entities;
using Querier.Api.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Http;
using Querier.Api.Domain.Exceptions;

public interface ISQLQueryService
{
    Task<IEnumerable<SQLQuery>> GetAllQueriesAsync(string userId);
    Task<SQLQuery> GetQueryByIdAsync(int id);
    Task<SQLQuery> CreateQueryAsync(SQLQuery query);
    Task<SQLQuery> UpdateQueryAsync(SQLQuery query);
    Task DeleteQueryAsync(int id);
    Task<IEnumerable<dynamic>> ExecuteQueryAsync(int id, Dictionary<string, object> parameters);
}

public class SQLQueryService : ISQLQueryService
{
    private readonly ApiDbContext _context;
    private readonly IUserService _userService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SQLQueryService(
        ApiDbContext context, 
        IUserService userService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<SQLQuery>> GetAllQueriesAsync(string userId)
    {
        return await _context.SQLQueries
            .Where(q => q.IsPublic || q.CreatedBy == userId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<SQLQuery> GetQueryByIdAsync(int id)
    {
        return await _context.SQLQueries.FindAsync(id);
    }

    public async Task<SQLQuery> CreateQueryAsync(SQLQuery query)
    {
        var currentUser = await _userService.GetCurrentUser(_httpContextAccessor.HttpContext.User);
        query.CreatedAt = DateTime.UtcNow;
        query.CreatedBy = currentUser?.Id;
        
        _context.SQLQueries.Add(query);
        await _context.SaveChangesAsync();
        
        return query;
    }

    public async Task<SQLQuery> UpdateQueryAsync(SQLQuery query)
    {
        var existingQuery = await _context.SQLQueries.FindAsync(query.Id);
        if (existingQuery == null) return null;

        existingQuery.Name = query.Name;
        existingQuery.Description = query.Description;
        existingQuery.Query = query.Query;
        existingQuery.IsPublic = query.IsPublic;
        existingQuery.Parameters = query.Parameters;
        existingQuery.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingQuery;
    }

    public async Task DeleteQueryAsync(int id)
    {
        var query = await _context.SQLQueries.FindAsync(id);
        if (query != null)
        {
            _context.SQLQueries.Remove(query);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<dynamic>> ExecuteQueryAsync(int id, Dictionary<string, object> parameters)
    {
        var query = await GetQueryByIdAsync(id);
        if (query == null) throw new NotFoundException("Query not found");

        // Sécuriser l'exécution de la requête
        // TODO: Ajouter une validation/sanitization de la requête

        using (var command = _context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = query.Query;
            command.CommandType = CommandType.Text;

            foreach (var param in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = param.Key;
                parameter.Value = param.Value;
                command.Parameters.Add(parameter);
            }

            await _context.Database.OpenConnectionAsync();
            
            using (var result = await command.ExecuteReaderAsync())
            {
                var data = new List<dynamic>();
                while (await result.ReadAsync())
                {
                    var row = new ExpandoObject() as IDictionary<string, object>;
                    for (var i = 0; i < result.FieldCount; i++)
                    {
                        row.Add(result.GetName(i), result.GetValue(i));
                    }
                    data.Add(row);
                }
                return data;
            }
        }
    }
} 