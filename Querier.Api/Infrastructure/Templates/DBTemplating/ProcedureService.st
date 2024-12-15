using $nameSpace$;
using $nameSpace$.ProcedureParameters;
using $nameSpace$.ProcedureReportRequests;
using $nameSpace$.ProcedureResultSets;
using Querier.Api.Tools;
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
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Domain.Common.Attributes;
using Querier.Api.Infrastructure.Database.Services.Base;

namespace $nameSpace$.Services
{
    $procedureList: {procedure|
    #region $procedure.Name$
    public interface I$procedure.CSName$Service $if (procedure.HasParameters && procedure.HasOutput)$: IDynamicContextProcedureWithParamsAndResult$endif$ $if (procedure.HasParameters && !procedure.HasOutput)$: IDynamicContextProcedureWithParams$endif$
    {
        Task$if (procedure.HasOutput)$<List<$procedure.CSName$Result>?>$endif$ ExecuteAsync($if (procedure.HasParameters)$$procedure.CSName$Params parameters$endif$);
        $if (procedure.HasOutput)$
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
        public async Task ExecuteAsync(Dictionary<string, object> parameters)
        {
            $procedure.CSName$Params p = new $procedure.CSName$Params();

            $procedure.Parameters: {parameter|
            p.$parameter.CSName$ = GetParameterValue<$parameter.CSType$>(parameters["$parameter.CSName$"]);
            }$
            
            await ExecuteAsync(p);
        \}
        $endif$

        $if (procedure.HasParameters && procedure.HasOutput)$
        public async Task<List<dynamic>?> ExecuteAsync(Dictionary<string, object> parameters)
        {
            $procedure.CSName$Params p = new $procedure.CSName$Params();

            $procedure.Parameters: {parameter|
            p.$parameter.CSName$ = GetParameterValue<$parameter.CSType$>(parameters["$parameter.CSName$"]);
            }$
            
            return ((IEnumerable<dynamic>) await ExecuteAsync(p)).ToList();
        \}
        $endif$

        public virtual async Task$if (procedure.HasOutput)$<List<$procedure.CSName$Result>?>$endif$ ExecuteAsync($if (procedure.HasParameters)$$procedure.CSName$Params parameters$endif$)
        {
            _logger.LogInformation("$procedure.CSName$ExecuteAsync");
            using (var context = _contextFactory.CreateDbContext())
            {
            $if (procedure.HasOutput)$
                var _ = new List<$procedure.CSName$Result>();
                var cacheKey = "$procedure.CSName$ExecuteAsync"$if (procedure.HasParameters)$ + Utils.ComputeMd5Hash(Utils.ObjectToByteArray(parameters))$endif$;
                byte[] objectFromCache = await _memoryCache.GetAsync(cacheKey);
                DateTime s = DateTime.Now;
                if (objectFromCache == null)
                {
                    _logger.LogInformation("$procedure.CSName$ExecuteAsync: Cache miss");
                    _ = await context.Procedures.$procedure.CSName$Async($if (procedure.HasParameters)$parameters$endif$);
                    _logger.LogInformation(\$"$procedure.CSName$ExecuteAsync: Time to get datas:", (DateTime.Now - s).TotalSeconds);
                    await _memoryCache.SetAsync(cacheKey, Utils.ObjectToByteArray(_), DefaultDistributedCacheExpiryOptions);
                \}
                else
                {
                    _logger.LogInformation("$procedure.CSName$ExecuteAsync: Cache hit");
                    using (StreamReader sr = new StreamReader(new MemoryStream(objectFromCache)))
                    {
                        _ = JsonConvert.DeserializeObject<List<$procedure.CSName$Result>>(sr.ReadToEnd());
                        _logger.LogInformation(\$"$procedure.CSName$ExecuteAsync: Time to get datas:", (DateTime.Now - s).TotalSeconds);
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
        $endif$
    \}
    #endregion

    }$
\}