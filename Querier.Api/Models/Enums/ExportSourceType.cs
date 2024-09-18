namespace Querier.Api.Models.Enums;

public enum ExportSourceType
{
    /// <summary>
    /// The source type is a procedure service in a dynamic context. Useful for service based datas retrieving
    /// </summary>
    dynamicContextProcedureService,
    /// <summary>
    /// The source type is an entity. Useful for entity and entityCRUDService based datas retrieving
    /// </summary>
    entity,
    /// <summary>
    /// The source tpye is a sql query. Useful for sql query based datas retrieving
    /// </summary>
    sql
}