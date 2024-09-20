using Movies.API.Auth;
using Movies.Application.Services;

namespace Movies.API.Endpoints.Ratings;

public static class DeleteUserRating
{
    public const string Name = "DeleteUserRating";
    public static IEndpointRouteBuilder MapDeleteRating(this IEndpointRouteBuilder app)
    {
        app.MapDelete(APIEndpoints.Movies.DeleteRating, async (Guid id, HttpContext context,
            IRatingService ratingService, CancellationToken token) =>
        {
            var userId = context.GetUserId();
            var success = await ratingService.DeleteRatingAsync(id, userId!.Value, token);
            if (!success)
            {
                return Results.NotFound();
            }
            return Results.NoContent();
        })
        .WithName(Name)
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .WithApiVersionSet(ApiVersioning.VersionSet!)
        .HasApiVersion(1.0);

        return app;
    }
}

/*
    [Authorize]
    [HttpDelete(APIEndpoints.Movies.DeleteRating)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRating([FromRoute] Guid id, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var success = await _ratingService.DeleteRatingAsync(id, userId!.Value, token);
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }
*/