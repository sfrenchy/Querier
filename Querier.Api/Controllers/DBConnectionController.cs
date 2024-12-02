﻿using Querier.Api.Models;
using Querier.Api.Models.Datatable;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Querier.Api.Models.QDBConnection;
using Querier.Api.Models.Requests;
using Microsoft.AspNetCore.Http;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for managing database connections
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Adding new database connections
    /// - Reading existing database connections
    /// - Deleting database connections
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
    /// - 401 Unauthorized: Authentication required
    /// - 403 Forbidden: User lacks required permissions
    /// - 500 Internal Server Error: Unexpected server error
    /// </remarks>
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class DBConnectionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IDBConnectionService _dbConnectionService;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<DBConnectionController> _logger;

        public DBConnectionController(IHostApplicationLifetime hostApplicationLifetime, IDBConnectionService dbConnectionService, IConfiguration configuration, ILogger<DBConnectionController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _dbConnectionService = dbConnectionService;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

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
        public async Task<IActionResult> AddDBConnectionAsync([FromBody] AddDBConnectionRequest connection)
        {
            return Ok(await _dbConnectionService.AddConnectionAsync(connection));
        }

        /// <summary>
        /// Retrieves database connections based on specified criteria
        /// </summary>
        /// <remarks>
        /// Returns a list of database connections that match the provided search criteria.
        /// 
        /// Sample request:
        ///     POST /api/v1/dbconnection/readdbconnection
        ///     {
        ///         "searchTerm": "production",
        ///         "pageSize": 10,
        ///         "pageNumber": 1
        ///     }
        /// </remarks>
        /// <param name="request">The search and pagination parameters</param>
        /// <returns>A list of matching database connections</returns>
        /// <response code="200">Returns the list of database connections</response>
        [HttpPost("ReadDBConnection")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ReadDBConnectionAsync(ServerSideRequest request)
        {
            return Ok(await _dbConnectionService.ReadDBConnectionAsync(request));
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
        /// <param name="request">The identifier of the connection to delete</param>
        /// <returns>The result of the deletion operation</returns>
        /// <response code="200">Connection was successfully deleted</response>
        /// <response code="404">If the connection was not found</response>
        [HttpDelete("DeleteDBConnection")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDBConnectionAsync(DeleteDBConnectionRequest request)
        {
            return Ok(await _dbConnectionService.DeleteDBConnectionAsync(request));
        }
    }
}
