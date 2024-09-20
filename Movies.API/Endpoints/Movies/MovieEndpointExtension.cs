namespace Movies.API.Endpoints.Movies;

public static class MovieEndpointExtension
{
    public static IEndpointRouteBuilder MapMovieEndpoints(this IEndpointRouteBuilder app)
    {
        /*
        app.MapCreateMovie();
        app.MapGetMovie();
        app.MapGetAllMovies();
        app.MapUpdateMovie();
        app.MapDeleteMovie();
        */
        return app;
    }
}
