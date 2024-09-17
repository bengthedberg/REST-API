namespace Movies.Contracts.Responses;

public class MovieRatingsResponse
{
    public IEnumerable<MovieRatingResponse> MovieRatings { get; init; } = Enumerable.Empty<MovieRatingResponse>();
}

