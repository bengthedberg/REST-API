using Dapper;
using Movies.Application.Database;
using Movies.Application.Models;

namespace Movies.Application.Repositories;

public class MovieRepository : IMovieRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactoryConnection;

    public MovieRepository(IDatabaseConnectionFactory databaseConnectionFactory)
    {
        _connectionFactoryConnection = databaseConnectionFactory;
    }

    public async Task<bool> CreateAsync(Movie movie)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync();
        // Use a transaction as we updates multiple tables in the database
        using var transaction = connection.BeginTransaction();

        var result = await connection.ExecuteAsync( new CommandDefinition("""
            INSERT INTO movies (id, title, slug, year) VALUES (@Id, @Title, @Slug, @Year)
            """, movie));
        if (result > 0)
        {
            foreach (var genre in movie.Genre)
            {
                result = await connection.ExecuteAsync( new CommandDefinition("""
                    INSERT INTO genres (movieId, name) VALUES (@MovieId, @Name)
                    """, new { MovieId = movie.Id, Name = genre }));
            }
        }    

        transaction.Commit();
        return result > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync();
        var result = await connection.ExecuteAsync( new CommandDefinition("""
            DELETE FROM movies WHERE id = @Id
            """, new { Id = id }));
        return result > 0;
    }

    public async Task<bool> ExistByIdAsync(Guid id)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync();
        var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
            SELECT COUNT(*) FROM movies WHERE id = @Id
            """, new { Id = id }));
        return exists;
    }

    public async Task<IEnumerable<Movie>> GetAllAsync()
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync();
        var result = await connection.QueryAsync(new CommandDefinition("""
            SELECT m.*, string_agg(g.name, ',') as genres 
              FROM movies m LEFT JOIN genres g ON m.id = g.movieId GROUP BY id
            """));
        return result.Select(x => new Movie() {
            Id = x.id,
            Title = x.title,
            Year = x.year,
            Genre = Enumerable.ToList(x.genres.Split(','))
        });
    }

    public async Task<Movie?> GetByIdAsync(Guid id)
    {
         using var connection = await _connectionFactoryConnection.CreateConnectionAsync();
         var movie = await connection.QueryFirstOrDefaultAsync<Movie>(new CommandDefinition("""
            SELECT * FROM movies WHERE id = @Id
            """, new { Id = id }));
        
        if (movie is null)  return null;

        var genres = await connection.QueryAsync<string>(new CommandDefinition("""
            SELECT name FROM genres WHERE movieId = @MovieId
            """, new { MovieId = id }));
        foreach (var genre in genres)
        {
            movie.Genre.Add(genre);
        }        
    
        return movie;
    }

    public async Task<Movie?> GetBySlugAsync(string id)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync();
        var movie = await connection.QueryFirstOrDefaultAsync<Movie>(new CommandDefinition("""
            SELECT * FROM movies WHERE slug = @Slug
            """, new { Slug = id }));

        if (movie is null)  return null;

        var genres = await connection.QueryAsync<string>(new CommandDefinition("""
            SELECT name FROM genres WHERE movieId = @MovieId
            """, new { MovieId = movie.Id }));
        foreach (var genre in genres)
        {
            movie.Genre.Add(genre);
        }        
        
        return movie;          
    }

    public async Task<bool> UpdateAsync(Movie movie)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync();
        // Use a transaction as we updates multiple tables in the database
        using var transaction = connection.BeginTransaction();        

        await connection.ExecuteAsync( new CommandDefinition("""
            DELETE FROM genres WHERE movieId = @Id
            """, new { Id = movie.Id }));
            
        foreach (var genre in movie.Genre)
        {
            await connection.ExecuteAsync( new CommandDefinition("""
                INSERT INTO genres (movieId, name) VALUES (@MovieId, @Name)
                """, new { MovieId = movie.Id, Name = genre }));
        }

        var result = await connection.ExecuteAsync( new CommandDefinition("""
            UPDATE movies SET title = @Title, slug = @Slug, year = @Year WHERE id = @Id
            """, movie));

        transaction.Commit();

        return result > 0;
    }
}
