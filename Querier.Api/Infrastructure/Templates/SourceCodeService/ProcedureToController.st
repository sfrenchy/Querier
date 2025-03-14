using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Common.Attributes;
using Querier.Api.Infrastructure.Database.Parameters;
using Querier.Api.Infrastructure.Base.Exceptions;
using $model.RootNamespace$.Services.Procedure;
using $model.RootNamespace$.Dtos.Procedure;

namespace $model.RootNamespace$.Controllers;

/// <summary>
/// Controller for managing $model.CSName$ stored procedure
/// </summary>
[Authorize]
[ApiController]
[ControllerFor(StoredProcedure = "$model.Name$")]
[Route("api/v1/$model.ApiRoute$/procedure/$model.CSName$")]
[Produces("application/json")]
public class $model.CSName$Controller : ControllerBase {
    private readonly I$model.CSName$Service _service;
    private readonly ILogger<$model.CSName$Controller> _logger;

    public $model.CSName$Controller(
        I$model.CSName$Service service,
        ILogger<$model.CSName$Controller> logger) {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    \}

    /// <summary>
    /// Executes the $model.CSName$ stored procedure
    /// </summary>
    /// <param name="parameters">Parameters for the stored procedure</param>
    /// <returns>Results from the stored procedure execution</returns>
    /// <response code="200">Returns the results from the stored procedure</response>
    /// <response code="400">If the parameters are invalid</response>
    /// <response code="500">If there was an error executing the stored procedure</response>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(List<$model.CSName$OutputDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    $if(model.HasParameters)$
    public async Task<ActionResult<List<$model.CSName$OutputDto>>> Execute([FromBody] $model.CSName$InputDto parameters) {
    $else$
    public async Task<ActionResult<List<$model.CSName$OutputDto>>> Execute() {
    $endif$
        try {
            $if(model.HasParameters)$
                if (parameters == null) {
                    throw new EntityValidationException("$model.RootNamespace$", "$model.CSName$", "Parameters cannot be null");
                \}
            $endif$
            
            $model.Parameters: {param|
            $if(!param.IsOutput && !param.IsNullable)$
            // Validate required parameters
            if (parameters.$param.CSName$ == null) {
                throw new EntityValidationException("$model.RootNamespace$", "$model.CSName$", "Parameter $param.CSName$ is required");
            \}
            $endif$
            }$

            $if(model.HasParameters)$
                var results = await _service.ExecuteAsync(parameters);
            $else$
                var results = await _service.ExecuteAsync();
            $endif$
            return Ok(results);
        \}
        catch (EntityValidationException ex) {
            _logger.LogWarning(ex, "Validation error in Execute");
            return BadRequest(new ProblemDetails {
                Title = "Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            \});
        \}
        catch (StoredProcedureException ex) {
            _logger.LogError(ex, "Error executing stored procedure");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails {
                Title = "Stored Procedure Error",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            \});
        \}
        catch (Exception ex) {
            _logger.LogError(ex, "Unexpected error executing stored procedure");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while executing the stored procedure",
                Status = StatusCodes.Status500InternalServerError
            \});
        \}
    \}

    /// <summary>
    /// Gets metadata about the stored procedure parameters
    /// </summary>
    /// <returns>Information about the stored procedure parameters</returns>
    /// <response code="200">Returns the parameter metadata</response>
    /// <response code="500">If there was an error retrieving the metadata</response>
    [HttpGet("metadata")]
    [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<Dictionary<string, object>> GetMetadata() {
        try {
            var metadata = new Dictionary<string, object> {
                ["name"] = "$model.Name$",
                $if(procedure.HasParameters)$
                ["parameters"] = new[] {
                    $model.Parameters: {param|
                    new {
                        name = "$param.CSName$",
                        type = "$param.CSType$",
                        isNullable = $if (param.IsNullable)$true$else$false$endif$,
                        isOutput = $if (param.IsOutput)$true$else$false$endif$
                    \},
                    }$
                \},
                $endif$
                ["resultColumns"] = new[] {
                    $model.OutputSet: {column|
                    new {
                        name = "$column.CSName$",
                        type = "$column.CSType$"
                    \},
                    }$
                \}
            \};

            return Ok(metadata);
        \}
        catch (Exception ex) {
            _logger.LogError(ex, "Error retrieving stored procedure metadata");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving stored procedure metadata",
                Status = StatusCodes.Status500InternalServerError
            \});
        \}
    \}
\}