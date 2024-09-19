using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Movies.API.SDK;
using Movies.API.SDK.Consumer;
using Movies.Contracts.Requests;
using Refit;

// Setup the DI container

// var moviesApi = RestService.For<IMoviesApi>("http://localhost:5181");

var services = new ServiceCollection();

services
    .AddHttpClient()
    .AddSingleton<AuthTokenProvider>()
    .AddRefitClient<IMoviesApi>(s => new RefitSettings
    {
        AuthorizationHeaderValueGetter = async (rq, ct) => await s.GetRequiredService<AuthTokenProvider>().GetTokenAsync()
    })
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5181"));

var provider = services.BuildServiceProvider();


// Call the API

var moviesApi = provider.GetRequiredService<IMoviesApi>();

try {
    var movie = await moviesApi.GetMovieAsync("inception-prep-2021");
    Console.WriteLine(JsonSerializer.Serialize(movie, new JsonSerializerOptions { WriteIndented = true }));
} catch (ApiException ex) {
    Console.WriteLine("Error GetMovieAsync: " + ex.Message);
    Console.WriteLine(ex.Content);
}

try {
    var movies = await moviesApi.GetMoviesAsync(new GetAllMoviesRequest()
    {
        Title = null,
        Year = null,
        SortBy = null,
        Page = 1,
        PageSize = 10
    });
    Console.WriteLine(JsonSerializer.Serialize(movies, new JsonSerializerOptions { WriteIndented = true }));
} catch (ApiException ex) {
    Console.WriteLine("Error GetMoviesAsync: " + ex.Message);
    Console.WriteLine(ex.Content);
}

Guid newMovieId = Guid.Empty;
try {
    var newMovie = await moviesApi.CreateMovieAsync(new CreateMovieRequest
        {
            Title = "Spiderman 2",
            Year = 2002,
            Genre = new []{ "Action"}
        });
    newMovieId = newMovie.Id;
    Console.WriteLine(JsonSerializer.Serialize(newMovie, new JsonSerializerOptions { WriteIndented = true }));
}   
catch (ApiException ex) {
    Console.WriteLine("Error CreateMovieAsync: " + ex.Message);
    Console.WriteLine(ex.Content);
}

try {
    await moviesApi.UpdateMovieAsync(newMovieId, new UpdateMovieRequest()
    {
        Title = "Spiderman 2",
        Year = 2002,
        Genre = new []{ "Action", "Adventure"}
    });
    Console.WriteLine("Movie updated");
    var updatedMovie = await moviesApi.GetMovieAsync(newMovieId.ToString());
    Console.WriteLine(JsonSerializer.Serialize(updatedMovie, new JsonSerializerOptions { WriteIndented = true }));
}
catch (ApiException ex)
{
    Console.WriteLine("Error UpdateMovieAsync: " + ex.Message);
    Console.WriteLine(ex.Content);
}   

try {
    await moviesApi.DeleteMovieAsync(newMovieId);
    Console.WriteLine("Movie deleted");
}
catch (ApiException ex)
{
    Console.WriteLine("Error DeleteMovieAsync: " + ex.Message);
    Console.WriteLine(ex.Content);
}

Console.WriteLine("Done");