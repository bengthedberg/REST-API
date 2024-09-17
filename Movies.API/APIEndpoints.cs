namespace Movies.API;

public static class APIEndpoints
{
  private const string BaseURL = "/api";

  public static class Movies
  {
    private const string Base = $"{BaseURL}/movies";

    public const string Create = Base;
    public const string GetAll = Base;
    public const string Get = $"{Base}/{{identity}}";
    public const string Update = $"{Base}/{{id:guid}}";
    public const string Delete = $"{Base}/{{id:guid}}";
    public const string Rating = $"{Base}/{{id:guid}}/rating";
    public const string DeleteRating = $"{Base}/{{id:guid}}/rating";
  }

  public static class Rating
  {
    private const string Base = $"{BaseURL}/rating";
    public const string GetUserRatings = $"{Base}/me";
  }

}
