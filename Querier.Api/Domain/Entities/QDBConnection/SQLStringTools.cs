using System;
using System.Linq;

namespace Querier.Api.Domain.Entities.QDBConnection
{
    public enum MSSQLNativeType
    {
        varbinary,//(1)
        binary,//(1)
        image,
        varchar,
        @char,
        nvarchar,//(1)
        nchar,//(1)
        text,
        ntext,
        uniqueidentifier,
        rowversion,
        bit,
        tinyint,
        smallint,
        @int,
        bigint,
        smallmoney,
        money,
        numeric,
        @decimal,
        real,
        @float,
        smalldatetime,
        datetime,
        sql_variant,
        table,
        cursor,
        timestamp,
        xml,
        date,
        datetime2,
        datetimeoffset,
        filestream,
        time,
    }

    public static class SQLStringTools
    {
        public static string ToPascalCase(string str)
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

        public static string NormalizeCSString(string str)
        {
            string csName = str.Replace("@", "");
            csName = csName.Replace("p_", "");
            csName = csName.Replace("P_", "");
            return ToPascalCase(csName);
        }

        public static string NormalizeProcedureNameCSString(string str)
        {
            string csName = str.Replace("@", "");
            return ToPascalCase(csName);
        }

        public static string SQLTypeToCSSqlParameterType(string sqlType)
        {
            string csType = "";
            if (!Enum.TryParse(sqlType, out MSSQLNativeType typeCode))
            {
                throw new Exception($"sql type {sqlType} inconnu");
            }
            switch (typeCode)
            {
                case MSSQLNativeType.filestream:
                case MSSQLNativeType.binary:
                case MSSQLNativeType.varbinary:
                    csType = "System.Data.SqlDbType.VarBinary";
                    break;
                case MSSQLNativeType.image:
                    csType = "System.Data.SqlDbType.Binary";
                    break;
                case MSSQLNativeType.timestamp:
                case MSSQLNativeType.rowversion:
                    csType = "System.Data.SqlDbType.Timestamp";
                    break;
                case MSSQLNativeType.tinyint:
                    csType = "System.Data.SqlDbType.TinyInt";
                    break;
                case MSSQLNativeType.varchar:
                    csType = "System.Data.SqlDbType.VarChar";
                    break;
                case MSSQLNativeType.nvarchar:
                    csType = "System.Data.SqlDbType.NVarChar";
                    break;
                case MSSQLNativeType.nchar:
                    csType = "System.Data.SqlDbType.NChar";
                    break;
                case MSSQLNativeType.text:
                    csType = "System.Data.SqlDbType.Text";
                    break;
                case MSSQLNativeType.ntext:
                    csType = "System.Data.SqlDbType.NText";
                    break;
                case MSSQLNativeType.xml:
                    csType = "System.Data.SqlDbType.Xml";
                    break;
                case MSSQLNativeType.@char:
                    csType = "System.Data.SqlDbType.Char";
                    break;
                case MSSQLNativeType.bigint:
                    csType = "System.Data.SqlDbType.BigInt";
                    break;
                case MSSQLNativeType.bit:
                    csType = "System.Data.SqlDbType.Bit";
                    break;
                case MSSQLNativeType.smalldatetime:
                case MSSQLNativeType.datetime:
                    csType = "System.Data.SqlDbType.DateTime";
                    break;
                case MSSQLNativeType.date:
                    csType = "System.Data.SqlDbType.Date";
                    break;
                case MSSQLNativeType.datetime2:
                    csType = "System.Data.SqlDbType.DateTime2";
                    break;
                case MSSQLNativeType.datetimeoffset:
                    csType = "System.Data.SqlDbType.DateTimeOffset";
                    break;
                case MSSQLNativeType.@decimal:
                case MSSQLNativeType.money:
                case MSSQLNativeType.numeric:
                case MSSQLNativeType.smallmoney:
                    csType = "System.Data.SqlDbType.Decimal";
                    break;
                case MSSQLNativeType.@float:
                    csType = "System.Data.SqlDbType.Float";
                    break;
                case MSSQLNativeType.@int:
                    csType = "System.Data.SqlDbType.Int";
                    break;
                case MSSQLNativeType.real:
                    csType = "System.Data.SqlDbType.Real";
                    break;
                case MSSQLNativeType.smallint:
                    csType = "System.Data.SqlDbType.SmallInt";
                    break;
                case MSSQLNativeType.uniqueidentifier:
                    csType = "System.Data.SqlDbType.UniqueIdentifier";
                    break;
                case MSSQLNativeType.sql_variant:
                    csType = "System.Data.SqlDbType.Variant";
                    break;
                case MSSQLNativeType.time:
                    csType = "System.Data.SqlDbType.Time";
                    break;
                default:
                    throw new Exception("none equal type");
            }

            return csType;
        }

        public static string SQLTypeToCSType(string sqlType, bool IsNullable, int Length = 1)
        {
            string csType = "";
            if (!Enum.TryParse(sqlType, out MSSQLNativeType typeCode))
            {
                throw new Exception($"sql type {sqlType} inconnu");
            }
            switch (typeCode)
            {
                case MSSQLNativeType.varbinary:
                case MSSQLNativeType.binary:
                case MSSQLNativeType.filestream:
                case MSSQLNativeType.image:
                case MSSQLNativeType.rowversion:
                case MSSQLNativeType.timestamp://?
                    csType = "byte[]";
                    break;
                case MSSQLNativeType.tinyint:
                    csType = "byte";
                    break;
                case MSSQLNativeType.varchar:
                case MSSQLNativeType.nvarchar:
                case MSSQLNativeType.nchar:
                case MSSQLNativeType.text:
                case MSSQLNativeType.ntext:
                case MSSQLNativeType.xml:
                    csType = "string";
                    break;
                case MSSQLNativeType.@char:
                    if (Length > 1)
                        csType = "string";
                    else
                        csType = "char";
                    break;
                case MSSQLNativeType.bigint:
                    csType = "long";
                    break;
                case MSSQLNativeType.bit:
                    csType = "bool";
                    break;
                case MSSQLNativeType.smalldatetime:
                case MSSQLNativeType.datetime:
                case MSSQLNativeType.date:
                case MSSQLNativeType.datetime2:
                    csType = "DateTime";
                    break;
                case MSSQLNativeType.datetimeoffset:
                    csType = "DateTimeOffset";
                    break;
                case MSSQLNativeType.@decimal:
                case MSSQLNativeType.money:
                case MSSQLNativeType.numeric:
                case MSSQLNativeType.smallmoney:
                    csType = "decimal";
                    break;
                case MSSQLNativeType.@float:
                    csType = "double";
                    break;
                case MSSQLNativeType.@int:
                    csType = "int";
                    break;
                case MSSQLNativeType.real:
                    csType = "Single";
                    break;
                case MSSQLNativeType.smallint:
                    csType = "short";
                    break;
                case MSSQLNativeType.uniqueidentifier:
                    csType = "Guid";
                    break;
                case MSSQLNativeType.sql_variant:
                    csType = "object";
                    break;
                case MSSQLNativeType.time:
                    csType = "TimeSpan";
                    break;
                default:
                    throw new Exception("none equal type");
            }

            if (IsNullable)
                csType += "?";

            return csType;
        }
    }
}