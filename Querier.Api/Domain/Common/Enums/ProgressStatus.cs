namespace Querier.Api.Domain.Common.Enums
{
    public enum ProgressStatus
    {
        Starting,
        ValidatingConnection,
        ConnectionValidated,
        RetrievingSchema,
        SchemaRetrieved,
        GeneratingControllers,
        GeneratingEntities,
        ControllersGenerated,
        Compiling,
        CompilationSucceeded,
        LoadingAssembly,
        Completed,
        Failed
    }
} 