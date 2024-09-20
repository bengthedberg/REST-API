namespace Movies.API.Endpoints.Ratings;

public static class RatingEndpointExtensions
{
    public static IEndpointRouteBuilder MapRatingEndpoints(this IEndpointRouteBuilder app)
    {

        app.MapRateMovie();
        app.MapDeleteRating();
        app.MapGetUserRating();

        return app;
    }
}
