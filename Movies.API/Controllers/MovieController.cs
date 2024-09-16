using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> Create([FromBody] CreateMovieRequest request)
    {
        var movie = request.ToMovie();
        await _movieService.CreateAsync(movie);
        return CreatedAtAction(nameof(Get), new { identity = movie.Id },
            movie.ToMovieResponse());
    }

    [HttpGet(APIEndpoints.Movies.Get)]
    public async Task<IActionResult> Get([FromRoute] string identity)
    {
        var movie = Guid.TryParse(identity, out var id)
            ? await _movieService.GetByIdAsync(id)
            : await _movieService.GetBySlugAsync(identity);
        if (movie is null)
        {
            return NotFound();
        }
        return Ok(movie.ToMovieResponse());
    }

    [HttpGet(APIEndpoints.Movies.GetAll)]
    public async Task<IActionResult> GetAll()
    {
        var movies = await _movieService.GetAllAsync();
        return Ok(movies.ToMoviesResponse());
    }

    [HttpPut(APIEndpoints.Movies.Update)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMovieRequest request)
    {
        var movie = request.ToMovie(id);
        var updateMovie = await _movieService.UpdateAsync(movie);
        if (updateMovie is null)
        {
            return NotFound();
        }
        return Ok(updateMovie.ToMovieResponse());
    }

    [HttpDelete(APIEndpoints.Movies.Delete)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var deleted = await _movieService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}
