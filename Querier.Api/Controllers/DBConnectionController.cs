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
    /// - Managing database connections (CRUD operations)
    /// - Analyzing and executing queries
    /// - Retrieving database metadata
    /// - Managing stored procedures and views
    /// - Handling connection parameters
    /// 
    /// ## Authentication
    /// All endpoints in this controller require authentication.
    /// Use a valid JWT token in the Authorization header:
    /// ```
    /// Authorization: Bearer {your-jwt-token}
    /// ```
    /// 
    /// ## Common Responses
    /// - 200 OK: Operation completed successfully
    /// - 201 Created: Resource created successfully
    /// - 204 No Content: Operation completed successfully with no response body
    /// - 400 Bad Request: Invalid input data
    /// - 401 Unauthorized: Authentication required
    /// - 403 Forbidden: User lacks required permissions
    /// - 404 Not Found: Resource not found
    /// - 500 Internal Server Error: Unexpected server error
    /// </remarks>
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class DbConnectionController(
        IDbConnectionService dbConnectionService,
        ILogger<DbConnectionController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Creates a new database connection
        /// </summary>
        /// <remarks>
        /// Creates a new database connection with the provided configuration.
        /// 
        /// Sample request:
        ///     POST /api/v1/dbconnection
        ///     {
        ///         "name": "MyDatabase",
        ///         "connectionString": "Server=myserver;Database=mydb;User Id=myuser;Password=mypassword;",
        ///         "type": "SqlServer",
        ///         "description": "Production database connection"
        ///     }
        /// 
        /// Sample success response:
        ///     {
        ///         "id": 1,
        ///         "name": "MyDatabase",
        ///         "type": "SqlServer",
        ///         "description": "Production database connection",
        ///         "state": "Connected",
        ///         "messages": []
        ///     }
        /// 
        /// Sample error response:
        ///     {
        ///         "error": "Connection error",
        ///         "messages": [
        ///             "Could not connect to server 'myserver'",
        ///             "Network error or server not found"
        ///         ]
        ///     }
        /// </remarks>
        /// <param name="connection">The database connection configuration</param>
        /// <returns>The created database connection details</returns>
        /// <response code="201">Returns the created database connection</response>
        /// <response code="400">If the connection configuration is invalid or connection test fails</response>
        /// <response code="500">If there was an unexpected error during creation</response>
        [HttpPost]
        [ProducesResponseType(typeof(DBConnectionDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateAsync([FromBody] DBConnectionCreateDto connection)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid model state for database connection creation");
                    return BadRequest(ModelState);
                }

                logger.LogInformation("Adding new database connection: {Name}", connection.Name);
                DBConnectionCreateResultDto result = await dbConnectionService.AddConnectionAsync(connection);

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
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
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
        /// Permanently removes a database connection from the system. This operation cannot be undone.
        /// 
        /// Sample request:
        ///     DELETE /api/v1/dbconnection/123
        /// 
        /// Sample error response:
        ///     {
        ///         "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        ///         "title": "Not Found",
        ///         "status": 404,
        ///         "detail": "Database connection with ID 123 not found"
        ///     }
        /// </remarks>
        /// <param name="id">The identifier of the connection to delete</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Connection was successfully deleted</response>
        /// <response code="404">If the connection was not found</response>
        /// <response code="500">If there was an unexpected error during deletion</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            try
            {
                logger.LogInformation("Deleting database connection with ID: {Id}", id);

                try
                {
                    await dbConnectionService.DeleteDbConnectionAsync(id);
                }
                catch (KeyNotFoundException)
                {
                    logger.LogWarning("Database connection not found with ID: {Id}", id);
                    return NotFound(new { error = $"Database connection with ID {id} not found" });
                }

                logger.LogInformation("Successfully deleted database connection with ID: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting database connection with ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Gets all database connections
        /// </summary>
        /// <remarks>
        /// Retrieves a list of all database connections accessible to the current user.
        /// 
        /// Sample response:
        ///     [
        ///         {
        ///             "id": 1,
        ///             "name": "Production DB",
        ///             "type": "SqlServer",
        ///             "description": "Main production database",
        ///             "state": "Connected",
        ///             "messages": []
        ///         },
        ///         {
        ///             "id": 2,
        ///             "name": "Test DB",
        ///             "type": "PostgreSQL",
        ///             "description": "Test environment",
        ///             "state": "Connected",
        ///             "messages": []
        ///         }
        ///     ]
        /// </remarks>
        /// <returns>List of database connections</returns>
        /// <response code="200">Returns the list of database connections</response>
        /// <response code="500">If there was an unexpected error</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<DBConnectionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllAsync()
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
        /// Gets a database connection by ID
        /// </summary>
        /// <remarks>
        /// Retrieves detailed information about a specific database connection.
        /// 
        /// Sample response:
        ///     {
        ///         "id": 1,
        ///         "name": "Production DB",
        ///         "type": "SqlServer",
        ///         "description": "Main production database",
        ///         "state": "Connected",
        ///         "messages": []
        ///     }
        /// </remarks>
        /// <param name="id">The ID of the database connection</param>
        /// <returns>The database connection details</returns>
        /// <response code="200">Returns the database connection</response>
        /// <response code="404">If the connection was not found</response>
        /// <response code="500">If there was an unexpected error</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DBConnectionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                logger.LogInformation("Retrieving database connection with ID: {Id}", id);
                var connection = await dbConnectionService.GetByIdAsync(id);
                
                if (connection == null)
                {
                    logger.LogWarning("Database connection not found with ID: {Id}", id);
                    return NotFound(new { error = $"Database connection with ID {id} not found" });
                }

                logger.LogInformation("Successfully retrieved database connection: {Id}", id);
                return Ok(connection);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving database connection: {Id}", id);
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
        ///     GET /api/v1/dbconnection/123/schema
        /// 
        /// Sample response:
        ///     {
        ///         "tables": [
        ///             {
        ///                 "name": "Users",
        ///                 "schema": "dbo",
        ///                 "columns": [
        ///                     {
        ///                         "name": "Id",
        ///                         "type": "int",
        ///                         "isNullable": false,
        ///                         "isPrimaryKey": true
        ///                     },
        ///                     {
        ///                         "name": "Email",
        ///                         "type": "nvarchar(256)",
        ///                         "isNullable": false
        ///                     }
        ///                 ]
        ///             }
        ///         ],
        ///         "views": [],
        ///         "storedProcedures": []
        ///     }
        /// </remarks>
        /// <param name="id">The ID of the database connection</param>
        /// <returns>Detailed description of the database schema</returns>
        /// <response code="200">Returns the database schema description</response>
        /// <response code="404">If the connection was not found</response>
        /// <response code="400">If there's an error retrieving the schema</response>
        /// <response code="500">If there was an unexpected error</response>
        [HttpGet("{id}/schema")]
        [ProducesResponseType(typeof(DBConnectionDatabaseSchemaDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDatabaseSchema(int id)
        {
            try
            {
                logger.LogInformation("Retrieving database schema for connection ID: {Id}", id);
                var schema = await dbConnectionService.GetDatabaseSchemaAsync(id);
                logger.LogInformation("Successfully retrieved schema for connection ID: {Id}", id);
                return Ok(schema);
            }
            catch (KeyNotFoundException)
            {
                logger.LogWarning("Database connection not found with ID: {Id}", id);
                return NotFound($"Database connection with ID {id} not found");
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning(ex, "Database type not supported for connection ID: {Id}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving database schema for connection ID: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the database schema");
            }
        }

        /// <summary>
        /// Analyzes a SQL query to find referenced objects
        /// </summary>
        /// <remarks>
        /// Analyzes the provided SQL query and returns a list of database objects (tables, views, etc.) that are referenced.
        /// This is useful for understanding dependencies and impact analysis.
        /// 
        /// Sample request:
        ///     POST /api/v1/dbconnection/123/analyze-query
        ///     {
        ///         "query": "SELECT o.OrderID, o.OrderDate, c.CustomerName FROM Orders o JOIN Customers c ON o.CustomerID = c.CustomerID",
        ///         "parameters": {}
        ///     }
        /// 
        /// Sample response:
        ///     {
        ///         "tables": ["Orders", "Customers"],
        ///         "columns": ["Orders.OrderID", "Orders.OrderDate", "Orders.CustomerID", "Customers.CustomerID", "Customers.CustomerName"],
        ///         "views": [],
        ///         "storedProcedures": []
        ///     }
        /// 
        /// Sample error response:
        ///     {
        ///         "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        ///         "title": "Bad Request",
        ///         "status": 400,
        ///         "detail": "Invalid SQL syntax near 'FROM'"
        ///     }
        /// </remarks>
        /// <param name="id">Database connection ID</param>
        /// <param name="request">Query to analyze</param>
        /// <returns>List of referenced database objects</returns>
        /// <response code="200">Returns the list of referenced objects</response>
        /// <response code="400">If the query is invalid</response>
        /// <response code="404">If the connection was not found</response>
        /// <response code="500">If there was an unexpected error</response>
        [HttpPost("{id}/analyze-query")]
        [ProducesResponseType(typeof(DBConnectionQueryAnalysisDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AnalyzeQuery(
            int id, 
            [FromBody] DBConnectionAnalyzeQueryDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid model state for query analysis on connection ID: {Id}", id);
                    return BadRequest(ModelState);
                }

                logger.LogInformation("Analyzing query for connection ID: {Id}", id);
                var objects = await dbConnectionService.GetQueryObjectsAsync(id, request.Query);
                logger.LogInformation("Successfully analyzed query for connection ID: {Id}", id);
                return Ok(objects);
            }
            catch (KeyNotFoundException)
            {
                logger.LogWarning("Database connection not found with ID: {Id}", id);
                return NotFound(new { error = $"Database connection with ID {id} not found" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error analyzing query for connection ID: {Id}", id);
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
        public async Task<ActionResult<List<DBConnectionEndpointInfoDto>>> GetEndpoints([FromRoute] int id, [FromQuery] string? targetTable, [FromQuery] string? controller, [FromQuery] string? action)
        {
            try
            {
                logger.LogInformation("Retrieving endpoints for connection ID: {Id}", id);
                var endpoints = await dbConnectionService.GetEndpointsAsync(id,  targetTable, controller, action);
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
