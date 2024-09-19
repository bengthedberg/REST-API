namespace Movies.API.SDK;

public static class APIEndpoints
{
  private const string BaseURL = "/api";

  public static class Movies
  {
    private const string Base = $"{BaseURL}/movies";

    public const string Create = Base;
    public const string GetAll = Base;
    public const string Get = $"{Base}/{{identity}}";
    public const string Update = $"{Base}/{{id}}";
    public const string Delete = $"{Base}/{{id}}";
    public const string Rating = $"{Base}/{{id}}/rating";
    public const string DeleteRating = $"{Base}/{{id}}/rating";
  }

  public static class Rating
  {
    private const string Base = $"{BaseURL}/rating";
    public const string GetUserRatings = $"{Base}/me";
  }

}
