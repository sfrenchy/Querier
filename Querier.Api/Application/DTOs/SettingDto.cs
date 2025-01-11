using System;
using Querier.Api.Domain.Common.Metadata;

namespace Querier.Api.Application.DTOs
{
    public class SettingDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public Type Type { get; set; }

        public static SettingDto FromEntity(Setting entity)
        {
            return new SettingDto()
            {
                Id = entity.Id,
                Name = entity.Name,
                Value = entity.Value,
                Description = entity.Description,
                Type = Type.GetType(entity.Type)
            };
        }
    }
}