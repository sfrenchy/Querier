using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Services.User;
using Querier.Api.Domain.Entities;
using Querier.Api.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Http;
using Querier.Api.Domain.Exceptions;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Common.Models;
using Querier.Api.Domain.Services;
using Querier.Api.Tools;
using System.Text.Json;
using Querier.Api.Domain.Common.ValueObjects;

public interface ISQLQueryService
{
    Task<IEnumerable<SQLQueryDTO>> GetAllQueriesAsync(string userId);
    Task<SQLQueryDTO> GetQueryByIdAsync(int id);
    Task<SQLQueryDTO> CreateQueryAsync(SQLQueryDTO query, Dictionary<string, object> sampleParameters = null);
    Task<SQLQueryDTO> UpdateQueryAsync(SQLQueryDTO query, Dictionary<string, object> sampleParameters = null);
    Task DeleteQueryAsync(int id);
    Task<PagedResult<dynamic>> ExecuteQueryAsync(int queryId, Dictionary<string, object> parameters, int pageNumber = 1, int pageSize = 0);
}

public class SQLQueryService : ISQLQueryService
{
    private readonly ApiDbContext _context;
    private readonly IUserService _userService;
    private readonly IEntityCRUDService _crudService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SQLQueryService(
        ApiDbContext context, 
        IUserService userService,
        IEntityCRUDService crudService, 
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userService = userService;
        _crudService = crudService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<SQLQueryDTO>> GetAllQueriesAsync(string userId)
    {
        var queries = await _context.SQLQueries
            .Where(q => q.IsPublic || q.CreatedBy == userId)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new SQLQueryDTO
            {
                Id = q.Id,
                Name = q.Name,
                Description = q.Description,
                Query = q.Query,
                CreatedBy = q.CreatedBy,
                CreatedAt = q.CreatedAt,
                LastModifiedAt = q.LastModifiedAt,
                IsPublic = q.IsPublic,
                Parameters = q.Parameters,
                DBConnectionId = q.ConnectionId
            })
            .ToListAsync();

        return queries;
    }

    public async Task<SQLQueryDTO> GetQueryByIdAsync(int id)
    {
        return SQLQueryDTO.FromEntity(await _context.SQLQueries
            .Include(q => q.Connection)
            .FirstOrDefaultAsync(q => q.Id == id));
    }

    public async Task<SQLQueryDTO> CreateQueryAsync(SQLQueryDTO query, Dictionary<string, object> sampleParameters = null)
    {
        var currentUser = await _userService.GetCurrentUser(_httpContextAccessor.HttpContext.User);
        query.CreatedAt = DateTime.UtcNow;
        query.CreatedBy = currentUser?.Id;
        query.DBConnection = DBConnectionDto.FromEntity(await _context.DBConnections.FindAsync(query.DBConnectionId));

        if (ValidateAndDescribeQuery(query, sampleParameters, out string outputDescription))
        {
            _context.SQLQueries.Add(new SQLQuery()
            {
                ConnectionId = query.DBConnectionId,
                Name = query.Name,
                Description = query.Description,
                Query = query.Query,
                CreatedBy = query.CreatedBy,
                CreatedAt = query.CreatedAt,
                LastModifiedAt = query.LastModifiedAt,
                IsPublic = query.IsPublic,
                Parameters = query.Parameters,
                OutputDescription = outputDescription,
            });
            await _context.SaveChangesAsync();
            query.OutputDescription = outputDescription;
            query.Id = _context.SQLQueries.Last().Id;
        }
        else
        {
            throw new Exception($"Unable to validate query: {outputDescription}");
        }
        
        return query;
    }

    private bool ValidateAndDescribeQuery(SQLQueryDTO query, Dictionary<string, object> sampleParameters, out string outputDescription)
    {
        try
        {
            if (query.DBConnection == null)
            {
                query.DBConnection = DBConnectionDto.FromEntity(_context.DBConnections.Find(query.DBConnectionId));
            }

            using (DbContext context = Utils.GetDbContextFromTypeName(query.DBConnection.ContextName))
            {
                List<DbParameter> parameters = new List<DbParameter>();
                if (sampleParameters != null)
                {
                    foreach (KeyValuePair<string, object> p in sampleParameters)
                    {
                        var command = context.Database.GetDbConnection().CreateCommand();
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = p.Key;
                        parameter.Value = p.Value ?? DBNull.Value;
                        parameters.Add(parameter);
                    }
                }

                DataTable dt = context.Database.RawSqlQuery(query.Query, parameters);
                
                var entityDefinition = new EntityDefinition
                {
                    Name = query.Name,
                    Properties = dt.Columns.Cast<DataColumn>()
                        .Select(column => new PropertyDefinition
                        {
                            Name = column.ColumnName,
                            Type = column.AllowDBNull ? $"{column.DataType.Name}?" : column.DataType.Name,
                            Options = column.AllowDBNull 
                                ? new List<PropertyOption> { PropertyOption.IsNullable }
                                : new List<PropertyOption>()
                        })
                        .ToList()
                };

                outputDescription = JsonSerializer.Serialize(entityDefinition);
                return true;
            }
        }
        catch (Exception ex)
        {
            outputDescription = ex.Message;
            return false;
        }
    }

    public async Task<SQLQueryDTO> UpdateQueryAsync(SQLQueryDTO query, Dictionary<string, object> sampleParameters = null)
    {
        var existingQuery = await _context.SQLQueries.FindAsync(query.Id);
        if (existingQuery == null) return null;
        if (ValidateAndDescribeQuery(query, sampleParameters, out string outputDescription))
        {
            existingQuery.Name = query.Name;
            existingQuery.Description = query.Description;
            existingQuery.Query = query.Query;
            existingQuery.IsPublic = query.IsPublic;
            existingQuery.Parameters = query.Parameters;
            existingQuery.LastModifiedAt = DateTime.UtcNow;
            existingQuery.OutputDescription = outputDescription;
            await _context.SaveChangesAsync();
        }
        else
        {
            throw new Exception($"Unable to validate query: {outputDescription}");
        }
        
        return SQLQueryDTO.FromEntity(existingQuery);
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

    public async Task<PagedResult<dynamic>> ExecuteQueryAsync(int id, Dictionary<string, object> parameters, int pageNumber = 1, int pageSize = 0)
    {
        var query = await GetQueryByIdAsync(id);
        if (query == null) throw new NotFoundException("Query not found");

        try
        {
            using (var dbContext = Utils.GetDbContextFromTypeName(query.DBConnection.ContextName))
            {
                var command = dbContext.Database.GetDbConnection().CreateCommand();

                // Construire la requête paginée
                string sqlQuery = query.Query.TrimEnd(';');
                if (pageSize > 0)
                {
                    // Si la requête commence par WITH, on doit la traiter différemment
                    if (sqlQuery.TrimStart().StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
                    {
                        // Extraire toutes les CTEs existantes
                        var withoutFirstWith = sqlQuery.TrimStart().Substring(4).TrimStart();
                        var lastSelectIndex = withoutFirstWith.LastIndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
                        var ctes = withoutFirstWith.Substring(0, lastSelectIndex).TrimEnd();
                        var mainSelect = withoutFirstWith.Substring(lastSelectIndex);

                        sqlQuery = $@"WITH {ctes},
BaseResult AS (
    {mainSelect}
),
CountData AS (
    SELECT COUNT(*) AS Total FROM BaseResult
),
PaginatedData AS (
    SELECT 
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RowNum,
        (SELECT Total FROM CountData) AS TotalCount,
        fr.*
    FROM BaseResult fr
)
SELECT *
FROM PaginatedData
WHERE RowNum BETWEEN @Skip + 1 AND @Skip + @Take;";
                    }
                    else
                    {
                        sqlQuery = $@"WITH CountData AS (
    SELECT COUNT(*) AS Total FROM ({sqlQuery}) BaseQuery
),
QueryData AS (
    SELECT 
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RowNum,
        (SELECT Total FROM CountData) AS TotalCount,
        q.*
    FROM ({sqlQuery}) q
)
SELECT *
FROM QueryData
WHERE RowNum BETWEEN @Skip + 1 AND @Skip + @Take;";
                    }

                    var skipParameter = command.CreateParameter();
                    skipParameter.ParameterName = "@Skip";
                    skipParameter.Value = (pageNumber - 1) * pageSize;
                    command.Parameters.Add(skipParameter);

                    var takeParameter = command.CreateParameter();
                    takeParameter.ParameterName = "@Take";
                    takeParameter.Value = pageSize;
                    command.Parameters.Add(takeParameter);
                }

                command.CommandText = sqlQuery;
                command.CommandType = CommandType.Text;

                // Ajouter les autres paramètres de la requête
                foreach (var param in parameters)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = param.Key;
                    parameter.Value = param.Value ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }

                await dbContext.Database.OpenConnectionAsync();
                
                using (var result = await command.ExecuteReaderAsync())
                {
                    var data = new List<dynamic>();
                    int totalCount = 0;

                    while (await result.ReadAsync())
                    {
                        var row = new ExpandoObject() as IDictionary<string, object>;
                        
                        // Récupérer le total si on est en mode paginé
                        if (pageSize > 0 && totalCount == 0)
                        {
                            totalCount = Convert.ToInt32(result.GetValue(result.GetOrdinal("TotalCount")));
                        }

                        // Ajouter toutes les colonnes sauf RowNum et TotalCount si on est en mode paginé
                        for (var i = 0; i < result.FieldCount; i++)
                        {
                            var columnName = result.GetName(i);
                            if (pageSize == 0 || (columnName != "RowNum" && columnName != "TotalCount"))
                            {
                                row.Add(columnName, result.GetValue(i));
                            }
                        }
                        data.Add(row);
                    }

                    // Si pas de pagination, le total est le nombre de lignes
                    if (pageSize == 0)
                    {
                        totalCount = data.Count;
                    }

                    return new PagedResult<dynamic>(data, totalCount);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
} 