using Microsoft.AspNetCore.OutputCaching;
using Movies.API.Auth;
using Movies.API.Mapping;
using Movies.Application.Services;
using Movies.Contracts.Requests;
using Movies.Contracts.Responses;

namespace Movies.API.Endpoints.Movies;

public static class UpdateMovieEndpoint
{
    public const string Name = "UpdateMovie";
    public static IEndpointRouteBuilder MapUpdateMovie(this IEndpointRouteBuilder app)
    {
        app.MapPut(APIEndpoints.Movies.Update,
            async (Guid id, UpdateMovieRequest request, IMovieService movieService,
                IOutputCacheStore outputCacheStore, HttpContext context, CancellationToken token) =>
        {
            var userId = context.GetUserId();
            var movie = request.ToMovie(id);
            var updateMovie = await movieService.UpdateAsync(movie, userId, token);
            if (updateMovie is null)
            {
                return Results.NotFound();
            }
            await outputCacheStore.EvictByTagAsync("MovieCache", token);
            return TypedResults.Ok(updateMovie.ToMovieResponse());
        })
        .WithName(Name)
        .RequireAuthorization(APIAuthorizationConstants.TrustedUserPolicyName)
        .Produces<MovieResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ValidationFailureResponse>(StatusCodes.Status400BadRequest)
        .WithApiVersionSet(ApiVersioning.VersionSet!)
        .HasApiVersion(1.0);

        return app;
    }
}


/*
    [HttpPut(APIEndpoints.Movies.Update)]
    [Authorize(APIAuthorizationConstants.TrustedUserPolicyName)]
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMovieRequest request, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var movie = request.ToMovie(id);
        var updateMovie = await _movieService.UpdateAsync(movie, userId, token);
        if (updateMovie is null)
        {
            return NotFound();
        }
        await _outputCacheStore.EvictByTagAsync("MovieCache", token);
        return Ok(updateMovie.ToMovieResponse());
    }
*/