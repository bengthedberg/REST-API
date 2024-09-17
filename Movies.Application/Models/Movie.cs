namespace Movies.Application.Models;

public class Movie
{
  public required Guid Id { get; init; }
  public required string Title { get; set; }
  public string Slug => GetSlug();
  public required int Year { get; set; }

  public int? UserRating { get; set; }
  public float? Rating { get; set; }

  public required List<string> Genre { get; init; } = new();

  private string GetSlug()
  {
    return Title.ToLower().Replace(" ", "-") + "-" + Year.ToString();
  }
}
