using Movies.API.Auth;
using Movies.Application.Services;
using Movies.Contracts.Requests;
using Movies.Contracts.Responses;

namespace Movies.API.Endpoints.Ratings;

public static class CreateUserRating
{
    public const string Name = "CreateUserRating";

    public static IEndpointRouteBuilder MapRateMovie(this IEndpointRouteBuilder app)
    {
        app.MapPost(APIEndpoints.Movies.Rating, async (Guid id, RateMovieRequest request,
            HttpContext context, IRatingService ratingService, CancellationToken token) =>
        {
            var userId = context.GetUserId();
            var success = await ratingService.RateMovieAsync(id, userId!.Value, request.Rating, token);
            if (!success)
            {
                return Results.NotFound();
            }
            return Results.Ok();
        })
        .WithName(Name)
        .RequireAuthorization()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithApiVersionSet(ApiVersioning.VersionSet!)
        .HasApiVersion(1.0);

        return app;
    }

}

/*
    [Authorize]
    [HttpPost(APIEndpoints.Movies.Rating)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rating([FromRoute] Guid id, [FromBody] RateMovieRequest request, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var success = await _ratingService.RateMovieAsync(id, userId!.Value, request.Rating, token);
        if (!success)
        {
            return NotFound();
        }
        return Ok();
    }
*/