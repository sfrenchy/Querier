using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for managing database connections and operations
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Managing database connections
    /// - Analyzing and executing queries
    /// - Retrieving database metadata
    /// - Managing stored procedures and views
    /// - Handling connection parameters
    /// </remarks>
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class DbConnectionController(
        IDBConnectionService dbConnectionService,
        ILogger<DbConnectionController> logger)
        : ControllerBase
    {

        /// <summary>
        /// Adds a new database connection
        /// </summary>
        /// <remarks>
        /// Creates a new database connection with the provided configuration.
        /// 
        /// Sample request:
        ///     POST /api/v1/dbconnection/adddbconnection
        ///     {
        ///         "name": "MyDatabase",
        ///         "connectionString": "Server=myserver;Database=mydb;User Id=myuser;Password=mypassword;"
        ///     }
        /// </remarks>
        /// <param name="connection">The database connection configuration</param>
        /// <returns>The created database connection details</returns>
        /// <response code="200">Returns the created database connection</response>
        /// <response code="400">If the connection configuration is invalid</response>
        [HttpPost("AddDbConnection")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddDbConnectionAsync([FromBody] DBConnectionCreateDto connection)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid model state for database connection creation");
                    return BadRequest(ModelState);
                }

                logger.LogInformation("Adding new database connection: {Name}", connection.Name);
                var result = await dbConnectionService.AddConnectionAsync(connection);

                if (result.State == Domain.Common.Enums.DBConnectionState.ConnectionError)
                {
                    logger.LogWarning("Connection error while adding database: {Name}. Messages: {@Messages}", 
                        connection.Name, result.Messages);
                    return BadRequest(new { error = "Connection error", messages = result.Messages });
                }

                if (result.State == Domain.Common.Enums.DBConnectionState.CompilationError)
                {
                    logger.LogWarning("Compilation error while adding database: {Name}. Messages: {@Messages}", 
                        connection.Name, result.Messages);
                    return BadRequest(new { error = "Compilation error", messages = result.Messages });
                }

                logger.LogInformation("Successfully added database connection: {Name}", connection.Name);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding database connection: {Name}", connection?.Name);
                throw;
            }
        }

        /// <summary>
        /// Deletes a database connection
        /// </summary>
        /// <remarks>
        /// Removes a database connection from the system.
        /// 
        /// Sample request:
        ///     DELETE /api/v1/dbconnection/deletedbconnection
        ///     {
        ///         "id": "123"
        ///     }
        /// </remarks>
        /// <param name="dbConnectionId">The identifier of the connection to delete</param>
        /// <returns>The result of the deletion operation</returns>
        /// <response code="200">Connection was successfully deleted</response>
        /// <response code="404">If the connection was not found</response>
        [HttpDelete("DeleteDBConnection")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDbConnectionAsync([FromQuery] int dbConnectionId)
        {
            try
            {
                logger.LogInformation("Deleting database connection with ID: {Id}", dbConnectionId);

                try
                {
                    await dbConnectionService.DeleteDbConnectionAsync(dbConnectionId);
                }
                catch (KeyNotFoundException)
                {
                    logger.LogWarning("Database connection not found with ID: {Id}", dbConnectionId);
                    return NotFound(new { error = $"Database connection with ID {dbConnectionId} not found" });
                }

                logger.LogInformation("Successfully deleted database connection with ID: {Id}", dbConnectionId);
                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting database connection with ID: {Id}", dbConnectionId);
                throw;
            }
        }

        /// <summary>
        /// Gets all database connections
        /// </summary>
        /// <returns>List of database connections</returns>
        /// <response code="200">Returns the list of database connections</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                logger.LogInformation("Retrieving all database connections");
                var connections = await dbConnectionService.GetAllAsync();
                logger.LogInformation("Successfully retrieved {Count} database connections", connections.Count);
                return Ok(connections);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving all database connections");
                throw;
            }
        }

        /// <summary>
        /// Gets the database schema description
        /// </summary>
        /// <remarks>
        /// Returns a detailed description of the database schema including:
        /// - Tables with their columns and relationships
        /// - Views with their columns
        /// - Stored procedures with their parameters
        /// - User-defined functions
        /// 
        /// Sample request:
        ///     GET /api/v1/dbconnection/{connectionId}/schema
        /// </remarks>
        /// <param name="connectionId">The ID of the database connection</param>
        /// <returns>Detailed description of the database schema</returns>
        /// <response code="200">Returns the database schema description</response>
        /// <response code="404">If the connection was not found</response>
        /// <response code="400">If there's an error retrieving the schema</response>
        [HttpGet("{connectionId}/schema")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDatabaseSchema(int connectionId)
        {
            try
            {
                logger.LogInformation("Retrieving database schema for connection ID: {Id}", connectionId);
                var schema = await dbConnectionService.GetDatabaseSchemaAsync(connectionId);
                logger.LogInformation("Successfully retrieved schema for connection ID: {Id}", connectionId);
                return Ok(schema);
            }
            catch (KeyNotFoundException)
            {
                logger.LogWarning("Database connection not found with ID: {Id}", connectionId);
                return NotFound($"Database connection with ID {connectionId} not found");
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning(ex, "Database type not supported for connection ID: {Id}", connectionId);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving database schema for connection ID: {Id}", connectionId);
                return StatusCode(500, "An error occurred while retrieving the database schema");
            }
        }

        /// <summary>
        /// Analyzes a SQL query to find referenced objects
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/v1/dbconnection/{connectionId}/analyze-query
        ///     {
        ///         "query": "SELECT o.OrderID, o.OrderDate FROM Orders o",
        ///         "parameters": {}
        ///     }
        /// </remarks>
        /// <param name="connectionId">Database connection ID</param>
        /// <param name="request">Query to analyze</param>
        /// <response code="200">Returns the list of referenced objects</response>
        /// <response code="400">If the query is invalid</response>
        /// <response code="404">If the connection was not found</response>
        [HttpPost("{connectionId}/analyze-query")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<string>>> AnalyzeQuery(
            int connectionId, 
            [FromBody] DBConnectionAnalyzeQueryDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid model state for query analysis on connection ID: {Id}", connectionId);
                    return BadRequest(ModelState);
                }

                logger.LogInformation("Analyzing query for connection ID: {Id}", connectionId);
                var objects = await dbConnectionService.GetQueryObjectsAsync(connectionId, request.Query);
                logger.LogInformation("Successfully analyzed query for connection ID: {Id}", connectionId);
                return Ok(objects);
            }
            catch (KeyNotFoundException)
            {
                logger.LogWarning("Database connection not found with ID: {Id}", connectionId);
                return NotFound($"Database connection with ID {connectionId} not found");
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid query for connection ID: {Id}", connectionId);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error analyzing query for connection ID: {Id}", connectionId);
                return StatusCode(500, "An error occurred while analyzing the query");
            }
        }

        /// <summary>
        /// Downloads the source code of a compiled database connection
        /// </summary>
        /// <remarks>
        /// Returns the source code as a zip file containing all generated files.
        /// </remarks>
        /// <param name="connectionId">The ID of the database connection</param>
        /// <returns>A zip file containing the source code</returns>
        /// <response code="200">Returns the zip file</response>
        /// <response code="404">If the connection was not found</response>
        [HttpGet("{connectionId}/sources")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadSources(int connectionId)
        {
            try
            {
                logger.LogInformation("Downloading sources for connection ID: {Id}", connectionId);
                var sources = await dbConnectionService.GetConnectionSourcesAsync(connectionId);
                logger.LogInformation("Successfully downloaded sources for connection ID: {Id}", connectionId);
                return File(sources.Content, "application/zip", sources.FileName);
            }
            catch (KeyNotFoundException)
            {
                logger.LogWarning("Database connection not found with ID: {Id}", connectionId);
                return NotFound($"Database connection with ID {connectionId} not found");
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "No source code available for connection ID: {Id}", connectionId);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error downloading sources for connection ID: {Id}", connectionId);
                return StatusCode(500, "An error occurred while downloading the sources");
            }
        }

        /// <summary>
        /// Enumerates database servers available on the network
        /// </summary>
        /// <remarks>
        /// Searches for accessible database servers of the specified type on the network.
        /// 
        /// Supported database types:
        /// - SQLServer
        /// - MySQL
        /// - PostgreSQL
        /// 
        /// Sample request:
        ///     GET /api/v1/dbconnection/enumerate-servers/SQLServer
        /// </remarks>
        /// <param name="databaseType">Type of database to search for</param>
        /// <returns>List of found servers with their information</returns>
        /// <response code="200">Returns the list of found servers</response>
        /// <response code="400">If the database type is not supported</response>
        [HttpGet("enumerate-servers/{databaseType}")]
        [ProducesResponseType(typeof(List<DBConnectionDatabaseServerInfoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<DBConnectionDatabaseServerInfoDto>>> EnumerateServers(string databaseType)
        {
            try
            {
                if (!new[] { "SQLServer", "MySQL", "PostgreSQL" }.Contains(databaseType))
                {
                    logger.LogWarning("Unsupported database type requested: {Type}", databaseType);
                    return BadRequest($"Database type '{databaseType}' is not supported. Supported types are: SQLServer, MySQL, PostgreSQL");
                }

                logger.LogInformation("Enumerating {Type} servers", databaseType);
                var servers = await dbConnectionService.EnumerateServersAsync(databaseType);
                logger.LogInformation("Successfully enumerated {Count} {Type} servers", servers.Count, databaseType);
                return Ok(servers);
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning(ex, "Database type not supported: {Type}", databaseType);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error enumerating {Type} servers", databaseType);
                return StatusCode(500, $"An error occurred while enumerating {databaseType} servers");
            }
        }

        /// <summary>
        /// Gets endpoints available for a database connection
        /// </summary>
        /// <param name="id">ID of the connection</param>
        /// <returns>List of endpoints with their JSON schemas</returns>
        /// <response code="200">Returns the list of endpoints</response>
        /// <response code="404">If the connection is not found</response>
        [HttpGet("{id}/endpoints")]
        [ProducesResponseType(typeof(List<DBConnectionEndpointInfoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<DBConnectionEndpointInfoDto>>> GetEndpoints(int id)
        {
            try
            {
                logger.LogInformation("Retrieving endpoints for connection ID: {Id}", id);
                var endpoints = await dbConnectionService.GetEndpointsAsync(id);
                logger.LogInformation("Successfully retrieved {Count} endpoints for connection ID: {Id}", 
                    endpoints.Count, id);
                return Ok(endpoints);
            }
            catch (KeyNotFoundException)
            {
                logger.LogWarning("Database connection not found with ID: {Id}", id);
                return NotFound($"Connection with ID {id} not found");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving endpoints for connection ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Gets controllers available for a database connection
        /// </summary>
        /// <param name="id">ID of the connection</param>
        /// <returns>List of controllers with their actions</returns>
        /// <response code="200">Returns the list of controllers</response>
        /// <response code="404">If the connection is not found</response>
        [HttpGet("{id}/controllers")]
        [ProducesResponseType(typeof(List<DBConnectionControllerInfoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<DBConnectionControllerInfoDto>>> GetControllers(int id)
        {
            try
            {
                logger.LogInformation("Retrieving controllers for connection ID: {Id}", id);
                var controllers = await dbConnectionService.GetControllersAsync(id);
                logger.LogInformation("Successfully retrieved {Count} controllers for connection ID: {Id}", 
                    controllers.Count, id);
                return Ok(controllers);
            }
            catch (KeyNotFoundException)
            {
                logger.LogWarning("Database connection not found with ID: {Id}", id);
                return NotFound($"Connection with ID {id} not found");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving controllers for connection ID: {Id}", id);
                throw;
            }
        }
    }
}
