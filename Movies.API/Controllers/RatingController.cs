using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Movies.API.Auth;
using Movies.API.Mapping;
using Movies.Application.Services;
using Movies.Contracts.Requests;
using Movies.Contracts.Responses;

namespace Movies.API.Controllers;

[ApiVersion(1.0)]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    [Authorize]
    [HttpGet(APIEndpoints.Rating.GetUserRatings)]
    [ProducesResponseType(typeof(MovieRatingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRatings([FromRoute] Guid id, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var result = await _ratingService.GetRatingsByUserAsync(userId!.Value, token);
        return Ok(result.ToMovieRatingsResponse());
    }
}

