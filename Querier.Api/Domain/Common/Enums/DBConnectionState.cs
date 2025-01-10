namespace Querier.Api.Domain.Common.Enums
{
    public enum DBConnectionState
    {
        None,
        ConnectionError,
        Connected,
        CompilationError,
        Available
    }
}