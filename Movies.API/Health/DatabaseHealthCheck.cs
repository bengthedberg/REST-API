using Microsoft.Extensions.Diagnostics.HealthChecks;
using Movies.Application.Database;

namespace Movies.API.Health;

public class DatabaseHealthCheck : IHealthCheck
{
    public static string Name => "Database";
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IDatabaseConnectionFactory databaseConnectionFactory, ILogger<DatabaseHealthCheck> logger)
    {
        _databaseConnectionFactory = databaseConnectionFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Check the database connection
        // If the connection is successful, return HealthCheckResult.Healthy
        // If the connection is not successful, return HealthCheckResult.Unhealthy
        // If the connection is slow, return HealthCheckResult.Degraded
        try
        {
            _ = await _databaseConnectionFactory.CreateConnectionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Database health check failed", ex);
            return HealthCheckResult.Unhealthy(ex.Message);
        }

        return HealthCheckResult.Healthy();
    }
}
