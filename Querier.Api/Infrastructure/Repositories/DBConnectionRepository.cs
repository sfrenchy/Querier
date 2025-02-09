using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Entities.DBConnection;
using Querier.Api.Infrastructure.Data.Context;
using Querier.Api.Infrastructure.Services;

namespace Querier.Api.Infrastructure.Repositories
{
    public class DBConnectionRepository
    {
        private readonly ApiDbContext _context;
        private readonly IEncryptionService _encryptionService;

        public DBConnectionRepository(ApiDbContext context, IEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public async Task<DBConnection> GetByIdAsync(int id)
        {
            var connection = await _context.DBConnections
                .Include(d => d.Parameters)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (connection != null)
            {
                // Injecter le service d'encryption dans chaque paramètre
                foreach (var param in connection.Parameters)
                {
                    param.EncryptionService = _encryptionService;
                }
            }

            return connection;
        }

        public async Task<List<DBConnectionDto>> GetAllDbConnectionsAsync()
        {
            // Charger les données et les transformer en DTO avant que le contexte ne soit disposé
            var connections = await _context.DBConnections
                .Include(c => c.Parameters)
                .AsNoTracking()  // Pour de meilleures performances en lecture seule
                .Select(c => new DBConnectionDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ConnectionType = c.ConnectionType,
                    Parameters = c.Parameters.Select(p => new DBConnectionStringParameterDto
                    {
                        Id = p.Id,
                        Key = p.Key,
                        IsEncrypted = p.IsEncrypted,
                        Value = p.StoredValue,
                    }).ToList(),
                    ApiRoute = c.ApiRoute,
                    ContextName = c.ContextName,
                    Description = c.Description
                })
                .ToListAsync();

            return connections;
        }

        public async Task<DBConnection> FindByIdAsync(int id)
        {
            var connection = await _context.DBConnections
                .Include(c => c.Parameters)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (connection != null)
            {
                // Injecter le service d'encryption dans chaque paramètre
                foreach (var param in connection.Parameters)
                {
                    param.EncryptionService = _encryptionService;
                }
            }

            return connection;
        }

        public async Task AddDbConnectionAsync(DBConnection connection)
        {
            // Injecter le service d'encryption dans les paramètres avant l'ajout
            foreach (var param in connection.Parameters)
            {
                param.EncryptionService = _encryptionService;
            }

            _context.DBConnections.Add(connection);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDbConnectionAsync(int id)
        {
            var connection = await _context.DBConnections
                .Include(c => c.Parameters)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (connection != null)
            {
                _context.DBConnections.Remove(connection);
                await _context.SaveChangesAsync();
            }
        }

        // Autres méthodes du repository...
    }
} 