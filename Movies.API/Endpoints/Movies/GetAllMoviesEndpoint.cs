using Movies.API.Auth;
using Movies.API.Mapping;
using Movies.Application.Services;
using Movies.Contracts.Requests;
using Movies.Contracts.Responses;

namespace Movies.API.Endpoints.Movies;

public static class GetAllMoviesEndpoint
{
    public const string Name = "GetAllMovies";
    public static IEndpointRouteBuilder MapGetAllMovies(this IEndpointRouteBuilder app)
    {
        app.MapGet(APIEndpoints.Movies.GetAll,
            async ([AsParameters] GetAllMoviesRequest request, IMovieService movieService, HttpContext context, CancellationToken token) =>
        {
            var userId = context.GetUserId();
            var options = request.ToGetAllMoviesOptions().WithUserId(userId);
            var movies = await movieService.GetAllAsync(options, token);
            var count = await movieService.GetCountAsync(request.Title, request.Year, token);

            return TypedResults.Ok(movies.ToMoviesResponse(
                request.Page.GetValueOrDefault(PageRequest.DefaultPage),
                request.PageSize.GetValueOrDefault(PageRequest.DefaultPageSize),
                count));
        })
        .WithName(Name)
        .Produces<MoviesResponse>(StatusCodes.Status200OK)
        .WithApiVersionSet(ApiVersioning.VersionSet!)
        .HasApiVersion(1.0)
        .CacheOutput("MovieCache");

        return app;
    }
}


/*
    [HttpGet(APIEndpoints.Movies.GetAll)]
    [AllowAnonymous]
    [OutputCache(PolicyName = "MovieCache")]
    [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "title", "year", "sortBy", "page", "pageSize" }, VaryByHeader = "api-version, Accept-Encoding, Accept", Location = ResponseCacheLocation.Client)]
    [ProducesResponseType(typeof(MoviesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] GetAllMoviesRequest request, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var options = request.ToGetAllMoviesOptions().WithUserId(userId);
        var movies = await _movieService.GetAllAsync(options, token);
        var count = await _movieService.GetCountAsync(request.Title, request.Year, token);

        return Ok(movies.ToMoviesResponse(request.Page, request.PageSize, count));
    }
*/