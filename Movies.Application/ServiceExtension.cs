using Microsoft.Extensions.DependencyInjection;
using Movies.Application.Database;
using Movies.Application.Repositories;

public static class ServiceExtension
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IMovieRepository, MovieRepository>();
        return services;
    }

    public static IServiceCollection AddDatabases(this IServiceCollection services, string connectionString)
    {
        // Note The factory is a singleton instance, but the CreateConnectionAsync method will create a new 
        // connection for each request.
        services.AddSingleton<IDatabaseConnectionFactory>(_ => new NpgSqlConnectionFactory(connectionString));
        services.AddSingleton<DatabaseMigration>();
        return services;
    }
}