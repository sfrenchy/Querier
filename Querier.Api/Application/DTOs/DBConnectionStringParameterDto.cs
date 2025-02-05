using Querier.Api.Domain.Entities.DBConnection;

namespace Querier.Api.Application.DTOs;

public class DBConnectionStringParameterDto
{
    public int Id { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
    public bool IsEncrypted { get; set; }

    public static DBConnectionStringParameterDto FromEntity(ConnectionStringParameter parameter)
    {
        return new DBConnectionStringParameterDto
        {
            Id = parameter.Id,
            Key = parameter.Key,
            Value = parameter.StoredValue,
            IsEncrypted = parameter.IsEncrypted
        };
    }
}