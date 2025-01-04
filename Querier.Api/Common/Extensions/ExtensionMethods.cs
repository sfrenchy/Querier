using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Querier.Api.Application.DTOs.Requests.Entity;
using Querier.Api.Domain.Common.Attributes;
using Querier.Api.Domain.Common.ValueObjects;

namespace Querier.Api.Tools
{
    public static class ExtensionMethods
    {
        private static readonly byte[] BMP = { 66, 77 };
        private static readonly byte[] DOC = { 208, 207, 17, 224, 161, 177, 26, 225 };
        private static readonly byte[] EXE_DLL = { 77, 90 };
        private static readonly byte[] GIF = { 71, 73, 70, 56 };
        private static readonly byte[] ICO = { 0, 0, 1, 0 };
        private static readonly byte[] JPG = { 255, 216, 255 };
        private static readonly byte[] MP3 = { 255, 251, 48 };
        private static readonly byte[] OGG = { 79, 103, 103, 83, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0 };
        private static readonly byte[] PDF = { 37, 80, 68, 70, 45, 49, 46 };
        private static readonly byte[] PNG = { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82 };
        private static readonly byte[] RAR = { 82, 97, 114, 33, 26, 7, 0 };
        private static readonly byte[] SWF = { 70, 87, 83 };
        private static readonly byte[] TIFF = { 73, 73, 42, 0 };
        private static readonly byte[] TORRENT = { 100, 56, 58, 97, 110, 110, 111, 117, 110, 99, 101 };
        private static readonly byte[] TTF = { 0, 1, 0, 0, 0 };
        private static readonly byte[] WAV_AVI = { 82, 73, 70, 70 };
        private static readonly byte[] WMV_WMA = { 48, 38, 178, 117, 142, 102, 207, 17, 166, 217, 0, 170, 0, 98, 206, 108 };
        private static readonly byte[] ZIP_DOCX = { 80, 75, 3, 4 };

        static readonly MethodInfo SetMethod =
            typeof(DbContext).GetMethod(nameof(DbContext.Set), 1, Array.Empty<Type>()) ??
            throw new Exception("Type not found: DbContext.Set");

        public static IQueryable Query(this DbContext context, string entityName) =>
            context.Query(context.Model.FindEntityType(entityName).ClrType);

        public static IQueryable Query(this DbContext context, Type entityType) =>
            (IQueryable)SetMethod.MakeGenericMethod(entityType)?.Invoke(context, null) ??
            throw new Exception($"Type not found: {entityType.FullName}");

        public static DataTable ToDataTable<T>(this IList<T> data)
        {
            
            DataTable table = new DataTable();

            if (data.Count > 0)
            {
                PropertyDescriptorCollection properties =
                    TypeDescriptor.GetProperties(data[0].GetType());
                foreach (PropertyDescriptor prop in properties)
                    table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                foreach (T item in data)
                {
                    DataRow row = table.NewRow();
                    foreach (PropertyDescriptor prop in properties)
                        row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                    table.Rows.Add(row);
                }
            }
            
            
            return table;
        }

        public static object ExecuteScalar(this DbContext context, string sql,
        List<DbParameter> parameters = null,
        CommandType commandType = CommandType.Text,
        int? commandTimeOutInSeconds = null)
        {
            Object value = ExecuteScalar(context.Database, sql, parameters,
                                         commandType, commandTimeOutInSeconds);
            return value;
        }

        public static DataTable RawSqlQuery(this DatabaseFacade database, string sql, List<DbParameter> parameters = null, CommandType commandType = CommandType.Text, int? commandTimeOutInSeconds = null)
        {
            var dt = new DataTable();
            using (var command = database.GetDbConnection().CreateCommand())
            {
                command.Connection.Open();
                command.CommandText = sql;
                command.CommandType = CommandType.Text;

                using (var reader = command.ExecuteReader())
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters.ToArray());
                    
                    dt.Load(reader);
                }
            }
            return dt;
        }

        public static object ExecuteScalar(this DatabaseFacade database,
        string sql, List<DbParameter> parameters = null,
        CommandType commandType = CommandType.Text,
        int? commandTimeOutInSeconds = null)
        {
            Object value;
            using (var cmd = database.GetDbConnection().CreateCommand())
            {
                if (cmd.Connection.State != ConnectionState.Open)
                {
                    cmd.Connection.Open();
                }
                cmd.CommandText = sql;
                cmd.CommandType = commandType;
                if (commandTimeOutInSeconds != null)
                {
                    cmd.CommandTimeout = (int)commandTimeOutInSeconds;
                }
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                }
                value = cmd.ExecuteScalar();
            }
            return value;
        }

        public static DataTable ToDataTable(this IEnumerable<object> objects)
        {
            DataTable table = new DataTable();

            if (objects.Any())
            {
                // Add columns based on the type of the first object
                var properties = objects.First().GetType().GetProperties();
                foreach (var property in properties)
                {
                    table.Columns.Add(property.Name, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
                }

                // Add rows
                foreach (var obj in objects)
                {
                    DataRow row = table.NewRow();
                    foreach (var property in properties)
                    {
                        row[property.Name] = property.GetValue(obj) ?? DBNull.Value;
                    }
                    table.Rows.Add(row);
                }
            }

            return table;
        }

        public static IServiceCollection AddLazyResolution(this IServiceCollection services)
        {
            return services.AddTransient(
                typeof(Lazy<>),
                typeof(LazilyResolved<>));
        }

        public static int MonthDifference(this DateTime lValue, DateTime rValue)
        {
            return Math.Abs((lValue.Month - rValue.Month) + 12 * (lValue.Year - rValue.Year));
        }

        public static DateTime FromTimeZone(this DateTimeOffset? lValue, string timeZone)
        {
            TimeZoneInfo tZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return TimeZoneInfo.ConvertTimeFromUtc(lValue.Value.DateTime, tZone);
        }

        public static bool Between(this DateTime input, DateTime date1, DateTime date2)
        {
            return (input > date1 && input < date2);
        }

        public static T CastTo<T>(this object o) => (T)o;
        public static T CastTo<T>(this object o, T type) => (T)o;

        public static List<dynamic> CastListToDynamic<T>(this List<T> source)
        {
            List<dynamic> result = new List<dynamic>();
            foreach (var item in source)
            {
                result.Add(item);
            }
            return result;
        }

        public static List<dynamic> CastToDynamic(this IEnumerable source)
        {
            List<dynamic> result = new List<dynamic>();
            foreach (var item in source)
            {
                result.Add((dynamic)item);
            }
            return result;
        }

        public static bool IsNullableProperty(this PropertyInfo propertyInfo) => propertyInfo.PropertyType.Name.IndexOf("Nullable`", StringComparison.Ordinal) > -1;

        public static async Task<object> InvokeAsync(this MethodInfo @this, object obj, params object[] parameters)
        {
            var task = (Task)@this.Invoke(obj, parameters);
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty.GetValue(task);
        }

        public static EntityDefinition ToEntityDefinition(this Type type)
        {
            EntityDefinition result = new EntityDefinition();
            result.Name = type.FullName;
            result.Properties = new List<PropertyDefinition>();
            foreach (PropertyInfo pi in type.GetProperties().Where(p => p.GetCustomAttribute(typeof(System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute)) == null &&
                                                                                   p.GetCustomAttribute(typeof(Newtonsoft.Json.JsonIgnoreAttribute)) == null))
            {
                PropertyDefinition pd = new PropertyDefinition();
                pd.Name = pi.Name;
                pd.Type = pi.IsNullableProperty() ? Nullable.GetUnderlyingType(pi.PropertyType).Name + "?" : pi.PropertyType.Name;
                pd.Options = new List<PropertyOption>();
                
                if (pi.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.KeyAttribute)).Any())
                    pd.Options.Add(PropertyOption.IsKey);
                
                if (pi.IsNullableProperty())
                    pd.Options.Add(PropertyOption.IsNullable);

                if (pi.GetCustomAttributes(typeof(JsonStringAttribute)).Any())
                    pd.Type = "JsonString";

                result.Properties.Add(pd);
            }

            return result;
        }

        public static bool IsNullOrEmpty(this JToken token)
        {
            return token == null ||
                   token.Type == JTokenType.Array && !token.HasValues ||
                   token.Type == JTokenType.Object && !token.HasValues ||
                   token.Type == JTokenType.String && token.ToString() == string.Empty ||
                   token.Type == JTokenType.Null;
        }

        public static DataTable Filter(this DataTable source, List<DataFilter> Filters)
        {
            string dateTimeFormat = "yyyy-MM-dd"; // TODO: y'a mieux à faire... Par mon lapin
            
            var predicate = PredicateBuilder.True<DataRow>();
            foreach (DataFilter filter in Filters)
            {
                string columnName = filter.Column.Name;
                if (source.Columns.Contains(columnName))
                {
                    int columnIndex = source.Columns.IndexOf(columnName);
                    DataColumn column = source.Columns[columnIndex];
                    Type columnType = column.DataType;
                    object value = columnType.GetValueFromString(filter.Operand);
                    switch (columnType)
                    {
                        case Type when columnType == typeof(Int64):
                            Int64 int64Value = (Int64) value;
                            switch (filter.Operator)
                            {
                                case "EqualTo":
                                    predicate = predicate.And(p => (Int64)p[columnName] == int64Value);
                                    break;
                                case "NotEqualTo":
                                    predicate = predicate.And(p => (Int64)p[columnName] != int64Value);
                                    break;
                                case "GreaterThan":
                                    predicate = predicate.And(p => (Int64)p[columnName] > int64Value);
                                    break;
                                case "LessThan":
                                    predicate = predicate.And(p => (Int64)p[columnName] < int64Value);
                                    break;
                                case "EqualOrGreaterThan":
                                    predicate = predicate.And(p => (Int64)p[columnName] >= int64Value);
                                    break;
                                case "EqualOrLessThan":
                                    predicate = predicate.And(p => (Int64)p[columnName] <= int64Value);
                                    break;
                            }
                            break;
                         case Type when columnType == typeof(int):
                            int intValue = (int) value;
                            switch (filter.Operator)
                            {
                                case "EqualTo":
                                    predicate = predicate.And(p => (int)p[columnName] == intValue);
                                    break;
                                case "NotEqualTo":
                                    predicate = predicate.And(p => (int)p[columnName] != intValue);
                                    break;
                                case "GreaterThan":
                                    predicate = predicate.And(p => (int)p[columnName] > intValue);
                                    break;
                                case "LessThan":
                                    predicate = predicate.And(p => (int)p[columnName] < intValue);
                                    break;
                                case "EqualOrGreaterThan":
                                    predicate = predicate.And(p => (int)p[columnName] >= intValue);
                                    break;
                                case "EqualOrLessThan":
                                    predicate = predicate.And(p => (int)p[columnName] <= intValue);
                                    break;
                            }
                            break;
                        case Type when columnType == typeof(decimal):
                            decimal decimalValue = (decimal)value;
                            switch (filter.Operator)
                            {
                                case "EqualTo":
                                    predicate = predicate.And(p => (decimal)p[columnName] == decimalValue);
                                    break;
                                case "NotEqualTo":
                                    predicate = predicate.And(p => (decimal)p[columnName] != decimalValue);
                                    break;
                                case "GreaterThan":
                                    predicate = predicate.And(p => (decimal)p[columnName] > decimalValue);
                                    break;
                                case "LessThan":
                                    predicate = predicate.And(p => (decimal)p[columnName] < decimalValue);
                                    break;
                                case "EqualOrGreaterThan":
                                    predicate = predicate.And(p => (decimal)p[columnName] >= decimalValue);
                                    break;
                                case "EqualOrLessThan":
                                    predicate = predicate.And(p => (decimal)p[columnName] <= decimalValue);
                                    break;
                            }
                            break;
                        case Type when columnType == typeof(DateTime):
                            DateTime dateTimeValue = (DateTime) value; 
                            switch (filter.Operator)
                            {
                                case "EqualTo":
                                    predicate = predicate.And(p => (DateTime)p[columnName] == dateTimeValue);
                                    break;
                                case "NotEqualTo":
                                    predicate = predicate.And(p => (DateTime)p[columnName] != dateTimeValue);
                                    break;
                                case "GreaterThan":
                                    predicate = predicate.And(p => (DateTime)p[columnName] > dateTimeValue);
                                    break;
                                case "LessThan":
                                    predicate = predicate.And(p => (DateTime)p[columnName] < dateTimeValue);
                                    break;
                                case "EqualOrGreaterThan":
                                    predicate = predicate.And(p => (DateTime)p[columnName] >= dateTimeValue);
                                    break;
                                case "EqualOrLessThan":
                                    predicate = predicate.And(p => (DateTime)p[columnName] <= dateTimeValue);
                                    break;
                            }
                            break;
                        case Type when columnType == typeof(string):
                            string stringValue = (string)value;
                            switch (filter.Operator)
                            {
                                case "EqualTo":
                                    predicate = predicate.And(p => (string)p[columnName] == stringValue);
                                    break;
                                case "Contains":
                                    predicate = predicate.And(p => ((string)p[columnName]).Contains(stringValue));
                                    break;
                                case "BeginWith":
                                    predicate = predicate.And(p => ((string)p[columnName]).StartsWith(stringValue));
                                    break;
                                case "EndWith":
                                    predicate = predicate.And(p => ((string)p[columnName]).EndsWith(stringValue));
                                    break;
                                default:
                                    throw new NotImplementedException(
                                        $"Operator {filter.Operator} is not implemented for type {columnType}");
                            }
                            break;
                        default:
                            throw new NotImplementedException($"Type {columnType} filtering is not yet implemented");
                    }

                    
                }
            }

            return source.AsEnumerable().AsQueryable().Where(predicate).CopyToDataTable();
        }

        public static object GetValueFromString<T>(this T type, string val) where T: Type
        {
            switch(type)
            {
                case Type when type == typeof(Decimal):
                    return Convert.ToDecimal(val);
                case Type when type == typeof(Int64):
                    return Convert.ToInt64(val);
                case Type when type == typeof(int):
                    return Convert.ToInt32(val);
                case Type when type == typeof(string):
                    return Convert.ToString(val);
                case Type when type == typeof(DateTime):
                    return DateTime.ParseExact(val, "yyyy-MM-dd", CultureInfo.CurrentCulture);
                default:
                    throw new NotImplementedException($"Type {type} not handled yet");
            }
        }

        public static string GetSpecificClaim(this ClaimsIdentity claimsIdentity, string claimType)
        {
            var claim = claimsIdentity.Claims.FirstOrDefault(x => x.Type == claimType);

            return claim != null ? claim.Value : string.Empty;
        }

        public static string GetHash<T>(this object instance) where T : HashAlgorithm, new()
        {
            T cryptoServiceProvider = new T();
            return computeHash(instance, cryptoServiceProvider);
        }

        public static string GetKeyedHash<T>(this object instance, byte[] key) where T : KeyedHashAlgorithm, new()
        {
            T cryptoServiceProvider = new T { Key = key };
            return computeHash(instance, cryptoServiceProvider);
        }

        public static string GetMD5Hash(this object instance)
        {
            return instance.GetHash<MD5CryptoServiceProvider>();
        }

        public static string GetSHA1Hash(this object instance)
        {
            return instance.GetHash<SHA1CryptoServiceProvider>();
        }

        private static string computeHash<T>(object instance, T cryptoServiceProvider) where T : HashAlgorithm, new()
        {
            DataContractSerializer serializer = new DataContractSerializer(instance.GetType());
            using (MemoryStream memoryStream = new MemoryStream())
            {
                serializer.WriteObject(memoryStream, instance);
                cryptoServiceProvider.ComputeHash(memoryStream.ToArray());
                return Convert.ToBase64String(cryptoServiceProvider.Hash);
            }
        }

        //Retire les accents d'une string
        public static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder
                .ToString()
                .Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        ///This method reads the first bytes of a file,
        ///and then uses the MimeTypeHelper class to determine the MIME type of the file based on the byte array.
        ///This method is more secure than just checking the file extension, 
        ///as it can detect if a file has been renamed to bypass file type restrictions.
        /// </summary>
        public static string GetMimeType(byte[] file, string fileName)
        {

            string mime = "application/octet-stream"; //DEFAULT UNKNOWN MIME TYPE

            //Ensure that the filename isn't empty or null
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return mime;
            }

            //Get the file extension
            string extension = Path.GetExtension(fileName) == null
                                   ? string.Empty
                                   : Path.GetExtension(fileName).ToUpper();

            //Get the MIME Type
            if (file.Take(2).SequenceEqual(BMP))
            {
                mime = "image/bmp";
            }
            else if (file.Take(8).SequenceEqual(DOC))
            {
                mime = "application/msword";
            }
            else if (file.Take(2).SequenceEqual(EXE_DLL))
            {
                mime = "application/x-msdownload"; //both use same mime type
            }
            else if (file.Take(4).SequenceEqual(GIF))
            {
                mime = "image/gif";
            }
            else if (file.Take(4).SequenceEqual(ICO))
            {
                mime = "image/x-icon";
            }
            else if (file.Take(3).SequenceEqual(JPG))
            {
                mime = "image/jpeg";
            }
            else if (file.Take(3).SequenceEqual(MP3))
            {
                mime = "audio/mpeg";
            }
            else if (file.Take(14).SequenceEqual(OGG))
            {
                if (extension == ".OGX")
                {
                    mime = "application/ogg";
                }
                else if (extension == ".OGA")
                {
                    mime = "audio/ogg";
                }
                else
                {
                    mime = "video/ogg";
                }
            }
            else if (file.Take(7).SequenceEqual(PDF))
            {
                mime = "application/pdf";
            }
            else if (file.Take(16).SequenceEqual(PNG))
            {
                mime = "image/png";
            }
            else if (file.Take(7).SequenceEqual(RAR))
            {
                mime = "application/x-rar-compressed";
            }
            else if (file.Take(3).SequenceEqual(SWF))
            {
                mime = "application/x-shockwave-flash";
            }
            else if (file.Take(4).SequenceEqual(TIFF))
            {
                mime = "image/tiff";
            }
            else if (file.Take(11).SequenceEqual(TORRENT))
            {
                mime = "application/x-bittorrent";
            }
            else if (file.Take(5).SequenceEqual(TTF))
            {
                mime = "application/x-font-ttf";
            }
            else if (file.Take(4).SequenceEqual(WAV_AVI))
            {
                mime = extension == ".AVI" ? "video/x-msvideo" : "audio/x-wav";
            }
            else if (file.Take(16).SequenceEqual(WMV_WMA))
            {
                mime = extension == ".WMA" ? "audio/x-ms-wma" : "video/x-ms-wmv";
            }
            else if (file.Take(4).SequenceEqual(ZIP_DOCX))
            {
                mime = extension == ".DOCX" ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document" : "application/x-zip-compressed";
            }

            return mime;
        }

        /// <summary>
        /// This code snippet first extracts the unit from the input string by taking the last two characters of the string,
        /// and then extracts the value by taking all the characters except for the last two. 
        /// Then it checks if the unit is "GB" or not. If it is, it will convert the value from GB to bytes.
        /// </summary>
        public static long ConvertGBToLong(string input)
        {
            string unit = input.Substring(input.Length - 2);
            int value = int.Parse(input.Substring(0, input.Length - 2));
            long result = 0;
            if (unit == "GB")
            {
                result = value * 1073741824;
            }
            return result;
        }

        public class LazilyResolved<T> : Lazy<T>
        {
            public LazilyResolved(IServiceProvider serviceProvider)
                : base(serviceProvider.GetRequiredService<T>)
            {
            }
        }
    }
}
