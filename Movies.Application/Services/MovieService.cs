using FluentValidation;
using Movies.Application.Models;
using Movies.Application.Repositories;

namespace Movies.Application.Services;

public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly IValidator<Movie> _movieValidator;

    public MovieService(IMovieRepository movieRepository, IValidator<Movie> movieValidator)
    {
        _movieRepository = movieRepository;
        _movieValidator = movieValidator;
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
    public async Task<IEnumerable<Movie>> GetAllAsync(CancellationToken token = default)
    {
        return await _movieRepository.GetAllAsync(token);
    }

    public async Task<Movie?> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        return await _movieRepository.GetByIdAsync(id, token);
    }

    public async Task<Movie?> GetBySlugAsync(string id, CancellationToken token = default)
    {
        return await _movieRepository.GetBySlugAsync(id, token);
    }

    public async Task<Movie?> UpdateAsync(Movie movie, CancellationToken token = default)
    {
        await _movieValidator.ValidateAndThrowAsync(movie, token);
        var exists = await _movieRepository.ExistByIdAsync(movie.Id, token);
        if (exists)
        {
            if (await _movieRepository.UpdateAsync(movie, token))
            {
                return movie;
            }
        }
        return null;
    }
}
