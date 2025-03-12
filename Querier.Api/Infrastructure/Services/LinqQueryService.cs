using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Antlr4.StringTemplate;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Common.Extensions;
using Querier.Api.Common.Utilities;
using Querier.Api.Domain.Common.Models;
using Querier.Api.Domain.Entities;
using Querier.Api.Domain.Exceptions;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Services;

public class LinqQueryService(IDbContextFactory<ApiDbContext> contextFactory,
    IDbConnectionService dbConnectionService,
    IUserService userService,
    IHttpContextAccessor httpContextAccessor,
    IAssemblyManagerService assemblyManagerService,
    IRoslynCompilerService roslynCompilerService,
    ILogger<SqlQueryService> logger) : ILinqQueryService
{
    public async Task<IEnumerable<LinqQueryDto>> GetAllQueriesAsync(string userMail)
    {
        try
        {
            logger.LogDebug("Getting all SQL queries for user {UserId}", userMail);

            if (string.IsNullOrEmpty(userMail))
            {
                logger.LogWarning("User email is null or empty");
                throw new ArgumentException("User email is required", nameof(userMail));
            }

            ApiUserDto userDto = await userService.GetByEmailAsync(userMail);
            using var context = await contextFactory.CreateDbContextAsync();
            IEnumerable<LinqQueryDto> queries = await context.LinqQueries
                .Where(q => q.IsPublic || q.CreatedBy == userDto.Id)
                .OrderByDescending(q => q.CreatedAt)
                .Select(q => new LinqQueryDto
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
                        DBConnectionId = q.ConnectionId,
                        OutputDescription = q.OutputDescription
                    }
                ).ToListAsync();

            foreach (LinqQueryDto sqlQuery in queries)
            {
                sqlQuery.CreatedByEmail = (await userService.GetByIdAsync(sqlQuery.CreatedBy)).Email;
            }

            logger.LogInformation("Retrieved {Count} queries for user {UserId}", queries.Count(), userMail);
            return queries;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving SQL queries for user {UserId}", userMail);
            throw;
        }
    }

    public async Task<LinqQueryDto> GetQueryByIdAsync(int id)
    {
        try
        {
            logger.LogDebug("Getting SQL query with ID {QueryId}", id);
            await using var context = await contextFactory.CreateDbContextAsync();
            var query = await context.LinqQueries
                .Include(q => q.Connection)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (query == null)
            {
                logger.LogWarning("SQL query with ID {QueryId} not found", id);
                return null;
            }

            var dto = LinqQueryDto.FromEntity(query);
            logger.LogInformation("Successfully retrieved SQL query with ID {QueryId}", id);
            return dto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving SQL query with ID {QueryId}", id);
            throw;
        }
    }

    public async Task<LinqQueryDto> CreateQueryAsync(LinqQueryDto query, Dictionary<string, object> sampleParameters = null)
    {
        try
        {
            logger.LogDebug("Creating new Linq query");

            if (query == null)
            {
                logger.LogWarning("Query DTO is null");
                throw new ArgumentNullException(nameof(query));
            }

            if (httpContextAccessor.HttpContext != null)
            {
                var currentUser = await userService.GetCurrentUserAsync(httpContextAccessor.HttpContext.User);
                if (currentUser == null && !string.IsNullOrEmpty(query.CreatedBy))
                    currentUser = await userService.GetByIdAsync(query.CreatedBy);
                query.CreatedAt = DateTime.UtcNow;
                query.CreatedBy = currentUser?.Id;
            }

            using var context = await contextFactory.CreateDbContextAsync();
            query.DBConnection = DBConnectionDto.FromEntity(await context.DBConnections.FindAsync(query.DBConnectionId));

            if (query.DBConnection == null)
            {
                logger.LogWarning("Database connection with ID {ConnectionId} not found", query.DBConnectionId);
                throw new NotFoundException($"Database connection with ID {query.DBConnectionId} not found");
            }

            logger.LogDebug("Validating query with sample parameters");
            var result = await ValidateAndCompileLinqQuery(query, sampleParameters, [query.DBConnection.AssemblyDll]);
            if (result.isValid)
            {
                var entity = new LinqQuery
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
                    AssemblyDll = result.assemblyBytes,
                    AssemblyPdb = result.pdbBytes
                };
                await assemblyManagerService.LoadQueryAssemblyAsync(query.DBConnection.Name, result.assemblyBytes);
                var connectionAssemblies = assemblyManagerService.GetAssemblies(query.DBConnection.Name);
                var assembly = connectionAssemblies.First(a => a.FullName.Contains(query.Name));
                Type? t = assembly.GetType($"{query.DBConnection.Name}.Contexts.LinqQuery.{query.DBConnection.Name}{query.Name}Query");
                if (t == null) throw new Exception($"Type {query.DBConnection}.Contexts.LinqQuery.{query.DBConnection}{query.Name}Query not found");
                MethodInfo? m = t.GetMethod("CreateDelegate", BindingFlags.Public | BindingFlags.Static);
                if (m == null) throw new Exception("CreateDelegate() method not found");
                var func = (Func<IDynamicReadOnlyDbContext, dynamic>)m.Invoke(null, null);

                var dbContextFactory = await dbConnectionService.GetReadOnlyDbContextFactoryByIdAsync(query.DBConnectionId);
                var dbContext = (IDynamicReadOnlyDbContext) dbContextFactory.CreateDbContext();
                dbContext.CompiledQueries[query.Name] = func;
                
                var dataresult = (IEnumerable) dbContext.CompiledQueries[query.Name](dbContext);
                DataTable dt = dataresult.ToDataTable();
                logger.LogDebug("Query execution successful, creating entity definition");
                string outputDescription = new JsonSchemaGeneratorService(logger).GenerateFromDataTable(dt, query.Name);
                
                context.LinqQueries.Add(entity);
                await context.SaveChangesAsync();
                
                query.OutputDescription = outputDescription;
                query.Id = entity.Id;

                logger.LogInformation("Successfully created Linq query with ID {QueryId}", query.Id);
                return query;
            }
            throw new Exception("Linq query is not valid");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating Linq query");
            throw;
        }
    }

    public async Task<LinqQueryDto> UpdateQueryAsync(LinqQueryDto query, Dictionary<string, object> sampleParameters = null)
    {
        try
        {
            logger.LogDebug("Updating Linq query with ID {QueryId}", query?.Id);

            if (query == null)
            {
                logger.LogWarning("Query DTO is null");
                throw new ArgumentNullException(nameof(query));
            }
            await using var context = await contextFactory.CreateDbContextAsync();
            var existingQuery = await context.LinqQueries.FindAsync(query.Id);
            if (existingQuery == null)
            {
                logger.LogWarning("Linq query with ID {QueryId} not found", query.Id);
                return null;
            }

            logger.LogDebug("Validating updated query");
            /*
            if (ValidateAndCompileLinqQuery(query, sampleParameters, out string outputDescription))
            {
                existingQuery.Name = query.Name;
                existingQuery.Description = query.Description;
                existingQuery.Query = query.Query;
                existingQuery.IsPublic = query.IsPublic;
                existingQuery.Parameters = query.Parameters;
                existingQuery.LastModifiedAt = DateTime.UtcNow;
                existingQuery.OutputDescription = outputDescription;
                    
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully updated Linq query with ID {QueryId}", query.Id);
                return LinqQueryDto.FromEntity(existingQuery);
            }
            */
            throw new Exception("Linq query is not valid");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating Linq query with ID {QueryId}", query?.Id);
            throw;
        }
    }

    public async Task DeleteQueryAsync(int id)
    {
        try
        {
            logger.LogDebug("Deleting Linq query with ID {QueryId}", id);
            await using var context = await contextFactory.CreateDbContextAsync();
            var query = await context.LinqQueries.FindAsync(id);
            if (query == null)
            {
                logger.LogWarning("Linq query with ID {QueryId} not found", id);
                throw new NotFoundException($"Linq query with ID {id} not found");
            }

            context.LinqQueries.Remove(query);
            await context.SaveChangesAsync();
            logger.LogInformation("Successfully deleted Linq query with ID {QueryId}", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting Linq query with ID {QueryId}", id);
            throw;
        }
    }

    public async Task<DataPagedResult<dynamic>> ExecuteQueryAsync(int queryId, DataRequestParametersWithParametersDto dataRequestParameters)
    {
        try
        {
            logger.LogDebug("Executing Linq query with ID {QueryId}, Page {PageNumber}, Size {PageSize}", queryId, dataRequestParameters.PageNumber, dataRequestParameters.PageSize);

            var query = await GetQueryByIdAsync(queryId);
            if (query == null)
            {
                logger.LogWarning("SQL query with ID {QueryId} not found", queryId);
                throw new NotFoundException("Query not found");
            }
            
            var dbContextFactory = await dbConnectionService.GetReadOnlyDbContextFactoryByIdAsync(query.DBConnectionId);
            var dbContext = (IDynamicReadOnlyDbContext) dbContextFactory.CreateDbContext();

            if (!dbContext.CompiledQueries.ContainsKey(query.Name))
            {
                var connectionAssemblies = assemblyManagerService.GetAssemblies(query.DBConnection.Name);
                if (!connectionAssemblies.Any(a => a.FullName.Contains(query.Name)))
                    await assemblyManagerService.LoadQueryAssemblyAsync(query.DBConnection.Name, query.AssemblyDll);

                var assembly = connectionAssemblies.First(a => a.FullName.Contains(query.Name));
                Type? t = assembly.GetType($"{query.DBConnection.Name}.Contexts.LinqQuery.{query.DBConnection.Name}{query.Name}Query");
                if (t == null) throw new Exception($"Type {query.DBConnection}.Contexts.LinqQuery.{query.DBConnection}{query.Name}Query not found");
                MethodInfo? m = t.GetMethod("CreateDelegate", BindingFlags.Public | BindingFlags.Static);
                if (m == null) throw new Exception("CreateDelegate() method not found");
                var func = (Func<IDynamicReadOnlyDbContext, dynamic>)m.Invoke(null, null);
                dbContext.CompiledQueries[query.Name] = func;
            }
            
            var data = (IEnumerable<dynamic>)dbContext.CompiledQueries[query.Name](dbContext);
            var enumerable = data as dynamic[] ?? data.ToArray();
            int totalCount = enumerable.Count();
            if (dataRequestParameters.PageSize > 0)
            {
                logger.LogDebug("Applying pagination to query");
                data = enumerable.Skip((dataRequestParameters.PageNumber - 1) * dataRequestParameters.PageSize).Take(dataRequestParameters.PageSize);
            }
            
            return new DataPagedResult<dynamic>(enumerable, totalCount, dataRequestParameters);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing SQL query with ID {QueryId}", queryId);
            throw;
        }
    }

    private async Task<(bool isValid, byte[] assemblyBytes, byte[] pdbBytes)> ValidateAndCompileLinqQuery(
        LinqQueryDto query, Dictionary<string, object> sampleParameters, List<byte[]> refAssemblilesBytes) 
    {
        try
        {
            logger.LogDebug("Validating and describing query for {QueryName}", query.Name);
            
            if (query.DBConnection == null)
            {
                using var context = contextFactory.CreateDbContext();
                query.DBConnection = DBConnectionDto.FromEntity(context.DBConnections.Find(query.DBConnectionId));
                if (query.DBConnection == null)
                {
                    throw new NotFoundException($"Database connection with ID {query.DBConnectionId} not found");
                }
            }

            int dbConnectionId = query.DBConnectionId;
            
            var targetContextFactory = dbConnectionService.GetDbContextFactoryByIdAsync(dbConnectionId).GetAwaiter().GetResult();
            using var dbcontext = targetContextFactory.CreateDbContext();
            
            var template = new Template(File.ReadAllText(
                Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", $"LinqQuery.st")
            ), '$', '$');
            
            string contextName = (await dbConnectionService.GetByIdAsync(dbConnectionId)).Name;
            string contextNamespace = $"{contextName}.Contexts";
            template.Add("contextNamespace", contextNamespace);
            template.Add("contextName", contextName);
            template.Add("linqQueryName", query.Name);
            template.Add("linqQueryCode", query.Query);
            string finalLinqQueryCode = template.Render();
            Dictionary<string, string> source = new Dictionary<string, string>();
            source.Add("query", finalLinqQueryCode);
            var compilationResult = roslynCompilerService.CompileAssembly(query.Name, source, [], refAssemblilesBytes);
            
            return (true, compilationResult.AssemblyBytes, compilationResult.PdbBytes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error compiling/validating query for {QueryName}", query.Name);
            return (false, null, null);
        }
    }
}