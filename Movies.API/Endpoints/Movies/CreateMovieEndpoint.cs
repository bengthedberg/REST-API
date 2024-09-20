using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.OutputCaching;
using Movies.API.Auth;
using Movies.API.Mapping;
using Movies.Application.Services;
using Movies.Contracts.Requests;
using Movies.Contracts.Responses;

namespace Movies.API.Endpoints.Movies;

public static class CreateMovieEndpoint
{
    public const string Name = "CreateMovie";

    public static IEndpointRouteBuilder MapCreateMovie(this IEndpointRouteBuilder app)
    {
        app.MapPost(APIEndpoints.Movies.Create,
            async (CreateMovieRequest request, IMovieService movieService,
                IOutputCacheStore outputCacheStore, CancellationToken token) =>
        {
            var movie = request.ToMovie();
            await movieService.CreateAsync(movie, token);
            await outputCacheStore.EvictByTagAsync("MovieCache", token);
            return TypedResults.CreatedAtRoute(movie.ToMovieResponse(), GetMovieEndpoint.Name, new { identity = movie.Id });
        })
        .WithName(Name)
        .RequireAuthorization(APIAuthorizationConstants.TrustedUserPolicyName)
        .Produces<MovieResponse>(StatusCodes.Status201Created)
        .Produces<ValidationFailureResponse>(StatusCodes.Status400BadRequest)
        .WithApiVersionSet(ApiVersioning.VersionSet!)
        .HasApiVersion(1.0);

        return app;
    }

}


/* 
    [HttpPost(APIEndpoints.Movies.Create)]
    [Authorize(APIAuthorizationConstants.TrustedUserPolicyName)]
    //[ServiceFilter(typeof(IAuthorizationFilter))]
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMovieRequest request, CancellationToken token)
    {
        var movie = request.ToMovie();
        await _movieService.CreateAsync(movie, token);
        await _outputCacheStore.EvictByTagAsync("MovieCache", token);
        return CreatedAtAction(nameof(Get), new { identity = movie.Id },
            movie.ToMovieResponse());
    }
*/