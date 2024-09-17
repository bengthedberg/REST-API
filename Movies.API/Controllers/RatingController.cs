using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Movies.API.Auth;
using Movies.Application.Services;
using Movies.Contracts.Requests;

namespace Movies.API.Controllers;


[ApiController]
public class RatingController : ControllerBase
{
    private readonly IRatingService _ratingService;
    public RatingController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    [Authorize]
    [HttpPost(APIEndpoints.Movies.Rating)]
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


    [Authorize]
    [HttpDelete(APIEndpoints.Movies.DeleteRating)]
    public async Task<IActionResult> DeleteRating([FromRoute] Guid id, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var success = await _ratingService.DeleteRatingAsync(id, userId!.Value, token);
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }

}

