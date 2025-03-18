﻿using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using MimeKit;
using Querier.Api.Infrastructure.Database.Templates;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Querier.Api.Domain.Services
{
    public class StoredProcedureParameter
    {
        public string Schema { get; set; }
        public string ProcedureName { get; set; }
        public string ParameterName { get; set; }
        public string DataType { get; set; }
        public int Length { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }
        public int ParameterOrder { get; set; }
        public string Collation { get; set; }
        public bool IsOutput { get; set; }
        public bool IsNullable { get; set; }
    }

    public abstract class ProcedureMetadataExtractorBase
    {
        protected readonly DatabaseModel DbModel;
        public ProcedureMetadataExtractorBase(DatabaseModel dbModel)
        {
            DbModel = dbModel;
        }
        protected abstract string GetStoredProcedureSqlCreate(string procedureName, string schema);
        protected List<StoredProcedureMetadata> _procedureMetadata = new();
        protected string ConnectionString;
        protected abstract string GetProcedureWithParametersQuery { get; }
        protected abstract DbConnection Connection { get; }
        protected abstract void ExtractProcedureOutputMetadata();
        public List<StoredProcedureMetadata> ProcedureMetadata => _procedureMetadata;
        protected abstract string GetCSharpType(string sqlType);
        protected void ExtractMetadata()
        {
            if (string.IsNullOrEmpty(GetProcedureWithParametersQuery))
                return;
            List<StoredProcedureParameter> parameters = new();
            DbCommand listStoredProcedureWithParameterCommand = Connection.CreateCommand();
            listStoredProcedureWithParameterCommand.CommandText = GetProcedureWithParametersQuery;
            using (var parameterReader = listStoredProcedureWithParameterCommand.ExecuteReader())
            {
                while (parameterReader.Read())
                {
                    if (parameterReader["ParameterName"] == DBNull.Value)
                        continue;
                    var parameter = new StoredProcedureParameter
                    {
                        Schema = parameterReader["SchemaName"].ToString(),
                        ProcedureName = parameterReader["ProcedureName"].ToString(),
                        ParameterName = parameterReader["ParameterName"].ToString(),
                        DataType = parameterReader["DataType"].ToString(),
                        Length = parameterReader["Length"] == DBNull.Value ? 0 : Convert.ToInt32(parameterReader["Length"]),
                        Precision = parameterReader["Precision"] == DBNull.Value ? 0 : Convert.ToInt32(parameterReader["Precision"]),
                        Scale = parameterReader["Scale"] == DBNull.Value ? 0 : Convert.ToInt32(parameterReader["Scale"]),
                        ParameterOrder = Convert.ToInt32(parameterReader["ParameterOrder"]),
                        Collation = parameterReader["Collation"].ToString(),
                        IsOutput = Convert.ToInt32(parameterReader["IsOutput"]) == 1,
                        IsNullable = Convert.ToInt32(parameterReader["IsNullable"]) == 1
                    };
                    parameters.Add(parameter);
                }
            }
            
            var parametersByProcedure = parameters.GroupBy(p => p.ProcedureName);

            _procedureMetadata = parametersByProcedure.Select(group =>
            {
                var procedureMetadata = new StoredProcedureMetadata
                {
                    Schema = group.First().Schema,
                    Name = group.First().ProcedureName,
                    CSName = NormalizeCsString(group.First().ProcedureName),
                    Parameters = group.OrderBy(p => p.ParameterOrder).Select(p => new TemplateProperty
                    {
                        Name = p.ParameterName,
                        CSName = NormalizeCsString(p.ParameterName),
                        IsKey = false,
                        IsForeignKey = false,
                        IsRequired = true,
                        IsAutoGenerated = false,
                        CSType = GetCSharpType(p.DataType)
                    }).ToList()
                };
                return procedureMetadata;
            }).ToList();
            ExtractProcedureOutputMetadata();
        }

        protected string NormalizeCsString(string str)
        {
            string csName = str.Replace("@", "");
            csName = csName.Replace("p_", "");
            csName = csName.Replace("P_", "");
            return ToPascalCase(csName);
        }

        protected string ToPascalCase(string str)
        {
            // Replace all non-letter and non-digits with an underscore and lowercase the rest.
            string sample = string.Join("", str?.Select(c => char.IsLetterOrDigit(c) ? c.ToString().ToLower() : "_").ToArray());

            // Split the resulting string by underscore
            // Select first character, uppercase it and concatenate with the rest of the string
            var arr = sample?
                .Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => $"{s.Substring(0, 1).ToUpper()}{s.Substring(1)}");

            // Join the resulting collection
            sample = string.Join("", arr);

            return sample;
        }

        protected bool TryAIOutputMetadataExtraction(StoredProcedureMetadata procedure, List<TemplateProperty> result)
        {
            string sqlProcedureCode = GetStoredProcedureSqlCreate(procedure.Name, procedure.Schema);
            return false;
        }
    }
}
