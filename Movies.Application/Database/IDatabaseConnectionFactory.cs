using System.Data;

namespace Movies.Application.Database;

public interface IDatabaseConnectionFactory
{ 
    Task<IDbConnection> CreateConnectionAsync();
}