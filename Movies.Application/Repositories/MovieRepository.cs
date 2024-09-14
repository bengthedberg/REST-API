using Movies.Application.Models;

namespace Movies.Application.Repositories;

public class MovieRepository : IMovieRepository
{
    // Implement an in-memory representation of the movie resource.
    // Will be replaced with an actual database later.
    private readonly List<Movie> _movies = new();

    public async Task<bool> CreateAsync(Movie movie)
    {
        _movies.Add(movie);
        return await Task.FromResult(true);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var records = await Task.FromResult(_movies.RemoveAll(m => m.Id == id));
        return records > 0; 
    }

    public async Task<IEnumerable<Movie>> GetAllAsync()
    {
        return await Task.FromResult(_movies.AsEnumerable());
    }

    public async Task<Movie?> GetByIdAsync(Guid id)
    {
        return await Task.FromResult(_movies.SingleOrDefault(m => m.Id == id));
    }

    public async Task<Movie?> GetBySlugAsync(string id)
    {
        return await Task.FromResult(_movies.SingleOrDefault(m => m.Slug == id));
    }

    public async Task<bool> UpdateAsync(Movie movie)
    {
        var index = await Task.FromResult(_movies.FindIndex(m => m.Id == movie.Id));
        if (index != -1)
        {
            _movies[index] = movie;
            return true;
        }
        return false;
    }
}
