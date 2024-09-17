using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Movies.API.Auth;
using Movies.API.Mapping;
using Movies.Application.Services;
using Movies.Contracts.Requests;

namespace Movies.API.Controllers;

[ApiController]
public class MovieController : ControllerBase
{
    private readonly IMovieService _movieService;
    public MovieController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpPost(APIEndpoints.Movies.Create)]
    [Authorize(APIAuthorizationConstants.TrustedUserPolicyName)]
    public async Task<IActionResult> Create([FromBody] CreateMovieRequest request, CancellationToken token)
    {
        var movie = request.ToMovie();
        await _movieService.CreateAsync(movie, token);
        return CreatedAtAction(nameof(Get), new { identity = movie.Id },
            movie.ToMovieResponse());
    }

    [HttpGet(APIEndpoints.Movies.Get)]
    [AllowAnonymous]
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
    public async Task<IActionResult> GetAll(CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var movies = await _movieService.GetAllAsync(userId, token);
        return Ok(movies.ToMoviesResponse());
    }

    [HttpPut(APIEndpoints.Movies.Update)]
    [Authorize(APIAuthorizationConstants.TrustedUserPolicyName)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMovieRequest request, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var movie = request.ToMovie(id);
        var updateMovie = await _movieService.UpdateAsync(movie, userId, token);
        if (updateMovie is null)
        {
            return NotFound();
        }
        return Ok(updateMovie.ToMovieResponse());
    }

    [HttpDelete(APIEndpoints.Movies.Delete)]
    [Authorize(APIAuthorizationConstants.AdminUserPolicyName)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken token)
    {
        var deleted = await _movieService.DeleteAsync(id, token);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}
