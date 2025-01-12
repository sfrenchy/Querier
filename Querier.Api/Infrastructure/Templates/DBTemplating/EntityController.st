using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using $nameSpace$.DTOs;
using $nameSpace$.Services;
using Querier.Api.Domain.Common.Models;

namespace $nameSpace$.Controllers
{
    $entityList: {entity|
    [Authorize]
    [ApiController]
    [Route("api/v1/$contextRoute$/$entity.Name$")]
    public class $entity.Name$Controller : ControllerBase
    {
        private readonly ILogger<$entity.Name$Controller> _logger;
        private readonly I$entity.Name$Service _service;

        public $entity.Name$Controller(
            I$entity.Name$Service service,
            ILogger<$entity.Name$Controller> logger)
        {
            _service = service;
            _logger = logger;
        \}

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<$entity.Name$Dto>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] PaginationParametersDto? parameters = null)
        {
            _logger.LogInformation("GetAll $entity.Name$ action called");
            var result = await _service.GetAllAsync(parameters);
            return Ok(result);
        \}

        [HttpGet("{id\}")]
        [ProducesResponseType(typeof($entity.Name$Dto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById($entity.KeyType$ id)
        {
            _logger.LogInformation("GetById $entity.Name$ action called with id: " + id);
            var result = await _service.GetByIdAsync(id);
            
            if (result == null)
                return NotFound();
                
            return Ok(result);
        \}

        [HttpPost]
        [ProducesResponseType(typeof($entity.Name$Dto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] Create$entity.Name$Dto createDto)
        {
            _logger.LogInformation("Create $entity.Name$ action called");
            var result = await _service.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = result.$entity.KeyName$ \}, result);
        \}

        [HttpPut("{id\}")]
        [ProducesResponseType(typeof($entity.Name$Dto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Update($entity.KeyType$ id, [FromBody] Update$entity.Name$Dto updateDto)
        {
            _logger.LogInformation("Update $entity.Name$ action called with id: " + id);
            var result = await _service.UpdateAsync(id, updateDto);
            
            if (result == null)
                return NotFound();
                
            return Ok(result);
        \}

        [HttpDelete("{id\}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete($entity.KeyType$ id)
        {
            _logger.LogInformation("Delete $entity.Name$ action called with id: " + id);
            var result = await _service.DeleteAsync(id);
            
            if (!result)
                return NotFound();
                
            return NoContent();
        \}
    \}
    }$
\} 