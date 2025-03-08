using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Common.Models;
using Querier.Api.Domain.Common.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using $rootNamespace$.DTOs;
using $rootNamespace$.Services;
using $rootNamespace$.Exceptions;

namespace $rootNamespace$.Controllers
{
    $entityList: {entity|
    /// <summary>
    /// Controller for managing $entity.Name$ entities
    /// </summary>
    [Authorize]
    [ApiController]
    [ControllerFor(Table = "$entity.PluralName$")]
    $if (entity.IsViewEntity)$
    [Route("api/v1/$contextRoute$/view/$entity.Name$")]
    $else$
    [Route("api/v1/$contextRoute$/$entity.Name$")]
    $endif$
    [Produces("application/json")]
    public class $entity.Name$Controller : ControllerBase
    {
        private readonly ILogger<$entity.Name$Controller> _logger;
        private readonly I$entity.Name$Service _service;

        public $entity.Name$Controller(
            I$entity.Name$Service service,
            ILogger<$entity.Name$Controller> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        \}

        /// <summary>
        /// Retrieves a paged list of $entity.PluralName$
        /// </summary>
        /// <param name="parameters">Query parameters for pagination, sorting and filtering</param>
        /// <returns>A paged result of $entity.Name$ entities</returns>
        /// <response code="200">Returns the paged list of entities</response>
        /// <response code="400">If the parameters are invalid</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost("records")]
        [ProducesResponseType(typeof(DataPagedResult<$entity.Name$Dto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DataPagedResult<$entity.Name$Dto>>> GetAll([FromBody] DataRequestParametersDto parameters)
        {
            try
            {
                var result = await _service.GetAllAsync(parameters);
                return Ok(result);
            \}
            catch (EntityValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error in GetAll");
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation Error",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                \});
            \}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all $entity.PluralName$");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while retrieving the entities",
                    Status = StatusCodes.Status500InternalServerError
                \});
            \}
        \}

        /// <summary>
        /// Retrieves a specific $entity.Name$ by id
        /// </summary>
        /// <param name="id">The ID of the $entity.Name$ to retrieve</param>
        /// <returns>The requested $entity.Name$ entity</returns>
        /// <response code="200">Returns the requested entity</response>
        /// <response code="404">If the entity was not found</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("{id\}")]
        [ProducesResponseType(typeof($entity.Name$Dto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<$entity.Name$Dto>> GetById($entity.KeyType$ id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = "$entity.Name$ with ID  " + id.ToString() + " was not found",
                        Status = StatusCodes.Status404NotFound
                    \});
                \}
                return Ok(result);
            \}
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex, "Entity not found");
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                \});
            \}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving $entity.Name$ with ID " + id.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while retrieving the entity",
                    Status = StatusCodes.Status500InternalServerError
                \});
            \}
        \}

        $if (entity.IsTableEntity)$
            /// <summary>
            /// Creates a new $entity.Name$ entity
            /// </summary>
            /// <param name="createDto">The data to create the entity</param>
            /// <returns>The created entity</returns>
            /// <response code="201">Returns the newly created entity</response>
            /// <response code="400">If the data is invalid</response>
            /// <response code="500">If there was an internal server error</response>
            [HttpPost]
            [ProducesResponseType(typeof($entity.Name$Dto), StatusCodes.Status201Created)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
            public async Task<ActionResult<$entity.Name$Dto>> Create([FromBody] Create$entity.Name$Dto createDto)
            {
                try
                {
                    var result = await _service.CreateAsync(createDto);
                    return CreatedAtAction(nameof(GetById), new { id = result.$entity.KeyName$ \}, result);
                \}
                catch (EntityValidationException ex)
                {
                    _logger.LogWarning(ex, "Validation error in Create");
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    \});
                \}
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating new $entity.Name$");
                    return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An error occurred while creating the entity",
                        Status = StatusCodes.Status500InternalServerError
                    \});
                \}
            \}
    
            /// <summary>
            /// Updates an existing $entity.Name$ entity
            /// </summary>
            /// <param name="id">The ID of the entity to update</param>
            /// <param name="updateDto">The data to update the entity</param>
            /// <returns>The updated entity</returns>
            /// <response code="200">Returns the updated entity</response>
            /// <response code="400">If the data is invalid</response>
            /// <response code="404">If the entity was not found</response>
            /// <response code="500">If there was an internal server error</response>
            [HttpPut("{id\}")]
            [ProducesResponseType(typeof($entity.Name$Dto), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
            public async Task<ActionResult<$entity.Name$Dto>> Update($entity.KeyType$ id, [FromBody] Update$entity.Name$Dto updateDto)
            {
                try
                {
                    var result = await _service.UpdateAsync(id, updateDto);
                    return Ok(result);
                \}
                catch (EntityNotFoundException ex)
                {
                    _logger.LogWarning(ex, "Entity not found");
                    return NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    \});
                \}
                catch (EntityValidationException ex)
                {
                    _logger.LogWarning(ex, "Validation error in Update");
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    \});
                \}
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating $entity.Name$ with ID " + id.ToString());
                    return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An error occurred while updating the entity",
                        Status = StatusCodes.Status500InternalServerError
                    \});
                \}
            \}
    
            /// <summary>
            /// Deletes a specific $entity.Name$ entity
            /// </summary>
            /// <param name="id">The ID of the entity to delete</param>
            /// <returns>No content if successful</returns>
            /// <response code="204">If the entity was successfully deleted</response>
            /// <response code="404">If the entity was not found</response>
            /// <response code="500">If there was an internal server error</response>
            [HttpDelete("{id\}")]
            [ProducesResponseType(StatusCodes.Status204NoContent)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> Delete($entity.KeyType$ id)
            {
                try
                {
                    await _service.DeleteAsync(id);
                    return NoContent();
                \}
                catch (EntityNotFoundException ex)
                {
                    _logger.LogWarning(ex, "Entity not found");
                    return NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    \});
                \}
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting $entity.Name$ with ID " + id.ToString());
                    return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An error occurred while deleting the entity",
                        Status = StatusCodes.Status500InternalServerError
                    \});
                \}
            \}
    
            /// <summary>
            /// Gets unique values for a specific column
            /// </summary>
            /// <param name="column">The name of the column</param>
            /// <returns>List of unique values</returns>
            /// <response code="200">Returns the list of unique values</response>
            /// <response code="400">If the column name is invalid</response>
            /// <response code="500">If there was an internal server error</response>
            [HttpGet("columns/{columnName\}/values")]
            [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
            public async Task<ActionResult<List<string>>> GetUniqueValues([FromRoute] string columnName)
            {
                try
                {
                    var result = await _service.ColumnUniqueValuesAsync(columnName);
                    return Ok(result);
                \}
                catch (EntityValidationException ex)
                {
                    _logger.LogWarning(ex, "Validation error in GetUniqueValues");
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    \});
                \}
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting unique values for column " + columnName);
                    return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An error occurred while retrieving unique values",
                        Status = StatusCodes.Status500InternalServerError
                    \});
                \}
            \}
        $endif$
    \}
    }$
\} 