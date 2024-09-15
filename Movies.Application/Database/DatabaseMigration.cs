using Dapper;
namespace Movies.Application.Database;

public class DatabaseMigration
{

  private readonly IDatabaseConnectionFactory _connectionFactory; 

  public DatabaseMigration(IDatabaseConnectionFactory connectionFactory)
  {
      _connectionFactory = connectionFactory;
  }

  public async Task Migrate()
  {
    using var connection = await _connectionFactory.CreateConnectionAsync();
    await connection.ExecuteAsync(
    """
    CREATE TABLE IF NOT EXISTS movies (
        id UUID PRIMARY KEY,
        title TEXT NOT NULL,
        slug TEXT NOT NULL,
        year INTEGER NOT NULL
    );
    """);
  await connection.ExecuteAsync(
    """
    CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS idx_movies_slug ON movies USING btree(slug);
    """);

  await connection.ExecuteAsync(
    """
    CREATE TABLE IF NOT EXISTS genres (
        movieId UUID REFERENCES movies(id) ON DELETE CASCADE,
        name TEXT NOT NULL,
        PRIMARY KEY(movieId, name)
    );
    """);
      
  }    

}
