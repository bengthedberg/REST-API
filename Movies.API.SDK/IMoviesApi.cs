using Movies.Contracts.Requests;
using Movies.Contracts.Responses;
using Refit;

namespace Movies.API.SDK;

[Headers("Authorization: Bearer")]
public interface IMoviesApi
{
    [Get(APIEndpoints.Movies.Get)]
    Task<MovieResponse> GetMovieAsync(string identity);

    [Get(APIEndpoints.Movies.GetAll)]
    Task<MoviesResponse> GetMoviesAsync(GetAllMoviesRequest request);

    [Post(APIEndpoints.Movies.Create)]
    Task<MovieResponse> CreateMovieAsync(CreateMovieRequest request);
    
    [Put(APIEndpoints.Movies.Update)]
    Task<MovieResponse> UpdateMovieAsync(Guid id, UpdateMovieRequest request);
    
    [Delete(APIEndpoints.Movies.Delete)]
    Task DeleteMovieAsync(Guid id);

    [Put(APIEndpoints.Movies.Rating)]
    Task RateMovieAsync(Guid id, RateMovieRequest request);
    
    [Delete(APIEndpoints.Movies.DeleteRating)]
    Task DeleteRatingAsync(Guid id);

    [Get(APIEndpoints.Rating.GetUserRatings)]
    Task<IEnumerable<MovieRatingResponse>> GetUserRatingsAsync();

}
