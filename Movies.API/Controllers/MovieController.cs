using Microsoft.AspNetCore.Mvc;
using Movies.API.Mapping;
using Movies.Application.Repositories;
using Movies.Contracts.Requests;

namespace Movies.API.Controllers;

[ApiController]
public class MovieController : ControllerBase
{
    private readonly IMovieRepository _movieRepository;
    public MovieController(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    [HttpPost(APIEndpoints.Movies.Create)]
    public async Task<IActionResult> Create([FromBody]CreateMovieRequest request)
    {
        var movie = request.ToMovie();
        await _movieRepository.CreateAsync(movie);
        return CreatedAtAction(nameof(Get), new { identity = movie.Id },
            movie.ToMovieResponse());
    }        

    [HttpGet(APIEndpoints.Movies.Get)]
    public async Task<IActionResult> Get([FromRoute] string identity)
    {
        var movie = Guid.TryParse(identity, out var id) 
            ? await _movieRepository.GetByIdAsync(id)
            : await _movieRepository.GetBySlugAsync(identity);
        if (movie is null)
        {
            return NotFound();
        }
        return Ok(movie.ToMovieResponse());
    }

    [HttpGet(APIEndpoints.Movies.GetAll)]
    public async Task<IActionResult> GetAll()
    {
        var movies = await _movieRepository.GetAllAsync();
        return Ok(movies.ToMoviesResponse());
    }

    [HttpPut(APIEndpoints.Movies.Update)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMovieRequest request)
    {
        var movie = request.ToMovie(id);
        var updated = await _movieRepository.UpdateAsync(movie);
        if (!updated)
        {
            return NotFound();
        }
        return Ok(movie.ToMovieResponse());
    }

    [HttpDelete(APIEndpoints.Movies.Delete)]
    public async Task<IActionResult> Delete([FromRoute]Guid id)
    {
        var deleted = await _movieRepository.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}
