namespace Querier.Api.Domain.Common.Enums
{
    public enum DBConnectionState
    {
        Unknown = 0,
        Connected = 1,
        ConnectionError = 2,
        CompilationError = 3,
        Available = 4,
        LoadError = 5
    }
}