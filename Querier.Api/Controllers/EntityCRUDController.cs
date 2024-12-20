using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Querier.Api.Tools;
using Querier.Api.Application.DTOs.Requests.Entity;
using Querier.Api.Application.DTOs.Responses.Entity;
using Querier.Api.Domain.Services;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EntityCRUDController : ControllerBase
    {
        private readonly IEntityCRUDService _entityCRUDService;
        private readonly ILogger<EntityCRUDController> _logger;

        public EntityCRUDController(IEntityCRUDService entityCRUDService, ILogger<EntityCRUDController> logger)
        {
            _logger = logger;
            _entityCRUDService = entityCRUDService;
        }

        [HttpGet("GetContexts")]
        public IActionResult GetContexts()
        {
            return new OkObjectResult(_entityCRUDService.GetContexts());
        }

        [HttpGet("GetEntities")]
        public IActionResult GetEntities(string contextTypeName)
        {
            return new OkObjectResult(_entityCRUDService.GetEntities(contextTypeName));
        }

        [HttpGet("GetEntity")]
        public IActionResult GetEntity(string contextTypeName, string entityName)
        {
            return new OkObjectResult(_entityCRUDService.GetEntity(contextTypeName, entityName));
        }

        [HttpGet("GetAll")]
        public IActionResult GetAll(
            [FromQuery] string contextTypeName, 
            [FromQuery] string entityTypeName,
            [FromQuery] int pageNumber = 0,
            [FromQuery] int pageSize = 0)
        {
            var paginationParams = new PaginationParameters 
            { 
                PageNumber = pageNumber,
                PageSize = pageSize 
            };
            
            var result = _entityCRUDService.GetAll(contextTypeName, entityTypeName, paginationParams);
            return Ok(new PagedResult<object>
            {
                Data = result.Data,
                TotalCount = result.TotalCount
            });
        }

        [HttpPost("ReadFromSql")]
        public IActionResult ReadFromSql([FromBody] CRUDReadSqlQueryRequest request)
        {
            var datas = _entityCRUDService.ReadFromSql(request.ContextTypeName, request.SqlQuery, request.Filters);
            return new OkObjectResult(datas);
        }


        [HttpPost("Create")]
        public IActionResult Create([FromBody] CRUDCreateOrUpdateRequest model)
        {
            CRUDCreateOrUpdateResponse response = new CRUDCreateOrUpdateResponse();
            response.NewEntity = _entityCRUDService.Create(model.ContextTypeName, model.EntityType, model.Data);
            return new OkObjectResult(response);
        }

        [HttpPost("Update")]
        public IActionResult Update([FromBody] CRUDCreateOrUpdateRequest model)
        {
            CRUDCreateOrUpdateResponse response = new CRUDCreateOrUpdateResponse();
            response.NewEntity = _entityCRUDService.Update(model.ContextTypeName, model.EntityType, model.Data);
            return new OkObjectResult(response);
        }

        [HttpPost("CreateOrUpdate")]
        public IActionResult CreateOrUpdate([FromBody] CRUDCreateOrUpdateRequest model)
        {
            CRUDCreateOrUpdateResponse response = new CRUDCreateOrUpdateResponse();
            response.NewEntity = _entityCRUDService.CreateOrUpdate(model.ContextTypeName, model.EntityType, model.Data);
            return new OkObjectResult(response);
        }

        [HttpDelete("Delete")]
        public IActionResult Delete([FromBody] CRUDDeleteRequest model)
        {
            _entityCRUDService.Delete(model.ContextTypeName, model.EntityType, model.Key);
            return new OkResult();
        }

        [HttpPost("GetSQLQueryEntityDefinition")]
        public IActionResult GetSQLQueryEntityDefinition([FromBody] CRUDExecuteSQLQueryRequest request)
        {
            var res = _entityCRUDService.GetSQLQueryEntityDefinition(request);
            return new OkObjectResult(res);
        }
    }
}
