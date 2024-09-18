using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Movies.API.Auth;
using Movies.API.Mapping;
using Movies.Application.Services;
using Movies.Contracts.Requests;
using Movies.Contracts.Responses;

namespace Movies.API.Controllers;

[ApiVersion(1.0)]
[ApiController]
public class MovieController : ControllerBase
{
    private readonly IMovieService _movieService;
    private readonly IOutputCacheStore _outputCacheStore;
    public MovieController(IMovieService movieService, IOutputCacheStore outputCacheStore)
    {
        _movieService = movieService;
        _outputCacheStore = outputCacheStore;
    }

    [HttpPost(APIEndpoints.Movies.Create)]
    [Authorize(APIAuthorizationConstants.TrustedUserPolicyName)]
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

    [HttpGet(APIEndpoints.Movies.GetAll)]
    [AllowAnonymous]
    [OutputCache(PolicyName = "MovieCache")]
    [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "title", "year", "sortBy", "page", "pageSize" }, VaryByHeader = "api-version, Accept-Encoding, Accept", Location = ResponseCacheLocation.Client)]
    [ProducesResponseType(typeof(MoviesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] GetAllMoviesRequest request, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var options = request.ToGetAllMoviesOptions().WithUserId(userId);
        var movies = await _movieService.GetAllAsync(options, token);
        var count = await _movieService.GetCountAsync(request.Title, request.Year, token);

        return Ok(movies.ToMoviesResponse(request.Page, request.PageSize, count));
    }

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
}
