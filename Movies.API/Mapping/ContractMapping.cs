using Movies.Application.Models;
using Movies.Contracts.Requests;
using Movies.Contracts.Responses;

namespace Movies.API.Mapping;

public static class ContractMapping
{
  public static Movie ToMovie(this CreateMovieRequest request)
  {
    return new Movie()
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Year = request.Year,
            Genre = request.Genre.ToList()
        };
  }

  public static Movie ToMovie(this UpdateMovieRequest request, Guid id)
  {
    return new Movie()
        {
            Id = id,
            Title = request.Title,
            Year = request.Year,
            Genre = request.Genre.ToList()
        };
  }

  public static MovieResponse ToMovieResponse(this Movie movie)
  {
    return new MovieResponse()
        {
            Id = movie.Id,
            Title = movie.Title,
            Slug = movie.Slug,
            Year = movie.Year,
            Genre = movie.Genre
        };
  }

  
  public static MoviesResponse ToMoviesResponse(this IEnumerable<Movie> movies)
  {
    return new MoviesResponse()
        {
            Movies = movies.Select(ToMovieResponse)
        };
  
  }
}
