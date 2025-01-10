using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Services
{
    public class RowService(IRowRepository repository, ICardService cardService) : IRowService
    {
        public async Task<RowDto> GetByIdAsync(int id)
        {
            var row = await repository.GetByIdAsync(id);
            return row != null ? await MapToResponse(row) : null;
        }

        public async Task<IEnumerable<RowDto>> GetByPageIdAsync(int pageId)
        {
            var rows = await repository.GetByPageIdAsync(pageId);
            var tasks = rows.Select(MapToResponse);
            return await Task.WhenAll(tasks);
        }

        public async Task<RowDto> CreateAsync(int pageId, RowCreateDto request)
        {
            var order = await repository.GetMaxOrderInPageAsync(pageId) + 1;
            
            var row = new Row
            {
                Order = order,
                PageId = pageId,
                Height = request.Height,
            };

            var result = await repository.CreateAsync(row);
            return await MapToResponse(result);
        }

        public async Task<RowDto> UpdateAsync(int id, RowCreateDto request)
        {
            var existingRow = await repository.GetByIdAsync(id);
            if (existingRow == null) return null;

            existingRow.Height = request.Height;

            var result = await repository.UpdateAsync(existingRow);
            return await MapToResponse(result);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await repository.DeleteAsync(id);
        }

        public async Task<bool> ReorderAsync(int pageId, List<int> rowIds)
        {
            var rows = await repository.GetByPageIdAsync(pageId);
            var rowDict = rows.ToDictionary(r => r.Id);

            for (int i = 0; i < rowIds.Count; i++)
            {
                if (rowDict.TryGetValue(rowIds[i], out var row))
                {
                    row.Order = i + 1;
                    await repository.UpdateAsync(row);
                }
            }

            return true;
        }

        private async Task<RowDto> MapToResponse(Row row)
        {
            var cards = await cardService.GetByRowIdAsync(row.Id);
            
            return new RowDto
            {
                Id = row.Id,
                Order = row.Order,
                Height = row.Height,
                Cards = cards.ToList()
            };
        }
    }
} 