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
using Querier.Api.Infrastructure.Base;
using Querier.Api.Infrastructure.Base.Exceptions;
using $model.RootNamespace$.Dtos;
using $model.RootNamespace$.Services;

namespace $model.RootNamespace$.Controllers;

/// <summary>
/// Controller for managing $model.Entity.Name$ entities
/// </summary>
[Authorize]
[ApiController]
[ControllerFor(Table = "$model.Entity.PluralName$")]
$if (model.Entity.IsViewEntity)$
[Route("api/v1/$model.ApiRoute$/view/$model.Entity.Name$")]
$else$
[Route("api/v1/$model.ApiRoute$/$model.Entity.Name$")]
$endif$
[Produces("application/json")]
public class $model.Entity.Name$Controller : ControllerBase
{
    private readonly ILogger<$model.Entity.Name$Controller> _logger;
    private readonly I$model.Entity.Name$Service _service;

    public $model.Entity.Name$Controller(
        I$model.Entity.Name$Service service,
        ILogger<$model.Entity.Name$Controller> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    \}

    /// <summary>
    /// Retrieves a paged list of $model.Entity.PluralName$
    /// </summary>
    /// <param name="parameters">Query parameters for pagination, sorting and filtering</param>
    /// <returns>A paged result of $model.Entity.Name$ entities</returns>
    /// <response code="200">Returns the paged list of entities</response>
    /// <response code="400">If the parameters are invalid</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("records")]
    [ProducesResponseType(typeof(DataPagedResult<Get$model.Entity.Name$Dto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DataPagedResult<Get$model.Entity.Name$Dto>>> Records([FromBody] DataRequestParametersDto parameters)
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
            _logger.LogError(ex, "Error retrieving all $model.Entity.PluralName$");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving the entities",
                Status = StatusCodes.Status500InternalServerError
            \});
        \}
    \}
    $if (model.Entity.IsTableEntity)$
        $if (model.Entity.HasKey)$
            /// <summary>
            /// Retrieves a specific $model.Entity.Name$ by id
            /// </summary>
            /// <param name="id">The ID of the $model.Entity.Name$ to retrieve</param>
            /// <returns>The requested $model.Entity.Name$ entity</returns>
            /// <response code="200">Returns the requested entity</response>
            /// <response code="404">If the entity was not found</response>
            /// <response code="500">If there was an internal server error</response>
            [HttpGet("{id\}")]
            [ProducesResponseType(typeof(Get$model.Entity.Name$Dto), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
            public async Task<ActionResult<Get$model.Entity.Name$Dto>> GetById($model.Entity.MethodSignatureParameter$)
            {
                try
                {
                    var result = await _service.GetByIdAsync($model.Entity.KeyParameterLine$);
                    if (result == null)
                    {
                        return NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = \$"$model.Entity.Name$ with ID $model.Entity.StringConcatParameters$ was not found",
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
                    _logger.LogError(ex, \$"Error retrieving $model.Entity.Name$ with ID $model.Entity.StringConcatParameters$");
                    return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An error occurred while retrieving the entity",
                        Status = StatusCodes.Status500InternalServerError
                    \});
                \}
            \}

    
            /// <summary>
            /// Creates a new $model.Entity.Name$ entity
            /// </summary>
            /// <param name="createDto">The data to create the entity</param>
            /// <returns>The created entity</returns>
            /// <response code="201">Returns the newly created entity</response>
            /// <response code="400">If the data is invalid</response>
            /// <response code="500">If there was an internal server error</response>
            [HttpPost]
            [ProducesResponseType(typeof(Create$model.Entity.Name$Dto), StatusCodes.Status201Created)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
            public async Task<ActionResult<Create$model.Entity.Name$Dto>> Create([FromBody] Create$model.Entity.Name$Dto createDto)
            {
                try
                {
                    var result = await _service.CreateAsync(createDto);
                    return Ok(result);
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
                    _logger.LogError(ex, "Error creating new $model.Entity.Name$");
                    return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An error occurred while creating the entity",
                        Status = StatusCodes.Status500InternalServerError
                    \});
                \}
            \}
    
            /// <summary>
            /// Updates an existing $model.Entity.Name$ entity
            /// </summary>
            /// <param name="id">The ID of the entity to update</param>
            /// <param name="updateDto">The data to update the entity</param>
            /// <returns>The updated entity</returns>
            /// <response code="200">Returns the updated entity</response>
            /// <response code="400">If the data is invalid</response>
            /// <response code="404">If the entity was not found</response>
            /// <response code="500">If there was an internal server error</response>
            [HttpPut("{id\}")]
            [ProducesResponseType(typeof(Update$model.Entity.Name$Dto), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
            public async Task<ActionResult<Get$model.Entity.Name$Dto>> Update($model.Entity.MethodSignatureParameter$, [FromBody] Update$model.Entity.Name$Dto updateDto)
            {
                try
                {
                    var result = await _service.UpdateAsync($model.Entity.KeyParameterLine$, updateDto);
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
                    _logger.LogError(ex, \$"Error updating $model.Entity.Name$ with ID $model.Entity.StringConcatParameters$");
                    return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An error occurred while updating the entity",
                        Status = StatusCodes.Status500InternalServerError
                    \});
                \}
            \}
    
            /// <summary>
            /// Deletes a specific $model.Entity.Name$ entity
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
            public async Task<IActionResult> Delete($model.Entity.MethodSignatureParameter$)
            {
                try
                {
                    await _service.DeleteAsync($model.Entity.KeyParameterLine$);
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
                    _logger.LogError(ex, \$"Error deleting $model.Entity.Name$ with ID $model.Entity.StringConcatParameters$");
                    return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An error occurred while deleting the entity",
                        Status = StatusCodes.Status500InternalServerError
                    \});
                \}
            \}
        $endif$
    $endif$
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
\}