using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using NpgsqlTypes;
using System;
using System.Data;
using System.Data.Common;

namespace Querier.Api.Infrastructure.Base;

public class DynamicContextRepositoryBase
{
    protected DbParameter GetDbParameter(DbContext context, string parameterName, Type type, object Value, ParameterDirection Direction = ParameterDirection.Input)
    {
        if (context.Database.IsSqlServer())
        {
            return new Microsoft.Data.SqlClient.SqlParameter
            {
                ParameterName = parameterName,
                SqlDbType = GetSqlDbType(type),
                Value = Value,
                Direction = Direction
            };
        }
        if (context.Database.IsNpgsql())
        {
            return new Npgsql.NpgsqlParameter
            {
                ParameterName = parameterName,
                NpgsqlDbType = GetNpgsqlDbType(type),
                Value = Value,
                Direction = Direction
            };
        }
        if (context.Database.IsMySql())
        {
            return new MySqlParameter
            {
                ParameterName = parameterName,
                MySqlDbType = GetMySqlDbType(type),
                Value = Value,
                Direction = Direction
            };
        }
        if (context.Database.IsSqlite())
        {
            return new Microsoft.Data.Sqlite.SqliteParameter
            {
                ParameterName = parameterName,
                DbType = GetSqliteDbType(type),
                Value = Value,
                Direction = Direction
            };
        }
        throw new NotImplementedException("Database provider not supported");
    }

    private DbType GetSqliteDbType(Type csharpType)
    {
        return csharpType switch
        {
            Type t when t == typeof(byte) => DbType.Byte,
            Type t when t == typeof(sbyte) => DbType.SByte,
            Type t when t == typeof(short) => DbType.Int16,
            Type t when t == typeof(ushort) => DbType.UInt16,
            Type t when t == typeof(int) => DbType.Int32,
            Type t when t == typeof(uint) => DbType.UInt32,
            Type t when t == typeof(long) => DbType.Int64,
            Type t when t == typeof(ulong) => DbType.UInt64,
            Type t when t == typeof(float) => DbType.Single,
            Type t when t == typeof(double) => DbType.Double,
            Type t when t == typeof(decimal) => DbType.Decimal,
            Type t when t == typeof(bool) => DbType.Boolean,
            Type t when t == typeof(string) => DbType.String,
            Type t when t == typeof(char) => DbType.String, 
            Type t when t == typeof(Guid) => DbType.Guid, 
            Type t when t == typeof(DateTime) => DbType.DateTime, 
            Type t when t == typeof(DateTimeOffset) => DbType.String,
            Type t when t == typeof(TimeSpan) => DbType.String,
            Type t when t == typeof(byte[]) => DbType.Binary,
            Type t when t == typeof(object) => DbType.Object,
            _ => throw new ArgumentException($"Unhandled type C# : {csharpType.Name}")
        };
    }

    private MySqlDbType GetMySqlDbType(Type csharpType)
    {
        return csharpType switch
        {
            Type t when t == typeof(byte) => MySqlDbType.UByte, 
            Type t when t == typeof(sbyte) => MySqlDbType.Byte, 
            Type t when t == typeof(short) => MySqlDbType.Int16,
            Type t when t == typeof(ushort) => MySqlDbType.UInt16,
            Type t when t == typeof(int) => MySqlDbType.Int32,
            Type t when t == typeof(uint) => MySqlDbType.UInt32,
            Type t when t == typeof(long) => MySqlDbType.Int64,
            Type t when t == typeof(ulong) => MySqlDbType.UInt64,
            Type t when t == typeof(float) => MySqlDbType.Float,
            Type t when t == typeof(double) => MySqlDbType.Double,
            Type t when t == typeof(decimal) => MySqlDbType.Decimal,
            Type t when t == typeof(bool) => MySqlDbType.Bit, 
            Type t when t == typeof(string) => MySqlDbType.VarChar,
            Type t when t == typeof(char) => MySqlDbType.String, 
            Type t when t == typeof(Guid) => MySqlDbType.Guid,
            Type t when t == typeof(DateTime) => MySqlDbType.DateTime,
            Type t when t == typeof(DateTimeOffset) => MySqlDbType.Timestamp,
            Type t when t == typeof(TimeSpan) => MySqlDbType.Time,
            Type t when t == typeof(byte[]) => MySqlDbType.Blob, 
            Type t when t == typeof(object) => MySqlDbType.JSON,
            _ => throw new ArgumentException($"Unhandled type C# : {csharpType.Name}")
        };
    }

    private NpgsqlDbType GetNpgsqlDbType(Type csharpType)
    {
        return csharpType switch
        {
            Type t when t == typeof(byte) => NpgsqlDbType.Smallint,
            Type t when t == typeof(short) => NpgsqlDbType.Smallint,
            Type t when t == typeof(int) => NpgsqlDbType.Integer,
            Type t when t == typeof(long) => NpgsqlDbType.Bigint,
            Type t when t == typeof(float) => NpgsqlDbType.Real,
            Type t when t == typeof(double) => NpgsqlDbType.Double,
            Type t when t == typeof(decimal) => NpgsqlDbType.Numeric,
            Type t when t == typeof(bool) => NpgsqlDbType.Boolean,
            Type t when t == typeof(string) => NpgsqlDbType.Text,
            Type t when t == typeof(char) => NpgsqlDbType.Char,
            Type t when t == typeof(Guid) => NpgsqlDbType.Uuid,
            Type t when t == typeof(DateTime) => NpgsqlDbType.Timestamp,
            Type t when t == typeof(DateTimeOffset) => NpgsqlDbType.TimestampTz,
            Type t when t == typeof(TimeSpan) => NpgsqlDbType.Interval,
            Type t when t == typeof(byte[]) => NpgsqlDbType.Bytea,
            Type t when t == typeof(object) => NpgsqlDbType.Jsonb,
            _ => throw new ArgumentException($"Unhandled type C# : {csharpType.Name}")
        };
    }

    private SqlDbType GetSqlDbType(Type csharpType)
    {
        return csharpType switch
        {
            Type t when t == typeof(byte) => SqlDbType.TinyInt,
            Type t when t == typeof(short) => SqlDbType.SmallInt,
            Type t when t == typeof(int) => SqlDbType.Int,
            Type t when t == typeof(long) => SqlDbType.BigInt,
            Type t when t == typeof(float) => SqlDbType.Real,
            Type t when t == typeof(double) => SqlDbType.Float,
            Type t when t == typeof(decimal) => SqlDbType.Decimal,
            Type t when t == typeof(bool) => SqlDbType.Bit,
            Type t when t == typeof(string) => SqlDbType.NVarChar,
            Type t when t == typeof(char) => SqlDbType.NChar,
            Type t when t == typeof(Guid) => SqlDbType.UniqueIdentifier,
            Type t when t == typeof(DateTime) => SqlDbType.DateTime,
            Type t when t == typeof(DateTimeOffset) => SqlDbType.DateTimeOffset,
            Type t when t == typeof(TimeSpan) => SqlDbType.Time,
            Type t when t == typeof(byte[]) => SqlDbType.VarBinary,
            Type t when t == typeof(object) => SqlDbType.Variant,
            _ => throw new ArgumentException($"Unhandled type C# : {csharpType.Name}")
        };
    }
}
