using System;
using System.Collections.Generic;
using Querier.Api.Models.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace $nameSpace$.Services
{
    public class $contextNameSpace$ProceduresServicesResolver : IDynamicContextProceduresServicesResolver
    {
        private readonly Dictionary<Type, Type> _proceduresServices = new Dictionary<Type, Type>();
        private readonly Dictionary<string, Type> _procedureNameService = new Dictionary<string, Type>();

        public $contextNameSpace$ProceduresServicesResolver()
        {
            $procedureList: {procedure|
                _proceduresServices.Add(typeof(I$procedure.CSName$Service), typeof(StoredProcedure$procedure.CSName$Service));
            }$

            $procedureList: {procedure|
                _procedureNameService.Add("$procedure.CSName$", typeof(I$procedure.CSName$Service));
            }$
        \}

        public Dictionary<Type, Type> ProceduresServices
        {
            get
            {
                return _proceduresServices;
            \}
        \}

        public Dictionary<string, Type> ProcedureNameService
        {
            get
            {
                return _procedureNameService;
            \}
        \}

        public void ConfigureServices(IServiceCollection services, string connectionString)
        {
            services.AddDbContext<$contextNameSpace$>(options => options.UseSqlServer(connectionString));
            services.AddDbContextFactory<$contextNameSpace$>(options => options.UseSqlServer(connectionString));
        \}

        public string DynamicContextName { get { return "$contextNameSpace$"; \} \}
    \}
\}