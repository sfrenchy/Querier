using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Common.Extensions;
using Querier.Api.Common.Utilities;
using Querier.Api.Domain.Common.Models;
using Querier.Api.Domain.Common.ValueObjects;
using Querier.Api.Domain.Entities;
using Querier.Api.Domain.Exceptions;
using Querier.Api.Infrastructure.Data.Context;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Infrastructure.Services
{
    public class SqlQueryService(
        ApiDbContext context,
        IUserService userService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SqlQueryService> logger)
        : ISqlQueryService
    {

        public async Task<IEnumerable<SQLQueryDTO>> GetAllQueriesAsync(string userId)
        {
            try
            {
                logger.LogDebug("Getting all SQL queries for user {UserId}", userId);

                if (string.IsNullOrEmpty(userId))
                {
                    logger.LogWarning("User ID is null or empty");
                    throw new ArgumentException("User ID is required", nameof(userId));
                }

            var queries = await context.SQLQueries
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

                logger.LogInformation("Retrieved {Count} queries for user {UserId}", queries.Count, userId);
            return queries;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving SQL queries for user {UserId}", userId);
                throw;
            }
        }

        public async Task<SQLQueryDTO> GetQueryByIdAsync(int id)
        {
            try
            {
                logger.LogDebug("Getting SQL query with ID {QueryId}", id);

                var query = await context.SQLQueries
                .Include(q => q.Connection)
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (query == null)
                {
                    logger.LogWarning("SQL query with ID {QueryId} not found", id);
                    return null;
                }

                var dto = SQLQueryDTO.FromEntity(query);
                logger.LogInformation("Successfully retrieved SQL query with ID {QueryId}", id);
                return dto;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving SQL query with ID {QueryId}", id);
                throw;
            }
        }

        public async Task<SQLQueryDTO> CreateQueryAsync(SQLQueryDTO query, Dictionary<string, object> sampleParameters = null)
        {
            try
            {
                logger.LogDebug("Creating new SQL query");

                if (query == null)
                {
                    logger.LogWarning("Query DTO is null");
                    throw new ArgumentNullException(nameof(query));
                }

                if (httpContextAccessor.HttpContext != null)
        {
            var currentUser = await userService.GetCurrentUserAsync(httpContextAccessor.HttpContext.User);
            query.CreatedAt = DateTime.UtcNow;
            query.CreatedBy = currentUser?.Id;
                }

                query.DBConnection = DBConnectionDto.FromEntity(await context.DBConnections.FindAsync(query.DBConnectionId));

                if (query.DBConnection == null)
                {
                    logger.LogWarning("Database connection with ID {ConnectionId} not found", query.DBConnectionId);
                    throw new NotFoundException($"Database connection with ID {query.DBConnectionId} not found");
                }

                logger.LogDebug("Validating query with sample parameters");
            if (ValidateAndDescribeQuery(query, sampleParameters, out string outputDescription))
                {
                    var entity = new SQLQuery
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
                    };

                    context.SQLQueries.Add(entity);
                await context.SaveChangesAsync();
                    
                query.OutputDescription = outputDescription;
                    query.Id = entity.Id;

                    logger.LogInformation("Successfully created SQL query with ID {QueryId}", query.Id);
                    return query;
                }

                var errorMessage = $"Unable to validate query: {outputDescription}";
                logger.LogWarning("Query validation failed: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating SQL query");
                throw;
            }
        }

        private bool ValidateAndDescribeQuery(SQLQueryDTO query, Dictionary<string, object> sampleParameters, out string outputDescription)
        {
            try
            {
                logger.LogDebug("Validating and describing query for {QueryName}", query.Name);

                if (query.DBConnection == null)
                {
                    query.DBConnection = DBConnectionDto.FromEntity(context.DBConnections.Find(query.DBConnectionId));
                    if (query.DBConnection == null)
                    {
                        throw new NotFoundException($"Database connection with ID {query.DBConnectionId} not found");
                    }
                }

                using var dbcontext = Utils.GetDbContextFromTypeName(query.DBConnection.ContextName);
                var parameters = new List<DbParameter>();
                if (sampleParameters != null)
                {
                    foreach (var p in sampleParameters)
                    {
                        var command = dbcontext.Database.GetDbConnection().CreateCommand();
                            var parameter = command.CreateParameter();
                            parameter.ParameterName = p.Key;
                            parameter.Value = p.Value ?? DBNull.Value;
                            parameters.Add(parameter);
                        }
                    logger.LogDebug("Created {Count} parameters for query validation", parameters.Count);
                    }

                DataTable dt = dbcontext.Database.RawSqlQuery(query.Query, parameters);
                logger.LogDebug("Query execution successful, creating entity definition");

                    var entityDefinition = new EntityDefinition
                    {
                        Name = query.Name,
                        Properties = dt.Columns.Cast<DataColumn>()
                            .Select(column => new PropertyDefinition
                            {
                                Name = column.ColumnName,
                                Type = column.AllowDBNull ? $"{column.DataType.Name}?" : column.DataType.Name,
                                Options = column.AllowDBNull
                                ? [PropertyOption.IsNullable]
                                : []
                            })
                            .ToList()
                    };

                    outputDescription = JsonSerializer.Serialize(entityDefinition);
                logger.LogInformation("Successfully validated and described query for {QueryName}", query.Name);
                    return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating query for {QueryName}", query.Name);
                outputDescription = ex.Message;
                return false;
            }
        }

        public async Task<SQLQueryDTO> UpdateQueryAsync(SQLQueryDTO query, Dictionary<string, object> sampleParameters = null)
        {
            try
            {
                logger.LogDebug("Updating SQL query with ID {QueryId}", query?.Id);

                if (query == null)
                {
                    logger.LogWarning("Query DTO is null");
                    throw new ArgumentNullException(nameof(query));
                }

            var existingQuery = await context.SQLQueries.FindAsync(query.Id);
                if (existingQuery == null)
                {
                    logger.LogWarning("SQL query with ID {QueryId} not found", query.Id);
                    return null;
                }

                logger.LogDebug("Validating updated query");
            if (ValidateAndDescribeQuery(query, sampleParameters, out string outputDescription))
            {
                existingQuery.Name = query.Name;
                existingQuery.Description = query.Description;
                existingQuery.Query = query.Query;
                existingQuery.IsPublic = query.IsPublic;
                existingQuery.Parameters = query.Parameters;
                existingQuery.LastModifiedAt = DateTime.UtcNow;
                existingQuery.OutputDescription = outputDescription;
                    
                await context.SaveChangesAsync();
                    logger.LogInformation("Successfully updated SQL query with ID {QueryId}", query.Id);
                    return SQLQueryDTO.FromEntity(existingQuery);
                }

                var errorMessage = $"Unable to validate query: {outputDescription}";
                logger.LogWarning("Query validation failed: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating SQL query with ID {QueryId}", query?.Id);
                throw;
            }
        }

        public async Task DeleteQueryAsync(int id)
        {
            try
            {
                logger.LogDebug("Deleting SQL query with ID {QueryId}", id);

                var query = await context.SQLQueries.FindAsync(id);
                if (query == null)
                {
                    logger.LogWarning("SQL query with ID {QueryId} not found", id);
                    throw new NotFoundException($"SQL query with ID {id} not found");
                }

                context.SQLQueries.Remove(query);
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully deleted SQL query with ID {QueryId}", id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting SQL query with ID {QueryId}", id);
                throw;
            }
        }

        public async Task<PagedResult<dynamic>> ExecuteQueryAsync(int id, Dictionary<string, object> parameters, int pageNumber = 1, int pageSize = 0)
        {
            try
            {
                logger.LogDebug("Executing SQL query with ID {QueryId}, Page {PageNumber}, Size {PageSize}", id, pageNumber, pageSize);

                var query = await GetQueryByIdAsync(id);
                if (query == null)
                {
                    logger.LogWarning("SQL query with ID {QueryId} not found", id);
                    throw new NotFoundException("Query not found");
                }

                await using var dbContext = Utils.GetDbContextFromTypeName(query.DBConnection.ContextName);
                    var command = dbContext.Database.GetDbConnection().CreateCommand();
                    string sqlQuery = query.Query.TrimEnd(';');

                    if (pageSize > 0)
                    {
                    logger.LogDebug("Applying pagination to query");
                    sqlQuery = BuildPaginatedQuery(sqlQuery);

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

                    foreach (var param in parameters)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = param.Key;
                        parameter.Value = param.Value ?? DBNull.Value;
                        command.Parameters.Add(parameter);
                    }

                logger.LogDebug("Opening database connection");
                    await dbContext.Database.OpenConnectionAsync();

                await using var result = await command.ExecuteReaderAsync();
                        var data = new List<dynamic>();
                        int totalCount = 0;

                        while (await result.ReadAsync())
                        {
                            var row = new ExpandoObject() as IDictionary<string, object>;

                            if (pageSize > 0 && totalCount == 0)
                            {
                                totalCount = Convert.ToInt32(result.GetValue(result.GetOrdinal("TotalCount")));
                            }

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

                        if (pageSize == 0)
                        {
                            totalCount = data.Count;
                        }

                logger.LogInformation(
                    "Successfully executed SQL query with ID {QueryId}. Retrieved {Count} rows, Total {Total}", 
                    id, data.Count, totalCount);

                        return new PagedResult<dynamic>(data, totalCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing SQL query with ID {QueryId}", id);
                throw;
            }
        }

        private string BuildPaginatedQuery(string sqlQuery)
        {
            if (sqlQuery.TrimStart().StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            {
                var withoutFirstWith = sqlQuery.TrimStart().Substring(4).TrimStart();
                var lastSelectIndex = withoutFirstWith.LastIndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
                var ctes = withoutFirstWith.Substring(0, lastSelectIndex).TrimEnd();
                var mainSelect = withoutFirstWith.Substring(lastSelectIndex);

                return $@"WITH {ctes},
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

            return $@"WITH CountData AS (
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
    }
}