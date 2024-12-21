using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Querier.Api.Domain.Entities;

public class SQLQueryConfiguration : IEntityTypeConfiguration<SQLQuery>
{
    public void Configure(EntityTypeBuilder<SQLQuery> builder)
    {
        builder.ToTable("SQLQueries");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Query).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        
        // Configuration du stockage JSON pour Parameters
        builder.Property(x => x.Parameters)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null))
            .HasColumnType("jsonb"); // Pour PostgreSQL, utilisez "json" pour SQLite/SQL Server
    }
}