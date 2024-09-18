using FluentValidation;
using Movies.Application.Models;
using Movies.Application.Repositories;

namespace Movies.Application.Services;

public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly IValidator<Movie> _movieValidator;
    private readonly IValidator<GetAllMoviesOptions> _getAllMoviesOptionsValidator;
    private readonly IRatingRepository _ratingRepository;

    public MovieService(IMovieRepository movieRepository,
        IValidator<Movie> movieValidator,
        IValidator<GetAllMoviesOptions> getAllMoviesOptionsValidator,
        IRatingRepository ratingRepository)
    {
        _movieRepository = movieRepository;
        _movieValidator = movieValidator;
        _getAllMoviesOptionsValidator = getAllMoviesOptionsValidator;
        _ratingRepository = ratingRepository;
    }

    public async Task<bool> CreateAsync(Movie movie, CancellationToken token = default)
    {
        await _movieValidator.ValidateAndThrowAsync(movie, token);
        return await _movieRepository.CreateAsync(movie, token);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken token = default)
    {
        return await _movieRepository.DeleteAsync(id, token);
    }
    public async Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken token = default)
    {
        await _getAllMoviesOptionsValidator.ValidateAndThrowAsync(options, token);
        return await _movieRepository.GetAllAsync(options, token);
    }

    public async Task<Movie?> GetByIdAsync(Guid id, Guid? userId = default, CancellationToken token = default)
    {
        return await _movieRepository.GetByIdAsync(id, userId, token);
    }

    public async Task<Movie?> GetBySlugAsync(string id, Guid? userId = default, CancellationToken token = default)
    {
        return await _movieRepository.GetBySlugAsync(id, userId, token);
    }

    public async Task<Movie?> UpdateAsync(Movie movie, Guid? userId = default, CancellationToken token = default)
    {
        await _movieValidator.ValidateAndThrowAsync(movie, token);
        var exists = await _movieRepository.ExistByIdAsync(movie.Id, token);
        if (!exists)
        {
            return null;
        }

        await _movieRepository.UpdateAsync(movie, userId, token);

        if (userId.HasValue)
        {
            (movie.Rating, movie.UserRating) = await _ratingRepository.GetRatingAsync(movie.Id, userId.Value, token);
        }
        else
        {
            movie.Rating = await _ratingRepository.GetRatingAsync(movie.Id, token);
        }

        return movie;
    }
}
