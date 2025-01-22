using $nameSpace$;
using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Infrastructure.Database.Extensions;
using Querier.Api.Infrastructure.Database.Parameters;

namespace $nameSpace$
{
    public partial class $contextNameSpace$
    {
        private $contextNameSpace$Procedures? _procedures;

        public $contextNameSpace$Procedures Procedures
        {
            get
            {
                if (_procedures is null) _procedures = new $contextNameSpace$Procedures(this);
                return _procedures;
            \}
            set
            {
                _procedures = value;
            \}
        \}

        public $contextNameSpace$Procedures GetProcedures()
        {
            return Procedures;
        \}

        public string ContextName
        {
            get
            {
                return "$contextNameSpace$";
            \}
        \}
    \}

    public partial class $contextNameSpace$Procedures
    {
        private readonly $contextNameSpace$ _context;

        public $contextNameSpace$Procedures($contextNameSpace$ context)
        {
            _context = context;
        }

        $procedureList: {procedure|
        [DisplayName("$procedure.Name$")]
        public virtual async $procedure.CSReturnSignature$ $procedure.CSName$Async($procedure.CSParameterSignature$)
        {
            var parameterreturnValue = new SqlParameter
            {
                ParameterName = "returnValue",
                Direction = System.Data.ParameterDirection.Output,
                SqlDbType = System.Data.SqlDbType.Int,
            \};

            var sqlParameters = new []
            {
                $procedure.Parameters: {parameter|
                    new SqlParameter
                    {
                        ParameterName = "$parameter.Name$",
                        Value = inputDto.$parameter.CSName$ ?? Convert.DBNull,
                        SqlDbType = $parameter.SqlParameterType$
                    \},
                }$
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