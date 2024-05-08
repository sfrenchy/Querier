using Querier.Api.Models;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Requests;
using Querier.Api.Models.Responses;
using Querier.Api.Services;
using Querier.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Notifications.MQMessages;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EntityCRUDController : ControllerBase
    {
        private readonly ILogger<EntityCRUDController> _logger;
        private readonly IEntityCRUDService _entityCRUDService;
        private readonly IToastMessageEmitterService _toastMessageEmitterService;

        public EntityCRUDController(IEntityCRUDService entityCRUDService, ILogger<EntityCRUDController> logger, IToastMessageEmitterService toastMessageEmitterService)//IKLogger logger)
        {
            _logger = logger;
            _entityCRUDService = entityCRUDService;
            _toastMessageEmitterService = toastMessageEmitterService;
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
        public IActionResult GetEntiy(string contextTypeName, string entityName)
        {
            return new OkObjectResult(_entityCRUDService.GetEntity(contextTypeName, entityName));
        }

        [HttpPost("Read")]
        public IActionResult Read([FromBody] CRUDReadRequest model)
        {
            ServerSideResponse<dynamic> response;
            var datas = _entityCRUDService.Read(model.ContextTypeName, model.EntityType, model.Filters, out Type entityType);

            var dataTableFilterArgs = new object[] { datas, model.DatatableParams, null };
            object result = typeof(ExtensionMethods)
                .GetMethod(nameof(ExtensionMethods.DatatableFilter))
                .MakeGenericMethod(datas.GetType().GetGenericArguments().Single())
                .Invoke(null, dataTableFilterArgs);

            var typedResult = (List<dynamic>)(typeof(ExtensionMethods)
                .GetMethod(nameof(ExtensionMethods.CastListToDynamic))
                .MakeGenericMethod(datas.GetType().GetGenericArguments().Single())
                .Invoke(null, new object[] { result }));

            response = new ServerSideResponse<dynamic>();

            response.draw = model.DatatableParams.draw;
            response.recordsTotal = datas.Count();
            response.recordsFiltered = (int)dataTableFilterArgs[2];
            response.data = typedResult;
            response.sums = new Dictionary<string, object>();

            return new OkObjectResult(response);
        }

        [HttpPost("ReadFromSql")]
        public IActionResult ReadFromSql([FromBody] CRUDReadSqlQueryRequest request)
        {
            ServerSideResponse<object> response;
            var datas = _entityCRUDService.ReadFromSql(request.ContextTypeName, request.SqlQuery, request.Filters);

            var dataTableFilterArgs = new object[] { datas, request.DatatableParams, null };
            object result = typeof(ExtensionMethods)
                .GetMethod(nameof(ExtensionMethods.DatatableFilter))
                .MakeGenericMethod(datas.GetType().GetGenericArguments().Single())
                .Invoke(null, dataTableFilterArgs);

            var typedResult = (List<dynamic>)(typeof(ExtensionMethods)
                .GetMethod(nameof(ExtensionMethods.CastListToDynamic))
                .MakeGenericMethod(datas.GetType().GetGenericArguments().Single())
                .Invoke(null, new object[] { result }));

            response = new ServerSideResponse<object>();

            response.draw = request.DatatableParams.draw;
            response.recordsTotal = datas.Count();
            response.recordsFiltered = (int)dataTableFilterArgs[2];
            response.data = typedResult;
            response.sums = new Dictionary<string, object>();

            return new OkObjectResult(response);
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

            if (res.QuerySuccessful == false)
            {
                ToastMessage sqlQueryStatusNotification = new ToastMessage();
                sqlQueryStatusNotification.Type = ToastType.Danger;
                sqlQueryStatusNotification.TitleCode = $"Sql query error";
                sqlQueryStatusNotification.ContentCode = "The Sql query you entered is invalid";
                sqlQueryStatusNotification.Recipient = "admin@herdia.fr";
                sqlQueryStatusNotification.Closable = false;
                sqlQueryStatusNotification.Persistent = false;
                _toastMessageEmitterService.PublishToast(sqlQueryStatusNotification);
            }
            
            return new OkObjectResult(res);
        }
    }
}
