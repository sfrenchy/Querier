using System;

namespace $nameSpace$.Exceptions
{
    /// <summary>
    /// Base exception for all dynamic context related exceptions
    /// </summary>
    public class DynamicContextException : Exception
    {
        public DynamicContextException(string message) : base(message) { \}
        public DynamicContextException(string message, Exception innerException) : base(message, innerException) { \}
    \}

    /// <summary>
    /// Exception thrown when an entity is not found
    /// </summary>
    public class EntityNotFoundException : DynamicContextException
    {
        public string EntityName { get; \}
        public object EntityId { get; \}

        public EntityNotFoundException(string entityName, object entityId)
            : base(entityName + " with id " + entityId + " was not found")
        {
            EntityName = entityName;
            EntityId = entityId;
        \}
    \}

    /// <summary>
    /// Exception thrown when a validation error occurs
    /// </summary>
    public class EntityValidationException : DynamicContextException
    {
        public string EntityName { get; \}

        public EntityValidationException(string entityName, string message)
            : base("Validation failed for " + entityName + ": " + message)
        {
            EntityName = entityName;
        \}
    \}

    /// <summary>
    /// Exception thrown when a database operation fails
    /// </summary>
    public class DatabaseOperationException : DynamicContextException
    {
        public string Operation { get; \}
        public string EntityName { get; \}

        public DatabaseOperationException(string operation, string entityName, Exception innerException)
            : base("Database operation '" + operation + "' failed for " + entityName, innerException)
        {
            Operation = operation;
            EntityName = entityName;
        \}
    \}

    /// <summary>
    /// Exception thrown when a stored procedure execution fails
    /// </summary>
    public class StoredProcedureException : DynamicContextException
    {
        public string ProcedureName { get; \}

        public StoredProcedureException(string procedureName, string message, Exception? innerException = null)
            : base("Stored procedure '" + procedureName + "' execution failed: " + message, innerException)
        {
            ProcedureName = procedureName;
        \}
    \}

    /// <summary>
    /// Exception thrown when cache operations fail
    /// </summary>
    public class CacheOperationException : DynamicContextException
    {
        public string Operation { get; \}

        public CacheOperationException(string operation, string message, Exception? innerException = null)
            : base("Cache operation '" + operation + "' failed: " + message, innerException)
        {
            Operation = operation;
        \}
    \}
\} 