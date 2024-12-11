using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Querier.Api.Models.Common;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Querier.Api.Tools;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.IO;

namespace Querier.Api.Services
{
    public class EntityCRUDService : IEntityCRUDService
    {
        private readonly IDynamicContextList _dynamicContextList;
        private readonly ILogger<EntityCRUDService> _logger;

        public EntityCRUDService(IDynamicContextList dynamicContextList, ILogger<EntityCRUDService> logger)
        {
            _logger = logger;
            _dynamicContextList = dynamicContextList; 
        }

        public List<string> GetContexts()
        {
            var contexts = new List<string>();
            var assembliesPath = Path.Combine("Assemblies");
            
            if (Directory.Exists(assembliesPath))
            {
                foreach (var file in Directory.GetFiles(assembliesPath, "*.dll"))
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(file);
                        var types = assembly.GetTypes()
                            .Where(t => t.IsAssignableTo(typeof(DbContext)));
                        
                        contexts.AddRange(types.Select(t => t.FullName));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error loading assembly {file}");
                    }
                }
            }

            return contexts;
        }

        public List<EntityDefinition> GetEntities(string contextTypeFullname)
        {
            List<EntityDefinition> result = new List<EntityDefinition>();
            DbContext targetContext = GetDbContextFromTypeName(contextTypeFullname);
            if (targetContext != null)
            {
                List<PropertyInfo> contextDbSetProperties = targetContext.GetType().GetProperties()
                    .Where(p => p.PropertyType.Name.Contains("DbSet")).ToList();
                foreach (PropertyInfo pi in contextDbSetProperties)
                {
                    result.Add(pi.PropertyType.GetGenericArguments().First().ToEntityDefinition());
                }
            }

            return result;
        }

        public EntityDefinition GetEntity(string contextTypeFullname, string entityFullname)
        {
            return GetEntities(contextTypeFullname).FirstOrDefault(e => e.Name == entityFullname);
        }

        public object Create(string contextTypeFullname, string entityTypeFullname, dynamic entity)
        {
            Type entityType = Utils.GetType(entityTypeFullname);
            if (entityType == null)
                throw new Exception($"Entity \"{entityTypeFullname}\" is not handled in the {contextTypeFullname} context.");

            DbContext targetContext = GetDbContextFromTypeName(contextTypeFullname);

            object newEntity = Activator.CreateInstance(entityType);
            dynamic modelEntity = JsonSerializer.Deserialize(entity.ToString(), entityType);

            foreach (PropertyInfo pi in newEntity.GetType().GetProperties().Where(p => p.GetCustomAttribute(typeof(NotMappedAttribute)) == null &&
                         p.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null))
            {
                string propertyName = pi.Name;
                object value = modelEntity.GetType().GetProperty(propertyName).GetValue(modelEntity, null);
                pi.SetValue(newEntity, value);
            }

            targetContext.Add(newEntity);
            targetContext.SaveChanges();

            return newEntity;
        }

        public IEnumerable<object> GetAll(string contextTypeFullname, string entityTypeFullname)
        {
            Type reqType = Utils.GetType(entityTypeFullname);
            if (reqType == null)
                throw new Exception($"Entity \"{entityTypeFullname}\" is not handled in the \"{contextTypeFullname}\" context.");

            DbContext targetContext = GetDbContextFromTypeName(contextTypeFullname);

            PropertyInfo contextProperty = targetContext.GetType().GetProperties().Where(p => p.PropertyType.Name.Contains("DbSet")).FirstOrDefault(p => p.PropertyType.GetGenericArguments().Any(a => a == reqType));
            if (contextProperty == null)
                throw new Exception($"Entity \"{entityTypeFullname}\" is not handled by any DbSet in the \"{contextTypeFullname}\" context.");

            var dbsetResult = contextProperty.GetValue(targetContext) as IEnumerable<object>;
            return JsonConvert.DeserializeObject<List<ExpandoObject>>(JsonConvert.SerializeObject(dbsetResult));
        }

        public IEnumerable<object> Read(string contextTypeFullname, string entityTypeFullname, List<DataFilter> filters)
        {
            return Read(contextTypeFullname, entityTypeFullname, filters, out Type type);
        }

        public IEnumerable<object> Read(string contextTypeFullname, string entityTypeFullname, List<DataFilter> filters, out Type entityType)
        {
            Type reqType = Utils.GetType(entityTypeFullname);
            if (reqType == null)
                throw new Exception($"Entity \"{entityTypeFullname}\" is not handled in the \"{contextTypeFullname}\" context.");

            DbContext targetContext = GetDbContextFromTypeName(contextTypeFullname);

            PropertyInfo contextProperty = targetContext.GetType().GetProperties().Where(p => p.PropertyType.Name.Contains("DbSet")).FirstOrDefault(p => p.PropertyType.GetGenericArguments().Any(a => a == reqType));
            if (contextProperty == null)
                throw new Exception($"Entity \"{entityTypeFullname}\" is not handled by any DbSet in the \"{contextTypeFullname}\" context.");

            entityType = contextProperty.PropertyType.GetGenericArguments()[0];
            var dbsetResult = contextProperty.GetValue(targetContext) as IEnumerable<object>;
            var dt = (DataTable) JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dbsetResult), typeof(DataTable));
            dt = dt.Filter(filters);
            return JsonConvert.DeserializeObject<List<ExpandoObject>>(JsonConvert.SerializeObject(dt));
        }

        public DataTable GetDatatableFromSql(string contextTypeFullname, string SqlQuery, List<DataFilter> Filters)
        {
            DbContext apiDbContext = GetDbContextFromTypeName(contextTypeFullname);
            DataTable dt = apiDbContext.Database.RawSqlQuery(SqlQuery); 
            return dt.Filter(Filters);
        }

        public IEnumerable<object> ReadFromSql(string contextTypeFullname, string SqlQuery, List<DataFilter> Filters)
        {
            DataTable dt = GetDatatableFromSql(contextTypeFullname, SqlQuery, Filters);
            var res = JsonConvert.DeserializeObject<List<ExpandoObject>>(JsonConvert.SerializeObject(dt));
            return res;
        }

        public object Update(string contextTypeFullname, string entityFullname, object entity)
        {
            Type entityType = Utils.GetType(entityFullname);
            if (entityType == null)
                throw new Exception($"Entity \"{entityFullname}\" is not handled in the {contextTypeFullname} context.");

            PropertyInfo keyProperty = entityType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute(typeof(KeyAttribute)) != null);
            if (keyProperty == null)
                throw new Exception($"Entity \"{entityFullname}\" has no defined Key attribute defined in the datamart context.");

            DbContext targetContext = GetDbContextFromTypeName(contextTypeFullname);

            object modelEntity = JsonSerializer.Deserialize(entity.ToString(), entityType);
            object keyValue = modelEntity.GetType().GetProperty(keyProperty.Name).GetValue(modelEntity, null);

            object existingEntity = targetContext.Find(entityType, keyValue);

            foreach (PropertyInfo pi in modelEntity.GetType().GetProperties().Where(p => p.GetCustomAttribute(typeof(NotMappedAttribute)) == null &&
                         p.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null))
            {
                if (pi.Name == keyProperty.Name)
                    continue;

                string propertyName = pi.Name;
                object value = modelEntity.GetType().GetProperty(propertyName).GetValue(modelEntity, null);
                pi.SetValue(existingEntity, value);
            }

            targetContext.SaveChanges();

            return existingEntity;
        }

        public object CreateOrUpdate(string contextTypeFullname, string entityFullname, object entity)
        {
            Type entityType = Utils.GetType(entityFullname);
            if (entityType == null)
                throw new Exception($"Entity \"{entityFullname}\" is not handled in the {contextTypeFullname} context.");

            PropertyInfo keyProperty = entityType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute(typeof(KeyAttribute)) != null);
            if (keyProperty == null)
                throw new Exception($"Entity \"{entityFullname}\" has no defined Key attribute defined in the datamart context.");

            DbContext targetContext = GetDbContextFromTypeName(contextTypeFullname);

            object modelEntity = JsonSerializer.Deserialize(entity.ToString(), entityType);
            object keyValue = modelEntity.GetType().GetProperty(keyProperty.Name).GetValue(modelEntity, null);

            object existingEntity = targetContext.Find(entityType, keyValue);

            if (existingEntity is null)
                return this.Create(contextTypeFullname, entityFullname, entity);
            return this.Update(contextTypeFullname, entityFullname, entity);
        }

        public void Delete(string contextTypeFullname, string entityFullname, object entityIdentifier)
        {
            Type entityType = Utils.GetType(entityFullname);
            if (entityType == null)
                throw new Exception($"Entity \"{entityFullname}\" is not handled in the {contextTypeFullname} context.");

            PropertyInfo keyProperty = entityType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute(typeof(KeyAttribute)) != null);
            if (keyProperty == null)
                throw new Exception($"Entity \"{entityFullname}\" has no defined Key attribute defined in the datamart context.");

            DbContext targetContext = GetDbContextFromTypeName(contextTypeFullname);

            object entityKey = Convert.ChangeType(entityIdentifier, keyProperty.PropertyType);
            object existingEntity = targetContext.Find(entityType, entityKey);

            targetContext.Remove(existingEntity);
            targetContext.SaveChanges();
        }

        public SQLQueryResult GetSQLQueryEntityDefinition(CRUDExecuteSQLQueryRequest request)
        {
            SQLQueryResult result = new SQLQueryResult();
            result.QuerySuccessful = true;
            // TODO : --> On ne doit pas ramener la donnée dans le front
            result.Datas = new List<dynamic>();
            DbContext apiDbContext = GetDbContextFromTypeName(request.ContextTypeName);

            try
            {
                DataTable dt = apiDbContext.Database.RawSqlQuery(request.SqlQuery);
                result.Entity = new EntityDefinition();
                result.Entity.Name = "UNK";
                result.Entity.Properties = new List<PropertyDefinition>();
                foreach (DataColumn dtColumn in dt.Columns)
                {
                    Type colType = dtColumn.DataType;
                    PropertyDefinition pd = new PropertyDefinition();
                    pd.Name = dtColumn.ColumnName;
                    pd.Type = dtColumn.AllowDBNull ? colType.Name + "?" : colType.Name;
                    pd.Options = new List<PropertyOption>();

                    if (dtColumn.AllowDBNull)
                        pd.Options.Add(PropertyOption.IsNullable);

                    result.Entity.Properties.Add(pd);
                }

                result.Datas = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(dt));
            }
            catch (Exception e)
            {
                result.QuerySuccessful = false;
                result.ErrorMessage = e.Message;
            }
            
            //DataTable dt = apiDbContext.Database.RawSqlQuery(@"
            //SELECT *
            //  FROM MHEmployee AS mhe
            // INNER JOIN MHContacts AS mhc on mhc.EmployeeId = mhe.Id
            //");

            return result;
        }

        private DbContext GetDbContextFromTypeName(string contextTypeName)
        {
            List<Type> contextTypes = AppDomain.CurrentDomain.GetAssemblies()
                       .SelectMany(assembly => assembly.GetTypes())
                       .Where(t => t.IsAssignableTo(typeof(DbContext)) && t.FullName == contextTypeName).ToList();


            DbContext target = ServiceActivator.GetScope().ServiceProvider.GetService(contextTypes.First()) as DbContext ??
                               Activator.CreateInstance(contextTypes.First()) as DbContext;
            return target;
        }
    }
}
