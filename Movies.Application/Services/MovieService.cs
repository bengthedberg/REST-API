using Movies.Application.Models;
using Movies.Application.Repositories;

namespace Movies.Application.Services;

public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;

    public MovieService(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    public async Task<bool> CreateAsync(Movie movie)
    {
        return await _movieRepository.CreateAsync(movie);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _movieRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<Movie>> GetAllAsync()
    {
        return await _movieRepository.GetAllAsync();
    }

    public async Task<Movie?> GetByIdAsync(Guid id)
    {
        return await _movieRepository.GetByIdAsync(id);
    }

    public async Task<Movie?> GetBySlugAsync(string id)
    {
        return await _movieRepository.GetBySlugAsync(id);
    }

    public async Task<Movie?> UpdateAsync(Movie movie)
    {
        var exists = await _movieRepository.ExistByIdAsync(movie.Id);
        if (exists)
        {
            if (await _movieRepository.UpdateAsync(movie))
            {
                return movie;
            }
        }
        return null;
    }
}
