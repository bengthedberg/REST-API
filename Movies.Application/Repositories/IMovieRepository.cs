using Movies.Application.Models;

namespace Movies.Application.Repositories;

public interface IMovieRepository
{
  Task<Movie?> GetByIdAsync(Guid id); 
  Task<Movie?> GetBySlugAsync(string id); 
  Task<IEnumerable<Movie>> GetAllAsync(); 
  Task<bool> CreateAsync(Movie movie); // true if movie was created successfully
  Task<bool> UpdateAsync(Movie movie); // true if movie was updated successfully
  Task<bool> DeleteAsync(Guid id); // true if movie was deleted successfully
}
