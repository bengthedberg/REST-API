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

    public async Task<bool> CreateAsync(Movie movie, CancellationToken token = default)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
        // Use a transaction as we updates multiple tables in the database
        using var transaction = connection.BeginTransaction();

        var result = await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO movies (id, title, slug, year) VALUES (@Id, @Title, @Slug, @Year)
            """, movie, cancellationToken: token));
        if (result > 0)
        {
            foreach (var genre in movie.Genre)
            {
                result = await connection.ExecuteAsync(new CommandDefinition("""
                    INSERT INTO genres (movieId, name) VALUES (@MovieId, @Name)
                    """, new { MovieId = movie.Id, Name = genre }, cancellationToken: token));
            }
        }

        transaction.Commit();
        return result > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken token = default)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
        var result = await connection.ExecuteAsync(new CommandDefinition("""
            DELETE FROM movies WHERE id = @Id
            """, new { Id = id }, cancellationToken: token));
        return result > 0;
    }

    public async Task<bool> ExistByIdAsync(Guid id, CancellationToken token = default)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
        var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
            SELECT COUNT(*) FROM movies WHERE id = @Id
            """, new { Id = id }, cancellationToken: token));
        return exists;
    }

    public async Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken token = default)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
        var result = await connection.QueryAsync(new CommandDefinition("""
            SELECT m.*, string_agg(distinct g.name, ',') as genres, round(avg(r.rating), 1) as rating, min(ur.rating) as userrating  
            FROM movies m 
             LEFT JOIN genres g ON m.id = g.movieId 
             LEFT JOIN ratings ur ON m.id = ur.movieId AND ur.userId = @UserId 
             LEFT JOIN ratings r ON m.id = r.movieId    
            WHERE (@Year IS NULL OR m.year = @Year) 
              AND (@Title IS NULL OR m.title ILIKE ( '%' || @Title || '%' ))          
            GROUP BY m.id
            """, new { UserId = options.UserId, Year = options.Year, Title = options.Title }, cancellationToken: token));
        return result.Select(x => new Movie()
        {
            Id = x.id,
            Title = x.title,
            Year = x.year,
            Rating = (float?)x.rating,
            UserRating = (int?)x.userrating,
            Genre = Enumerable.ToList(x.genres.Split(','))
        });
    }

    public async Task<Movie?> GetByIdAsync(Guid id, Guid? userId = default, CancellationToken token = default)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
        var movie = await connection.QueryFirstOrDefaultAsync<Movie>(new CommandDefinition("""
            SELECT m.*, round(avg(r.rating), 1) as rating, min(ur.rating) as userrating 
            FROM movies m
             LEFT JOIN ratings ur ON m.id = ur.movieId AND ur.userId = @UserId 
             LEFT JOIN ratings r ON m.id = r.movieId 
            WHERE m.id = @Id 
            GROUP BY m.id
            """, new { Id = id, UserId = userId }, cancellationToken: token));

        if (movie is null) return null;

        var genres = await connection.QueryAsync<string>(new CommandDefinition("""
            SELECT name FROM genres WHERE movieId = @MovieId
            """, new { MovieId = id }, cancellationToken: token));
        foreach (var genre in genres)
        {
            movie.Genre.Add(genre);
        }

        return movie;
    }

    public async Task<Movie?> GetBySlugAsync(string id, Guid? userId = default, CancellationToken token = default)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
        var movie = await connection.QueryFirstOrDefaultAsync<Movie>(new CommandDefinition("""
            SELECT m.*, ROUND(AVG(r.rating), 1) AS rating, myr.rating AS userrating
            FROM movies m
             LEFT JOIN  ratings r ON m.id = r.movieid
             LEFT JOIN  ratings myr ON m.id = myr.movieid
                AND myr.userid = @UserId
            WHERE slug = @Id
            GROUP BY m.id, userrating            
            """, new { Id = id, UserId = userId }, cancellationToken: token));

        if (movie is null) return null;

        var genres = await connection.QueryAsync<string>(new CommandDefinition("""
            SELECT name FROM genres WHERE movieId = @MovieId
            """, new { MovieId = movie.Id }, cancellationToken: token));
        foreach (var genre in genres)
        {
            movie.Genre.Add(genre);
        }

        return movie;
    }

    public async Task<bool> UpdateAsync(Movie movie, Guid? userId = default, CancellationToken token = default)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
        // Use a transaction as we updates multiple tables in the database
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(new CommandDefinition("""
            DELETE FROM genres WHERE movieId = @Id
            """, new { Id = movie.Id }, cancellationToken: token));

        foreach (var genre in movie.Genre)
        {
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO genres (movieId, name) VALUES (@MovieId, @Name)
                """, new { MovieId = movie.Id, Name = genre }, cancellationToken: token));
        }

        var result = await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE movies SET title = @Title, slug = @Slug, year = @Year WHERE id = @Id
            """, movie, cancellationToken: token));

        transaction.Commit();

        return result > 0;
    }
}
