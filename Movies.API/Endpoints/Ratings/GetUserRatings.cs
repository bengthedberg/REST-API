using Movies.API.Auth;
using Movies.API.Mapping;
using Movies.Application.Services;

namespace Movies.API.Endpoints.Ratings;

public static class GetUserRatings
{
    public const string Name = "GetUserRatings";

    public static IEndpointRouteBuilder MapGetUserRating(this IEndpointRouteBuilder app)
    {
        app.MapGet(APIEndpoints.Rating.GetUserRatings, async (Guid id, HttpContext context,
            IRatingService ratingService, CancellationToken token) =>
        {
            var userId = context.GetUserId();
            var result = await ratingService.GetRatingsByUserAsync(userId!.Value, token);
            return TypedResults.Ok(result.ToMovieRatingsResponse());
        })
        .WithName(Name)
        .RequireAuthorization()
        .Produces(StatusCodes.Status200OK)
        .WithApiVersionSet(ApiVersioning.VersionSet!)
        .HasApiVersion(1.0);

        return app;
    }
}

/*
    [Authorize]
    [HttpGet(APIEndpoints.Rating.GetUserRatings)]
    [ProducesResponseType(typeof(MovieRatingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRatings([FromRoute] Guid id, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var result = await _ratingService.GetRatingsByUserAsync(userId!.Value, token);
        return Ok(result.ToMovieRatingsResponse());
    }
*/