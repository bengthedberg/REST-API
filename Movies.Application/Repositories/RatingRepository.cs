using Dapper;
using Movies.Application.Database;

namespace Movies.Application.Repositories;

public class RatingRepository : IRatingRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactoryConnection;

    public RatingRepository(IDatabaseConnectionFactory databaseConnectionFactory)
    {
        _connectionFactoryConnection = databaseConnectionFactory;
    }

    public async Task<bool> UpsertRatingAsync(Guid movieId, Guid userId, int rating, CancellationToken token = default)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
        var result = await connection.ExecuteAsync(new CommandDefinition("""
                    INSERT INTO ratings (userId, movieId, rating) VALUES (@UserId, @MovieId, @Rating)
                    ON CONFLICT (userId, movieId) DO UPDATE SET rating = @Rating
                    """, new { UserId = userId, MovieId = movieId, Rating = rating }, cancellationToken: token));
        return result > 0;
    }

    public async Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
        var result = await connection.ExecuteAsync(new CommandDefinition("""
                    DELETE FROM ratings WHERE movieId = @MovieId AND userId = @UserId
                    """, new { MovieId = movieId, UserId = userId }, cancellationToken: token));
        return result > 0;
    }

    public async Task<float?> GetRatingAsync(Guid movieId, CancellationToken token = default)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
        return await connection.QuerySingleOrDefaultAsync<float?>(new CommandDefinition("""
            SELECT round(avg(rating), 1) FROM ratings WHERE id = @MovieId                      
            """, new { MovieId = movieId }, cancellationToken: token));
    }

    public async Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
    {
        using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
        return await connection.QuerySingleOrDefaultAsync<(float? Rating, int? UserRating)>(new CommandDefinition("""
            SELECT round(avg(r.rating), 1) as rating, min(ur.rating) as userrating 
            FROM movies m
             LEFT JOIN ratings ur ON m.id = ur.movieId AND ur.userId = @UserId 
             LEFT JOIN ratings r ON m.id = r.movieId 
            WHERE m.id = @MovieId                     
            """, new { MovieId = movieId, @UserId = userId }, cancellationToken: token));
    }
}
