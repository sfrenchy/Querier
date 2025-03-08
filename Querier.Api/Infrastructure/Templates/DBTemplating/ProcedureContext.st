using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Infrastructure.Database.Extensions;
using Querier.Api.Infrastructure.Database.Parameters;
using $rootNamespace$.DTOs;

namespace $contextNamespace$
{
    public partial class $contextName$
    {
        private $contextName$Procedures? _procedures;

        public $contextName$Procedures Procedures
        {
            get
            {
                if (_procedures is null) _procedures = new $contextName$Procedures(this);
                return _procedures;
            \}
            set
            {
                _procedures = value;
            \}
        \}

        public $contextName$Procedures GetProcedures()
        {
            return Procedures;
        \}

        public string ContextName
        {
            get
            {
                return "$contextName$";
            \}
        \}
    \}

    public partial class $contextName$Procedures
    {
        private readonly $contextName$ _context;

        public $contextName$Procedures($contextName$ context)
        {
            _context = context;
        }

        $procedureList: {procedure|
        [DisplayName("$procedure.Name$")]
        public virtual async Task<List<$procedure.CSName$Dto>> $procedure.CSName$Async($if(procedure.HasParameters)$$procedure.CSName$InputDto inputDto, $endif$OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default)
        {
            var parameterreturnValue = new SqlParameter
            {
                ParameterName = "returnValue",
                Direction = System.Data.ParameterDirection.Output,
                SqlDbType = System.Data.SqlDbType.Int,
            \};

            var sqlParameters = new []
            {
                $if(procedure.HasParameters)$
                $procedure.Parameters: {parameter|
                    new SqlParameter
                    {
                        ParameterName = "$parameter.Name$",
                        Value = inputDto.$parameter.CSName$ ?? Convert.DBNull,
                        SqlDbType = $parameter.SqlParameterType$
                    \},
                }$
                $endif$
                parameterreturnValue
            \};

            $if (procedure.HasOutput)$
                var _ = await _context.SqlQueryAsync<$procedure.CSName$Dto>("EXEC @returnValue = [dbo].[$procedure.Name$] $procedure.InlineParameters$", sqlParameters, cancellationToken);
            $else$
                await _context.SqlQueryAsync<object>("EXEC @returnValue = [dbo].[$procedure.Name$] $procedure.InlineParameters$", sqlParameters, cancellationToken);
            $endif$

            returnValue?.SetValue(parameterreturnValue.Value);

            $if (procedure.HasOutput)$
            return _;
            $endif$
        \}
        }$
    \}
\}