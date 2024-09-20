using Movies.API.Endpoints.Movies;
using Movies.API.Endpoints.Ratings;

namespace Movies.API.Endpoints;

public static class EndpointsExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMovieEndpoints();
        app.MapRatingEndpoints();
        return app;
    }
}
