using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Diagnostics.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Scaffolding.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using Pomelo.EntityFrameworkCore.MySql.Diagnostics.Internal;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;
using Pomelo.EntityFrameworkCore.MySql.Storage.Internal;

namespace Querier.Api.Domain.Services
{
    public class DatabaseScaffolderFactory
    {
        public static IReverseEngineerScaffolder CreateMssqlScaffolder() =>
            new ServiceCollection()
                .AddEntityFrameworkSqlServer()
                .AddLogging()
                .AddEntityFrameworkDesignTimeServices()
                .AddSingleton<LoggingDefinitions, SqlServerLoggingDefinitions>()
                .AddSingleton<IRelationalTypeMappingSource, SqlServerTypeMappingSource>()
                .AddSingleton<IAnnotationCodeGenerator, AnnotationCodeGenerator>()
                .AddSingleton<IDatabaseModelFactory, SqlServerDatabaseModelFactory>()
                .AddSingleton<IProviderConfigurationCodeGenerator, SqlServerCodeGenerator>()
                .AddSingleton<IScaffoldingModelFactory, RelationalScaffoldingModelFactory>()
                .AddSingleton<IPluralizer, Bricelam.EntityFrameworkCore.Design.Pluralizer>()
                .AddSingleton<ProviderCodeGeneratorDependencies>()
                .AddSingleton<AnnotationCodeGeneratorDependencies>()
                .BuildServiceProvider()
                .GetRequiredService<IReverseEngineerScaffolder>();

        public static IReverseEngineerScaffolder CreateMySQLScaffolder() =>
            new ServiceCollection()
                .AddEntityFrameworkMySql()
                .AddLogging()
                .AddEntityFrameworkDesignTimeServices()
                .AddSingleton<LoggingDefinitions, MySqlLoggingDefinitions>()
                .AddSingleton<IRelationalTypeMappingSource, MySqlTypeMappingSource>()
                .AddSingleton<IAnnotationCodeGenerator, AnnotationCodeGenerator>()
                .AddSingleton<IDatabaseModelFactory, MySqlDatabaseModelFactory>()
                .AddSingleton<IProviderConfigurationCodeGenerator, MySqlCodeGenerator>()
                .AddSingleton<IScaffoldingModelFactory, RelationalScaffoldingModelFactory>()
                .AddSingleton<IPluralizer, Bricelam.EntityFrameworkCore.Design.Pluralizer>()
                .AddSingleton<ProviderCodeGeneratorDependencies>()
                .AddSingleton<AnnotationCodeGeneratorDependencies>()
                .BuildServiceProvider()
                .GetRequiredService<IReverseEngineerScaffolder>();

        public static IReverseEngineerScaffolder CreatePgSQLScaffolder() =>
            new ServiceCollection()
                .AddEntityFrameworkNpgsql()
                .AddLogging()
                .AddEntityFrameworkDesignTimeServices()
                .AddSingleton<LoggingDefinitions, NpgsqlLoggingDefinitions>()
                .AddSingleton<IRelationalTypeMappingSource, NpgsqlTypeMappingSource>()
                .AddSingleton<IAnnotationCodeGenerator, AnnotationCodeGenerator>()
                .AddSingleton<IDatabaseModelFactory, NpgsqlDatabaseModelFactory>()
                .AddSingleton<IProviderConfigurationCodeGenerator, NpgsqlCodeGenerator>()
                .AddSingleton<IScaffoldingModelFactory, RelationalScaffoldingModelFactory>()
                .AddSingleton<IPluralizer, Bricelam.EntityFrameworkCore.Design.Pluralizer>()
                .AddSingleton<ProviderCodeGeneratorDependencies>()
                .AddSingleton<AnnotationCodeGeneratorDependencies>()
                .BuildServiceProvider()
                .GetRequiredService<IReverseEngineerScaffolder>();
    }
} 