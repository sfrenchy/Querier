namespace Querier.Api.Models.Interfaces
{
    public interface IMQMessage
    {
        byte[] GetBytes();
        string ToJSONString();
    }
}