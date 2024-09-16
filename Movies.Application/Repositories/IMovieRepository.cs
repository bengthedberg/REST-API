using Movies.Application.Models;

namespace Movies.Application.Repositories;

public interface IMovieRepository
{
  Task<Movie?> GetByIdAsync(Guid id, CancellationToken token = default);
  Task<Movie?> GetBySlugAsync(string id, CancellationToken token = default);
  Task<IEnumerable<Movie>> GetAllAsync(CancellationToken token = default);
  Task<bool> CreateAsync(Movie movie, CancellationToken token = default); // true if movie was created successfully
  Task<bool> UpdateAsync(Movie movie, CancellationToken token = default); // true if movie was updated successfully
  Task<bool> DeleteAsync(Guid id, CancellationToken token = default); // true if movie was deleted successfully
  Task<bool> ExistByIdAsync(Guid id, CancellationToken token = default);
}
