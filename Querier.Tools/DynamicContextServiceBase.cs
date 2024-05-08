using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Querier.Api.Models.Attributes;
using Querier.Api.Models.Datatable;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Querier.Tools
{
    public abstract class DynamicContextServiceBase
    {
        protected string _dynamicContextProcedureName = "";
        protected string _dynamicContextName = "";
        protected Dictionary<string, int> _nbDigitsAfterComa = new Dictionary<string, int>();
        protected DistributedCacheEntryOptions DefaultDistributedCacheExpiryOptions
        {
            get
            {
                return new DistributedCacheEntryOptions().SetAbsoluteExpiration(DateTime.Now.AddDays(3)).SetSlidingExpiration(TimeSpan.FromDays(1));
            }
        }

        protected MemoryCacheEntryOptions DefaultMemoryCacheExpiryOptions
        {
            get
            {
                return new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddMinutes(60),
                    Priority = CacheItemPriority.Normal,
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                };
            }
        } 

        protected T GetParameterValue<T>(object value)
        {
            var t = typeof(T);

            // if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
            // {
            //     return (T)Convert.ChangeType(Convert.ToDecimal(value), typeof(T));
            // }

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) 
            {
                if (value == null) 
                { 
                    return default(T); 
                }

                t = Nullable.GetUnderlyingType(t);
            }

            return (T)Convert.ChangeType(value, t);
        }
    }

    public abstract class DynamicContextServiceBaseWithResult : DynamicContextServiceBase
    {
        public abstract List<string> GetSummableColumns();
        public Dictionary<string, object> ComputeReportSums<T>(List<T>? source)
        {
            Dictionary<string, object> sums = new Dictionary<string, object>();
            Dictionary<string, object> sumResultsByCol = new Dictionary<string, object>();
            var typesWithMyAttribute =
                from a in AppDomain.CurrentDomain.GetAssemblies()
                from t in a.GetTypes()
                let attributes = t.GetCustomAttributes(typeof(DynamicContextProcedureTotalCalculator), true)
                where t.IsDefined(typeof(DynamicContextProcedureTotalCalculator), false) &&
                    t.GetCustomAttribute<DynamicContextProcedureTotalCalculator>().DynamicContext == _dynamicContextName &&
                    t.GetCustomAttribute<DynamicContextProcedureTotalCalculator>().Procedure == _dynamicContextProcedureName
                select t;
            List<MethodInfo> customTotalMethods = typesWithMyAttribute.Count() > 0 ? typesWithMyAttribute.First().GetMethods().Where(m => m.GetCustomAttributes().Any(a => a.GetType() == typeof(ColumnCustomTotalAttribute))).ToList() : new List<MethodInfo>();

            foreach (string col in GetSummableColumns())
            {
                if (!customTotalMethods.Any(cm => cm.GetCustomAttribute<ColumnCustomTotalAttribute>().ForColumn == col))
                {
                    PropertyInfo pi = typeof(T).GetProperties().First(p => ((ColumnAttribute)p.GetCustomAttribute(typeof(ColumnAttribute))).Name.ToLower() == col.ToLower());
                    bool isNullable = Nullable.GetUnderlyingType(pi.PropertyType) != null;
                    Type targetType = isNullable ? Nullable.GetUnderlyingType(pi.PropertyType) : pi.PropertyType;
                    switch (targetType.Name)
                    {
                        case "Int16":
                            sums.Add(col, source.Select(p => pi.GetValue(p) != null ? Convert.ToDecimal(pi.GetValue(p)) : 0).Sum());
                            break;
                        case "Decimal":
                            sums.Add(col, source.Select(p => (decimal)(pi.GetValue(p) ?? (decimal)0)).Sum());
                            break;
                        case "Int32":
                            sums.Add(col, source.Select(p => (int)(pi.GetValue(p) ?? 0)).Sum());
                            break;
                        default:
                            throw new Exception($"Type {targetType.Name} inconnu pour calculer la somme.");
                    }
                }
                else
                {
                    if (source.Count == 0)
                    {
                        sums.Add(col, 0);
                    }
                    else
                    {
                        MethodInfo methodToCall = customTotalMethods.First(cm => cm.GetCustomAttribute<ColumnCustomTotalAttribute>().ForColumn == col);
                        object customSumObject = Activator.CreateInstance(typesWithMyAttribute.First());
                        object d = methodToCall.Invoke(customSumObject, new[] { source });
                        sums.Add(col, d);
                    }
                }
            }

            return sums;
        }
    }
}