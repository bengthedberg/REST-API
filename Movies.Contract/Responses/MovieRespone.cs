namespace Movies.Contracts.Responses;

public class MovieResponse
{
  public required Guid Id { get; init; }
  public required string Title { get; init; }
  public required string Slug { get; init; }
  public required int Year { get; init; }
  public required IEnumerable<string> Genre { get; init; } = Enumerable.Empty<string>();    

}
