
using System.Runtime.CompilerServices;
using FluentValidation;
using FluentValidation.Results;
using Movies.Application.Models;
using Movies.Application.Repositories;

namespace Movies.Application.Services;

public class RatingService : IRatingService
{
    private readonly IRatingRepository _ratingRepository;
    private readonly IMovieRepository _movieRepository;


    public RatingService(IRatingRepository ratingRepository, IMovieRepository movieRepository)
    {
        _ratingRepository = ratingRepository;
        _movieRepository = movieRepository;
    }

    public async Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
    {
        var movieExists = await _movieRepository.ExistByIdAsync(movieId, token);
        if (!movieExists)
        {
            return false;
        }
        return await _ratingRepository.DeleteRatingAsync(movieId, userId, token);
    }

    public async Task<IEnumerable<MovieRating>> GetRatingsByUserAsync(Guid userId, CancellationToken token = default)
    {
        return await _ratingRepository.GetRatingsByUserAsync(userId, token);
    }

    public async Task<bool> RateMovieAsync(Guid movieId, Guid userId, int rating, CancellationToken token = default)
    {
        if (rating < 1 || rating > 5)
        {
            throw new ValidationException([
                new ValidationFailure("rating", "Rating must be between 1 and 5")
            ]);
        }
        if (userId == Guid.Empty)
        {
            throw new ValidationException([
                new ValidationFailure("userId", "User ID must be provided")
            ]);
        }
        var movieExists = await _movieRepository.ExistByIdAsync(movieId, token);
        if (!movieExists)
        {
            return false;
        }

        return await _ratingRepository.UpsertRatingAsync(movieId, userId, rating, token);
    }
}
