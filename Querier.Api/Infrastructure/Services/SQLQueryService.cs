using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
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
using Querier.Api.Domain.Services;
using Querier.Api.Tools;

public interface ISQLQueryService
{
    Task<IEnumerable<SQLQueryDTO>> GetAllQueriesAsync(string userId);
    Task<SQLQuery> GetQueryByIdAsync(int id);
    Task<SQLQuery> CreateQueryAsync(SQLQuery query, Dictionary<string, object> sampleParameters = null);
    Task<SQLQuery> UpdateQueryAsync(SQLQuery query, Dictionary<string, object> sampleParameters = null);
    Task DeleteQueryAsync(int id);
    Task<IEnumerable<dynamic>> ExecuteQueryAsync(int id, Dictionary<string, object> parameters);
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
                ConnectionId = q.ConnectionId
            })
            .ToListAsync();

        return queries;
    }

    public async Task<SQLQuery> GetQueryByIdAsync(int id)
    {
        return await _context.SQLQueries
            .Include(q => q.Connection)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<SQLQuery> CreateQueryAsync(SQLQuery query, Dictionary<string, object> sampleParameters = null)
    {
        var currentUser = await _userService.GetCurrentUser(_httpContextAccessor.HttpContext.User);
        query.CreatedAt = DateTime.UtcNow;
        query.CreatedBy = currentUser?.Id;

        query.Connection = await _context.QDBConnections.FindAsync(query.ConnectionId);

        if (ValidateAndDescribeQuery(query, sampleParameters, out string outputDescription))
        {
            query.OutputDescription = outputDescription;
            _context.SQLQueries.Add(query);
            await _context.SaveChangesAsync();
        }
        else
        {
            throw new Exception($"Unable to validate query: {outputDescription}");
        }
        
        return query;
    }

    private bool ValidateAndDescribeQuery(SQLQuery query, Dictionary<string, object> sampleParameters, out string outputDescription)
    {
        try
        {
            if (query.Connection == null)
            {
                query.Connection = _context.QDBConnections.Find(query.ConnectionId);
            }

            using (DbContext context = Utils.GetDbContextFromTypeName(query.Connection.ContextName))
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
                
                var columnsDescription = dt.Columns.Cast<DataColumn>()
                    .Select(column => new
                    {
                        name = column.ColumnName,
                        type = new
                        {
                            clrType = column.DataType.Name,
                            sqlType = GetSqlType(column),
                            isNullable = column.AllowDBNull
                        },
                        constraints = new
                        {
                            isPrimaryKey = column.Unique && !column.AllowDBNull,
                            isUnique = column.Unique,
                            allowNull = column.AllowDBNull,
                            maxLength = GetMaxLength(column),
                            defaultValue = column.DefaultValue?.ToString()
                        }
                    })
                    .ToList();

                var description = new
                {
                    columns = columnsDescription,
                    metadata = new
                    {
                        totalColumns = dt.Columns.Count,
                        hasParameters = parameters.Any(),
                        parameters = parameters.Select(p => new { name = p.ParameterName, type = p.DbType.ToString() })
                    }
                };

                outputDescription = System.Text.Json.JsonSerializer.Serialize(
                    description, 
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = false }
                );

                return true;
            }
        }
        catch (Exception ex)
        {
            outputDescription = ex.Message;
            return false;
        }
    }

    private string GetSqlType(DataColumn column)
    {
        // Mapping de base entre types .NET et types SQL
        var typeMap = new Dictionary<Type, string>
        {
            { typeof(int), "int" },
            { typeof(long), "bigint" },
            { typeof(string), "nvarchar" },
            { typeof(DateTime), "datetime2" },
            { typeof(bool), "bit" },
            { typeof(decimal), "decimal" },
            { typeof(float), "real" },
            { typeof(double), "float" },
            { typeof(Guid), "uniqueidentifier" },
            { typeof(byte[]), "varbinary" }
        };

        return typeMap.TryGetValue(column.DataType, out var sqlType) ? sqlType : "unknown";
    }

    private int? GetMaxLength(DataColumn column)
    {
        try
        {
            if (column.MaxLength > 0)
                return column.MaxLength;
            
            // Pour les types string, on peut essayer de détecter la taille via le schéma
            if (column.DataType == typeof(string))
            {
                // Logique additionnelle pour détecter la taille via le schéma si nécessaire
                return null;
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<SQLQuery> UpdateQueryAsync(SQLQuery query, Dictionary<string, object> sampleParameters = null)
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

        using (var command = Utils.GetDbContextFromTypeName(query.Connection.ContextName).Database.GetDbConnection().CreateCommand())
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