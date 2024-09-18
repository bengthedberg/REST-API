using Movies.Application.Models;

namespace Movies.Application.Repositories;

public interface IMovieRepository
{
  Task<Movie?> GetByIdAsync(Guid id, Guid? userId = default, CancellationToken token = default);
  Task<Movie?> GetBySlugAsync(string id, Guid? userId = default, CancellationToken token = default);
  Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken token = default);
  Task<bool> CreateAsync(Movie movie, CancellationToken token = default); // true if movie was created successfully
  Task<bool> UpdateAsync(Movie movie, Guid? userId = default, CancellationToken token = default); // true if movie was updated successfully
  Task<bool> DeleteAsync(Guid id, CancellationToken token = default); // true if movie was deleted successfully
  Task<bool> ExistByIdAsync(Guid id, CancellationToken token = default);
  Task<int> GetCountAsync(string? title, int? year, CancellationToken token = default);
}
