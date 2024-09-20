using Movies.API.Auth;
using Movies.API.Mapping;
using Movies.Application.Services;
using Movies.Contracts.Responses;

namespace Movies.API.Endpoints.Movies;

public static class GetMovieEndpoint
{
    public const string Name = "GetMovie";
    public static IEndpointRouteBuilder MapGetMovie(this IEndpointRouteBuilder app)
    {
        app.MapGet(APIEndpoints.Movies.Get,
            async (string identity, IMovieService movieService, HttpContext context, CancellationToken token) =>
        {
            var userId = context.GetUserId();
            var movie = Guid.TryParse(identity, out var id)
                ? await movieService.GetByIdAsync(id, userId, token)
                : await movieService.GetBySlugAsync(identity, userId, token);
            if (movie is null)
            {
                return Results.NotFound();
            }
            return TypedResults.Ok(movie.ToMovieResponse());
        })
        .WithName(Name)
        .Produces<MovieResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithApiVersionSet(ApiVersioning.VersionSet!)
        .HasApiVersion(1.0)
        .CacheOutput("MovieCache");

        return app;
    }
}

/*
    [HttpGet(APIEndpoints.Movies.Get)]
    [AllowAnonymous]
    [OutputCache(PolicyName = "MovieCache")]
    [ResponseCache(Duration = 60, VaryByHeader = "api-version, Accept-Encoding, Accept", Location = ResponseCacheLocation.Client)]
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string identity, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var movie = Guid.TryParse(identity, out var id)
            ? await _movieService.GetByIdAsync(id, userId, token)
            : await _movieService.GetBySlugAsync(identity, userId, token);
        if (movie is null)
        {
            return NotFound();
        }
        return Ok(movie.ToMovieResponse());
    }
*/