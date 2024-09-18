using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Querier.Api.Tools
{
    public static class DbContextExtensions
    {
        public static async Task<List<T>> SqlQueryAsync<T>(this DbContext db, string sql, object[]? parameters = null, CancellationToken cancellationToken = default) where T : class
        {
            if (parameters is null)
            {
                parameters = new object[] { };
            }

            if (typeof(T).GetProperties().Any())
            {
                using (var db2 = new ContextForQueryType<T>(db.Database.GetDbConnection(), db.Database.CurrentTransaction))
                {
                    db2.Database.SetCommandTimeout(db.Database.GetCommandTimeout());
                    return await db2.Set<T>().FromSqlRaw(sql, parameters).ToListAsync(cancellationToken);
                }
            }
            else
            {
                await db.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
                return default;
            }
        }

        private sealed class ContextForQueryType<T> : DbContext where T : class
        {
            private readonly DbConnection connection;
            private readonly IDbContextTransaction transaction;

            public ContextForQueryType(DbConnection connection, IDbContextTransaction tran)
            {
                this.connection = connection;
                transaction = tran;

                if (tran != null)
                {
                    _ = Database.UseTransaction((tran as IInfrastructure<DbTransaction>).Instance);
                }
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                if (transaction != null)
                {
                    optionsBuilder.UseSqlServer(connection);
                }
                else
                {
                    optionsBuilder.UseSqlServer(connection, options => options.EnableRetryOnFailure());
                }
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<T>().HasNoKey().ToView(null);
            }
        }
    }

    public class OutputParameter<TValue>
    {
        private bool _valueSet = false;

        private TValue? _value;
        public TValue Value
        {
            get
            {
                if (!_valueSet)
                    throw new InvalidOperationException("Value not set.");

                return _value;
            }
        }

        public void SetValue(object value)
        {
            _valueSet = true;

            _value = null == value || Convert.IsDBNull(value) ? default : (TValue)value;
        }
    }
}
