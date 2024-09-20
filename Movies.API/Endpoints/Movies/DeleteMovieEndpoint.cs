using Microsoft.AspNetCore.OutputCaching;
using Movies.API.Auth;
using Movies.Application.Services;

namespace Movies.API.Endpoints.Movies;

public static class DeleteMovieEndpoint
{
    private const string Name = "DeleteMovie";

    public static IEndpointRouteBuilder MapDeleteMovie(this IEndpointRouteBuilder app)
    {

        app.MapDelete(APIEndpoints.Movies.Delete, async (Guid id, IMovieService movieService,
            IOutputCacheStore outputCacheStore, CancellationToken token) =>
        {
            var deleted = await movieService.DeleteAsync(id, token);
            if (!deleted)
            {
                return Results.NotFound();
            }
            await outputCacheStore.EvictByTagAsync("MovieCache", token);
            return Results.NoContent();
        })
        .WithName(Name)
        .RequireAuthorization(APIAuthorizationConstants.AdminUserPolicyName)
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .WithApiVersionSet(ApiVersioning.VersionSet!)
        .HasApiVersion(1.0);

        return app;
    }
}


/*
    [HttpDelete(APIEndpoints.Movies.Delete)]
    [Authorize(APIAuthorizationConstants.AdminUserPolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken token)
    {
        var deleted = await _movieService.DeleteAsync(id, token);
        if (!deleted)
        {
            return NotFound();
        }
        await _outputCacheStore.EvictByTagAsync("MovieCache", token);
        return NoContent();
    }
*/