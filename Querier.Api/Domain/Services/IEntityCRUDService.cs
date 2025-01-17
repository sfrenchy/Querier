﻿using System;
using System.Collections.Generic;
using System.Data;
using Querier.Api.Application.DTOs.Requests.Entity;
using Querier.Api.Domain.Common.Models;
using Querier.Api.Domain.Common.ValueObjects;

namespace Querier.Api.Domain.Services
{
    public interface IEntityCRUDService
    {
        /// <summary>
        /// Get the list of available EntityFrameworkCore contexts
        /// </summary>
        /// <returns>The list of full names of available contexts (ie: Querier.Api.Models.ApiDbContext)</returns>
        public List<string> GetContexts();

        /// <summary>
        /// Get the list of available entities definitions for a context
        /// </summary>
        /// <param name="contextTypeFullname">A full name of an available context (ie: Querier.Api.Models.ApiDbContext)</param>
        /// <returns>The list of entites available in the context with regarding informations</returns>
        public List<EntityDefinition> GetEntities(string contextTypeFullname);

        /// <summary>
        /// Get the entity definition for the context and the entity
        /// </summary>
        /// <param name="contextTypeFullname">A full name of an available context (ie: Querier.Api.Models.ApiDbContext)</param>
        /// <param name="entityFullname">A full name of an available context entity (ie: Querier.Api.Models.UI.QPageCategory)</param>
        /// <returns></returns>
        public EntityDefinition GetEntity(string contextTypeFullname, string entityFullname);

        /// <summary>
        /// Create a new entity in a specific context
        /// </summary>
        /// <param name="contextTypeFullname">The fullname of the context</param>
        /// <param name="entityTypeFullname">The fullname of the entity in the context</param>
        /// <param name="newEntity">The type wished to be created</param>
        /// <returns>The new entity</returns>
        public object Create(string contextTypeFullname, string entityTypeFullname, object newEntity);

        /// <summary>
        /// Read entities from repository
        /// </summary>
        /// <param name="contextTypeFullname">The fullname of the context</param>
        /// <param name="entityTypeFullname">The fullname of the entity in the context</param>
        /// <param name="filters">The data filters to be applied to the resultset</param>
        /// <param name="entityType">The Type object of the entity</param>
        /// <returns>An enumerable that hold the datas for entity of the context</returns>
        public IEnumerable<object> Read(string contextTypeFullname, string entityTypeFullname, List<DataFilter> filters, out Type entityType);

        /// <summary>
        /// Read a resultset from an SQL query
        /// </summary>
        /// <param name="contextTypeFullname">The fullname of the context</param>
        /// <param name="entityTypeFullname">the SQL query that return a resultset</param>
        /// <param name="Filters">The data filters to be applied to the resultset</param>
        /// <returns>An enumerable that hold the datas for dbset of the query on the context</returns>
        public IEnumerable<object> ReadFromSql(string contextTypeFullname, string SqlQuery, List<DataFilter> Filters);

        public DataTable GetDatatableFromSql(string contextTypeFullname, string SqlQuery, List<DataFilter> Filters);

        /// <summary>
        /// Read entities from repository
        /// </summary>
        /// <param name="contextTypeFullname">The fullname of the context</param>
        /// <param name="entityTypeFullname">The fullname of the entity in the context</param>
        /// <param name="filters">The data filters to be applied to the resultset</param>
        /// <returns>An enumerable that hold the datas for entity of the context</returns>
        public IEnumerable<object> Read(string contextTypeFullname, string entityTypeFullname, List<DataFilter> filters);

        public PagedResult<object> GetAll(string contextTypeFullname, string entityTypeFullname, PaginationParameters pagination, string OrderBy);

        /// <summary>
        /// Update an entity in a specific context
        /// </summary>
        /// <param name="contextTypeFullname">The fullname of the context</param>
        /// <param name="entityTypeFullname">The fullname of the entity in the context</param>
        /// <param name="entity">The type wished to be updated</param>
        /// <returns>The modified entity</returns>
        public object Update(string contextTypeFullname, string entityFullname, object entity);

        public SQLQueryResult GetSQLQueryEntityDefinition(CRUDExecuteSQLQueryRequest request);
        public object CreateOrUpdate(string contextTypeFullname, string entityFullname, object entity);
        public void Delete(string contextTypeFullname, string entityFullname, object entityIdentifier);
    }
}