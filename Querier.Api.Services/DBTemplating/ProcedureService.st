using $nameSpace$;
using $nameSpace$.ProcedureParameters;
using $nameSpace$.ProcedureReportRequests;
using $nameSpace$.ProcedureResultSets;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Interfaces;
using Querier.Tools;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Linq;
using System.IO;
using Querier.Api.Models.QDBConnection;
using Querier.Api.Models.Attributes;

namespace $nameSpace$.Services
{
    $procedureList: {procedure|
    #region $procedure.Name$
    public interface I$procedure.CSName$Service $if (procedure.HasParameters && procedure.HasOutput)$: IDynamicContextProcedureWithParamsAndResult$endif$ $if (procedure.HasParameters && !procedure.HasOutput)$: IDynamicContextProcedureWithParams$endif$
    {
        Task$if (procedure.HasOutput)$<List<$procedure.CSName$Result>?>$endif$ DatasAsync($if (procedure.HasParameters)$$procedure.CSName$Params parameters$endif$);
        $if (procedure.HasOutput)$
        Task<ServerSideResponse<$procedure.CSName$Result>?> ReportDatasAsync($procedure.CSName$ReportRequest datatableRequest);
		List<string> GetSummableColumns();
        $endif$
    \}
    [DynamicContextProcedure(ContextName="$nameSpace$", ServiceName="I$procedure.CSName$Service")]
    public partial class StoredProcedure$procedure.CSName$Service : $if (procedure.HasOutput)$DynamicContextServiceBaseWithResult$else$DynamicContextServiceBase$endif$, I$procedure.CSName$Service
    {
        protected readonly IDbContextFactory<$contextNameSpace$> _contextFactory;
        protected readonly IDistributedCache _memoryCache;
        protected readonly ILogger<StoredProcedure$procedure.CSName$Service> _logger;

        public StoredProcedure$procedure.CSName$Service(IDbContextFactory<$contextNameSpace$> contextFactory, ILogger<StoredProcedure$procedure.CSName$Service> logger, IDistributedCache memoryCache)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _memoryCache = memoryCache;
        \}

        $if (procedure.HasParameters && !procedure.HasOutput)$
        public async Task DatasAsync(Dictionary<string, object> parameters)
        {
            $procedure.CSName$Params p = new $procedure.CSName$Params();

            $procedure.Parameters: {parameter|
            p.$parameter.CSName$ = GetParameterValue<$parameter.CSType$>(parameters["$parameter.CSName$"]);
            }$
            
            await DatasAsync(p);
        \}
        $endif$

        $if (procedure.HasParameters && procedure.HasOutput)$
        public async Task<List<dynamic>?> DatasAsync(Dictionary<string, object> parameters)
        {
            $procedure.CSName$Params p = new $procedure.CSName$Params();

            $procedure.Parameters: {parameter|
            p.$parameter.CSName$ = GetParameterValue<$parameter.CSType$>(parameters["$parameter.CSName$"]);
            }$
            
            return ((IEnumerable<dynamic>) await DatasAsync(p)).ToList();
        \}

        public async Task<ServerSideResponse<dynamic>?> ReportDatasAsync(Dictionary<string, object> parameters, ServerSideRequest DatatableParams)
        {
            $procedure.CSName$Params p = new $procedure.CSName$Params();

            $procedure.Parameters: {parameter|
            p.$parameter.CSName$ = GetParameterValue<$parameter.CSType$>(parameters["$parameter.CSName$"]);
            }$

            $procedure.CSName$ReportRequest r = new $procedure.CSName$ReportRequest();
            r.DatatableParams = DatatableParams;
            r.ProcedureParams = p;
            ServerSideResponse<$procedure.CSName$Result> typedResult = await this.ReportDatasAsync(r);

            ServerSideResponse<dynamic> _ = new ServerSideResponse<dynamic>();
            _.draw = typedResult.draw;
            _.recordsTotal = typedResult.recordsTotal;
            _.recordsFiltered = typedResult.recordsFiltered;
            _.data = typedResult.data.CastToDynamic();
            _.sums = typedResult.sums;

            return _;
        \}
        $endif$

        public virtual async Task$if (procedure.HasOutput)$<List<$procedure.CSName$Result>?>$endif$ DatasAsync($if (procedure.HasParameters)$$procedure.CSName$Params parameters$endif$)
        {
            _logger.LogInformation("$procedure.CSName$DatasAsync");
            using (var context = _contextFactory.CreateDbContext())
            {
            $if (procedure.HasOutput)$
                var _ = new List<$procedure.CSName$Result>();
                var cacheKey = "$procedure.CSName$DatasAsync"$if (procedure.HasParameters)$ + Utils.ComputeMd5Hash(Utils.ObjectToByteArray(parameters))$endif$;
                byte[] objectFromCache = await _memoryCache.GetAsync(cacheKey);
                DateTime s = DateTime.Now;
                if (objectFromCache == null)
                {
                    _logger.LogInformation("$procedure.CSName$DatasAsync: Cache miss");
                    _ = await context.Procedures.$procedure.CSName$Async($if (procedure.HasParameters)$parameters$endif$);
                    _logger.LogInformation(\$"$procedure.CSName$DatasAsync: Time to get datas:", (DateTime.Now - s).TotalSeconds);
                    await _memoryCache.SetAsync(cacheKey, Utils.ObjectToByteArray(_), DefaultDistributedCacheExpiryOptions);
                \}
                else
                {
                    _logger.LogInformation("$procedure.CSName$DatasAsync: Cache hit");
                    using (StreamReader sr = new StreamReader(new MemoryStream(objectFromCache)))
                    {
                        _ = JsonConvert.DeserializeObject<List<$procedure.CSName$Result>>(sr.ReadToEnd());
                        _logger.LogInformation(\$"$procedure.CSName$DatasAsync: Time to get datas:", (DateTime.Now - s).TotalSeconds);
                    \}
                \}
                return _;
            \}
            $else$
                await context.Procedures.$procedure.CSName$Async($if (procedure.HasParameters)$parameters$endif$);
            \}
            $endif$
        \}

        $if (procedure.HasOutput)$
        public override List<string> GetSummableColumns()
        {
            return new List<string> {
            $procedure.SummableOutputColumns:{summableOutputColumn|
                "$summableOutputColumn$",
            }$
            \};
        \}

        public virtual async Task<ServerSideResponse<$procedure.CSName$Result>?> ReportDatasAsync($procedure.CSName$ReportRequest datatableRequest)
        {
            _logger.LogInformation("$procedure.CSName$ReportDatasAsync");
            var _ = new ServerSideResponse<$procedure.CSName$Result>();
            var cacheKey = "$procedure.CSName$ReportDatasAsync" + Utils.ComputeMd5Hash(Utils.ObjectToByteArray(datatableRequest));
            byte[] objectFromCache = await _memoryCache.GetAsync(cacheKey);
            DateTime s = DateTime.Now;
            if (objectFromCache == null)
            {
                _logger.LogInformation("$procedure.CSName$ReportDatasAsync: Cache miss");
                int? countFiltered = null;
                List<$procedure.CSName$Result>? datas = await DatasAsync($if (procedure.HasParameters)$datatableRequest.ProcedureParams$endif$);
                List<$procedure.CSName$Result>? filteredDatas = datas?.DatatableFilter(datatableRequest.DatatableParams, out countFiltered);
                _logger.LogInformation(\$"Time to get datas:", (DateTime.Now - s).TotalSeconds);
                _.draw = datatableRequest.DatatableParams.draw;
                _.recordsTotal = datas == null ? 0 : datas.Count;
                _.recordsFiltered = countFiltered ?? 0;
                _.data = filteredDatas?.ToList();
                _.sums = ComputeReportSums(datas);

                await _memoryCache.SetAsync(cacheKey, Utils.ObjectToByteArray(_), DefaultDistributedCacheExpiryOptions);
            \}
            else
            {
                _logger.LogInformation("$procedure.CSName$ReportDatasAsync: Cache hit");
                using (StreamReader sr = new StreamReader(new MemoryStream(objectFromCache)))
                {
                    _ = JsonConvert.DeserializeObject<ServerSideResponse<$procedure.CSName$Result>>(sr.ReadToEnd());
                    _logger.LogInformation(\$"$procedure.CSName$ReportDatasAsync: Time to get datas:", (DateTime.Now - s).TotalSeconds);
                \}
            \}
            return _;
        \}
        $endif$
    \}
    #endregion

    }$
\}