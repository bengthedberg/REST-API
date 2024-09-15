# REST API 

An implementation of a REST API, step by step instructions.


### Create Solution 

- API project with the defined endpoints as well as common API logic such as caching, pagination, etc.
- Application library for business logic
- Contract library defining the comntracts exposed by the API. This can packaged as a nuget and consumed by future clients.
  

```
git init 
dotnet new gitignore

dotnet new sln -n Movies

dotnet new webapi -n Movies.API -o Movies.API -controllers
dotnet new classlib -n Movies.Application -o Movies.Application
dotnet new classlib -n Movies.Contracts -o Movies.Contract

dotnet sln add **/*.csproj
```

The API project will reference the Application library and the Contract library.
```
dotnet add ./Movies.API/Movies.API.csproj reference ./Movies.Contract/Movies.Contracts.csproj
dotnet add ./Movies.API/Movies.API.csproj reference ./Movies.Application/Movies.Application.csproj
```

#### Cleanup

Delete the default `weather` controller and model in the `API` project.
Delete the default `Class.cs` files in the `Contracts` and `Application` projects.

#### Commit

```
git add .
git commit -m 'initial, empty project'
```


### Add Contracts

The contract class will be exposed to any clients of this API. It contains the requests and responses this API uses. 

These requests/responses will be accesible using a nuget package. 

Add the following files in the `Contracts` project:

-  `Requests\CreateMovieRequest.cs`
    ```csharp
    namespace Movies.Contracts.Requests;

    public class CreateMovieRequest
    {
      public required string Title { get; init; }
      public required int Year { get; init; }
      public required IEnumerable<string> Genre { get; init; } = Enumerable.Empty<string>();    
    }
    ```
-  `Requests\UpdateMovieRequest.cs`
    ```csharp
    namespace Movies.Contracts.Requests;

    public class UpdateMovieRequest
    {
      public required string Title { get; init; }
      public required int Year { get; init; }
      public required IEnumerable<string> Genre { get; init; } = Enumerable.Empty<string>();    
    }
    ```
-  `Response\MovieResponse.cs`
    ```csharp
    namespace Movies.Contracts.Responses;

    public class CreateMovieResponse
    {
      public required Guid Id { get; init; }
      public required string Title { get; init; }
      public required int Year { get; init; }
      public required IEnumerable<string> Genre { get; init; } = Enumerable.Empty<string>();    

    }
    ```
-  `Response\MoviesResponse.cs`
    ```csharp
    namespace Movies.Contracts.Responses;

    public class MoviesResponse
    {
      public IEnumerable<MovieResponse> Movies { get; init; } = Enumerable.Empty<MovieResponse>();
    }
    ```    


#### Commit

```
git add .
git commit -m 'add create movie request/response'
```

### Create a Database Repository

A repository is responsible for creating a database connection to a database server and 
connecting to the database server using the provided parameters specified.

We will not be using Entity Framework, instead we will use the Dapper ORM framework. Though you could replace it with a NoSQL database if you want to, or anything else for that matter.

If we were using Entity Framework then we would not require a repository as that is provided within the framwork.

Initially we will store the data in an internal list.

Add a model for the movies:

- `Models\Movie.cs`
  ```csharp
  namespace Movies.Application.Models;

  public class Movie
  {
    public required Guid Id { get; init; } // Required and immutable
    public required string Title { get; set; } // Required and mutable
    public required int Year { get; set; } // Required and mutable
    public required List<string> Genre { get; init; } = new(); // Required, list itself is immutable but items can be modified   
  }
  ```

Add a repository for persisting the data by adding the following files in the `Application` project:

- `Repositories\IMovieRepository.cs`
  ```csharp
  using Movies.Application.Models;
  namespace Movies.Application.Repositories;

  public interface IMovieRepository
  {
    Task<Movie?> GetByIdAsync(Guid id); // Nullable if id does not exist
    Task<IEnumerable<Movie>> GetAllAsync(); 
    Task<bool> CreateAsync(Movie movie); // true if movie was created successfully
    Task<bool> UpdateAsync(Movie movie); // true if movie was updated successfully
    Task<bool> DeleteAsync(Guid id); // true if movie was deleted successfully    
  }
  ```

- `Repositories\MovieRepository.cs`
  ```csharp
  using Movies.Application.Models;

  namespace Movies.Application.Repositories;

  public class MovieRepository : IMovieRepository
  {
      // Implement an in-memory representation of the movie resource.
      // Will be replaced with an actual database later.
      private readonly List<Movie> _movies = new();

      public async Task<bool> CreateAsync(Movie movie)
      {
          _movies.Add(movie);
          return await Task.FromResult(true);
      }

      public async Task<bool> DeleteAsync(Guid id)
      {
          var records = await Task.FromResult(_movies.RemoveAll(m => m.Id == id));
          return records > 0; 
      }

      public async Task<IEnumerable<Movie>> GetAllAsync()
      {
          return await Task.FromResult(_movies.AsEnumerable());
      }

      public async Task<Movie?> GetByIdAsync(Guid id)
      {
          return await Task.FromResult(_movies.SingleOrDefault(m => m.Id == id));
      }

      public async Task<bool> UpdateAsync(Movie movie)
      {
          var index = await Task.FromResult(_movies.FindIndex(m => m.Id == movie.Id));
          if (index != -1)
          {
              _movies[index] = movie;
              return true;
          }
          return false;
  }
  ```

Add a Service Extension helper class to help setup required dependencies :

- `ServiceExtension.cs`
  ```csharp
  using Microsoft.Extensions.DependencyInjection;
  using Movies.Application.Repositories;

  public static class ServiceExtension
  {
      public static IServiceCollection AddApplication(this IServiceCollection services)
      {
          services.AddSingleton<IMovieRepository, MovieRepository>();
          return services;
      }
  }
  ```

Add required nuget package:
`dotnet add ./Movies.Application/Movies.Application.csproj package Microsoft.Extensions.DependencyInjection.Abstractions`

Use the service extension in the API project by adding the line
  ```csharp
  builder.Services.AddApplication();
  ```
to `Program.cs`

#### Commit

```
git add .
git commit -m 'add create movie model and repository'
```

### Create Movie Controller

Create a single instance of all routes in the API. 

- `APIEndpoints.cs`
  ```csharp
  namespace Movies.API;

  public static class APIEndpoints
  {
    private const string _baseURL = "/api"; 

    public static class Movies 
    {
      private const string _base = $"{_baseURL}/movies";

      public const string Create = _base;
      public const string GetAll = _base;
      public const string Get = $"{_base}/{{id:guid}}";
      public const string Update = $"{_base}/{{id:guid}}";
      public const string Delete = $"{_base}/{{id:guid}}";
    }
  }
  ```

Lets implement some mapping between the models and the requests/responses.

> These mappers are supposed to be very simple. Do not implement any business logic here.

- `Mapping\ContractMapping.cs`
  ```csharp
  using Movies.Application.Models;
  using Movies.Contracts.Requests;
  using Movies.Contracts.Responses;

  namespace Movies.API.Mapping;

  public static class ContractMapping
  {
    public static Movie ToMovie(this CreateMovieRequest request)
    {
      return new Movie()
          {
              Id = Guid.NewGuid(),
              Title = request.Title,
              Year = request.Year,
              Genre = request.Genre.ToList()
          };
    }

    public static Movie ToMovie(this UpdateMovieRequest request, Guid id)
    {
      return new Movie()
          {
              Id = id,
              Title = request.Title,
              Year = request.Year,
              Genre = request.Genre.ToList()
          };
    }

    public static MovieResponse ToMovieResponse(this Movie movie)
    {
      return new MovieResponse()
          {
              Id = movie.Id,
              Title = movie.Title,
              Year = movie.Year,
              Genre = movie.Genre
          };
    }

    public static MoviesResponse ToMoviesResponse(this IEnumerable<Movie> movies)
    {
      return new MoviesResponse()
          {
              Movies = movies.Select(ToMovieResponse)
          };
    
    }
  }
  ```

Create a new movie controller that will action the specified endpoints:

- `Controllers\MovieController.cs`
  ```csharp
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
          return CreatedAtAction(nameof(Get), new { id = movie.Id },
              movie.ToMovieResponse());
      }        

      [HttpGet(APIEndpoints.Movies.Get)]
      public async Task<IActionResult> Get([FromRoute] Guid id)
      {
          var movie = await _movieRepository.GetByIdAsync(id);
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

  ```

Add a `http` file to test the endpoints:

- `Movies.API.http`
  ```http
   @baseURL = http://localhost:5181

  ### Create a new movie
  # @name createMovie
  POST {{baseURL}}/api/movies
  Content-Type: application/json
  Accept: application/json

  {
    "Title": "Inception",
    "Year": 2010,
    "Genre": [
      "Action",
      "Adventure",
      "Cinema",
      "Thriller"
    ]
  }

  ### Save location information
  @location = {{createMovie.response.headers.Location}}

  ### Get movie using location information
  # @name getMovie
  GET {{location}}
  Content-Type: application/json
  Accept: application/json

  ### Save the movie id
  @movieId = {{getMovie.response.body.id}}

  ### Update a Movie using id
  PUT {{baseURL}}/api/movies/{{movieId}}
  Content-Type: application/json
  Accept: application/json

  {
    "Title": "Inception",
    "Year": 2011,
    "Genre": [
      "Action",
      "Adventure",
      "Cinema",
      "Thriller",
      "Sci-fi"
    ]
  }

  ### Get All Movies
  GET {{baseURL}}/api/movies
  Content-Type: application/json
  Accept: application/json

  ### Delete a Movie using id
  DELETE {{baseURL}}/api/movies/{{movieId}}
  Content-Type: application/json
  Accept: application/json
  ```

#### Commit

```
git add .
git commit -m 'add movie controller with create and get endpoints'
```  

### Add a slug

A slug is a human-readable, unique identifier, used to identify a resource instead of a less human-readable identifier like an id. 

You use a slug when you want to refer to an item while preserving the ability to see, at a glance, what the item is.

In our case the slug will be the title + the year.

1. Update the contracts

   - `Contracts\Responses\MovieResponse.cs`
     ```csharp
      namespace Movies.Contracts.Responses;

      public class MovieResponse
      {
        public required Guid Id { get; init; }
        public required string Title { get; init; }
        public required string Slug { get; init; }
        public required int Year { get; init; }
        public required IEnumerable<string> Genre { get; init; } = Enumerable.Empty<string>();    

      }
     ```

2. Update the Model, the `Slug` is a computed field.

     - `Models\Movie.cs`
       ```csharp
         namespace Movies.Application.Models;

         public class Movie
         {
           public required Guid Id { get; init; }
           public required string Title { get; set; }
           public string Slug => GetSlug();  
           public required int Year { get; set; }
           public required List<string> Genre { get; init; } = new();    

           private string GetSlug()
           {
             return Title.ToLower().Replace(" ", "-") + "-" + Year.ToString();
           }
         }

       ```

3. Update the Mappings

     - `Mapping\ContractMapping.cs` method `ToMovieResponse`
       ```csharp 
           public static MovieResponse ToMovieResponse(this Movie movie)
           {
             return new MovieResponse()
                 {
                     Id = movie.Id,
                     Title = movie.Title,
                     Slug = movie.Slug,
                     Year = movie.Year,
                     Genre = movie.Genre
                 };
           }
       ```

4. Update the endpoints for `Get` 

    - `APIEndpoint.cs`
      ```csharp
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
        }
      }
      ```
5. Update the Repository
   
    - Add 
      ```csharp
      Task<Movie?> GetBySlugAsync(string id); 
      ```
      to `IMovieRepository.cs`

    - Implement it in `MovieRepository.cs`
      ```csharp
      public async Task<Movie?> GetBySlugAsync(string id)
      {
          return await Task.FromResult(_movies.SingleOrDefault(m => m.Slug == id));
      }
      ```

6. Update the Controller

   - `Controllers\MoviesController.cs`
     ```csharp
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

      [HttpPost(APIEndpoints.Movies.Create)]
      public async Task<IActionResult> Create([FromBody]CreateMovieRequest request)
      {
          var movie = request.ToMovie();
          await _movieRepository.CreateAsync(movie);
          return CreatedAtAction(nameof(Get), new { identity = movie.Id },
              movie.ToMovieResponse());
      }        
      ```
   
1. Update the `HTTP` file

    - Add the following
      ```http
      ### Get Movie using slug
      Get {{baseURL}}/api/movies/inception-2011
      Content-Type: application/json
      Accept: application/json
      ```   

### Add a Database for local development

Docker will be used for local development. This will create a local PostgreSQL database instance.

Create a `docker-compose.yml` file:

```yml
version: "3.8"
services:
  db:
    image: postgres
    environment:
      POSTGRES_USER: demo
      POSTGRES_PASSWORD: demo
      POSTGRES_DB: movies
    ports:
      - 5432:5432
    volumes:
      - ./data:/var/lib/postgresql/data
```

You can start the database with the command:

`docker-compose up -d`

### Refactor Application to use PostgeSQL database

#### Install dependencies

Dapper will be used as a ORM for the application. 

Add the following nuget packages:

```
dotnet add ./Movies.Application/Movies.Application.csproj package Dapper
dotnet add ./Movies.Application/Movies.Application.csproj package Npgsq
```

#### Add Database Scaffolds to Application

1. Add `Database\IDatabaaseConnectionFactory.cs` 
   ```csharp
    using System.Data;

    namespace Movies.Application.Database;

    public interface IDatabaseConnectionFactory
    { 
        Task<IDbConnection> CreateConnectionAsync();
    }
   ```

2. Implement `Database\IDatabaseConnectionFactory` with the `Database\NpgSqlConnectionFactory.cs`   
   ```csharp
    using System.Data;
    using Npgsql;

    namespace Movies.Application.Database;

    public class NpgSqlConnectionFactory : IDatabaseConnectionFactory
    {
        private readonly string _connectionString;

        public NpgSqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IDbConnection> CreateConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
   ```

3. Create a Database Migration

    Implement a simple database migration using the following class 

    `Database\DatabaseMigration.cs`
    ```csharp
    using Dapper;
    namespace Movies.Application.Database;

    public class DatabaseMigration
    {

      private readonly IDatabaseConnectionFactory _connectionFactory; 

      public DatabaseMigration(IDatabaseConnectionFactory connectionFactory)
      {
          _connectionFactory = connectionFactory;
      }

      public async Task Migrate()
      {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(
        """
        CREATE TABLE IF NOT EXISTS movies (
            id UUID PRIMARY KEY,
            title TEXT NOT NULL,
            slug TEXT NOT NULL,
            year INTEGER NOT NULL
        );
        """);
      await connection.ExecuteAsync(
        """
        CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS idx_movies_slug ON movies USING btree(slug);
        """);

      await connection.ExecuteAsync(
        """
        CREATE TABLE IF NOT EXISTS genres (
            movieId UUID REFERENCES movies(id) ON DELETE CASCADE,
            name TEXT NOT NULL,
            PRIMARY KEY(movieId, name)
        );
        """);
      }    

    }

    ```

4. Register the connection factory and migrate classes

    Add the connection factory and migration methods to the `ServiceExtension` class
    ```csharp
    using Microsoft.Extensions.DependencyInjection;
    using Movies.Application.Database;
    using Movies.Application.Repositories;

    public static class ServiceExtension
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<IMovieRepository, MovieRepository>();
            return services;
        }

        public static IServiceCollection AddDatabases(this IServiceCollection services, string connectionString)
        {
            // Note The factory is a singleton instance, but the CreateConnectionAsync method will create a new 
            // connection for each request.
            services.AddSingleton<IDatabaseConnectionFactory>(_ => new NpgSqlConnectionFactory(connectionString));
            services.AddSingleton<DatabaseMigration>();
            return services;
        }
    }
    ```

5. Setup the startup and migration logic:

    Use the `ServiceExtension` in the startup process, by adding the following to `Program.cs`:
    ```csharp
    using Movies.Application.Database;

    var builder = WebApplication.CreateBuilder(args);
    var config = builder.Configuration;
    // Add services to the container.

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddApplication();
    builder.Services.AddDatabases(config["Database:ConnectionString"]!);

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    var dbMigration = app.Services.GetRequiredService<DatabaseMigration>();
    await dbMigration.Migrate();

    app.Run();

    ```

    Add the connection string to `appsettings.json`
    ```json
    {
      "Database" : {
        "ConnectionString": "Server=localhost;Port=5432;Database=movies;User Id=demo;Password=demo;"
      },
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
      "AllowedHosts": "*"
    }
    ```

### Replace In-Memory Database

1. Add a `ExistsByIdAsync` method to to the `Repositories\IMovieRepository.cs`
  ```csharp
  using Movies.Application.Models;

  namespace Movies.Application.Repositories;

  public interface IMovieRepository
  {
    Task<Movie?> GetByIdAsync(Guid id); 
    Task<Movie?> GetBySlugAsync(string id); 
    Task<IEnumerable<Movie>> GetAllAsync(); 
    Task<bool> CreateAsync(Movie movie); // true if movie was created successfully
    Task<bool> UpdateAsync(Movie movie); // true if movie was updated successfully
    Task<bool> DeleteAsync(Guid id); // true if movie was deleted successfully
    Task<bool> ExistByIdAsync(Guid id);
  }
  ```

2. Replace the `Repositories\MovieRepository.cs` class with this:
  ```csharp
  using Dapper;
  using Movies.Application.Database;
  using Movies.Application.Models;

  namespace Movies.Application.Repositories;

  public class MovieRepository : IMovieRepository
  {
      private readonly IDatabaseConnectionFactory _connectionFactoryConnection;

      public MovieRepository(IDatabaseConnectionFactory databaseConnectionFactory)
      {
          _connectionFactoryConnection = databaseConnectionFactory;
      }

      public async Task<bool> CreateAsync(Movie movie)
      {
          using var connection = await _connectionFactoryConnection.CreateConnectionAsync();
          // Use a transaction as we updates multiple tables in the database
          using var transaction = connection.BeginTransaction();

          var result = await connection.ExecuteAsync( new CommandDefinition("""
              INSERT INTO movies (id, title, slug, year) VALUES (@Id, @Title, @Slug, @Year)
              """, movie));
          if (result > 0)
          {
              foreach (var genre in movie.Genre)
              {
                  result = await connection.ExecuteAsync( new CommandDefinition("""
                      INSERT INTO genres (movieId, name) VALUES (@MovieId, @Name)
                      """, new { MovieId = movie.Id, Name = genre }));
              }
          }    

          transaction.Commit();
          return result > 0;
      }

      public async Task<bool> DeleteAsync(Guid id)
      {
          using var connection = await _connectionFactoryConnection.CreateConnectionAsync();
          var result = await connection.ExecuteAsync( new CommandDefinition("""
              DELETE FROM movies WHERE id = @Id
              """, new { Id = id }));
          return result > 0;
      }

      public async Task<bool> ExistByIdAsync(Guid id)
      {
          using var connection = await _connectionFactoryConnection.CreateConnectionAsync();
          var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
              SELECT COUNT(*) FROM movies WHERE id = @Id
              """, new { Id = id }));
          return exists;
      }

      public async Task<IEnumerable<Movie>> GetAllAsync()
      {
          using var connection = await _connectionFactoryConnection.CreateConnectionAsync();
          var result = await connection.QueryAsync(new CommandDefinition("""
              SELECT m.*, string_agg(g.name, ',') as genres 
                FROM movies m LEFT JOIN genres g ON m.id = g.movieId GROUP BY id
              """));
          return result.Select(x => new Movie() {
              Id = x.id,
              Title = x.title,
              Year = x.year,
              Genre = Enumerable.ToList(x.genres.Split(','))
          });
      }

      public async Task<Movie?> GetByIdAsync(Guid id)
      {
          using var connection = await _connectionFactoryConnection.CreateConnectionAsync();
          var movie = await connection.QueryFirstOrDefaultAsync<Movie>(new CommandDefinition("""
              SELECT * FROM movies WHERE id = @Id
              """, new { Id = id }));
          
          if (movie is null)  return null;

          var genres = await connection.QueryAsync<string>(new CommandDefinition("""
              SELECT name FROM genres WHERE movieId = @MovieId
              """, new { MovieId = id }));
          foreach (var genre in genres)
          {
              movie.Genre.Add(genre);
          }        
      
          return movie;
      }

      public async Task<Movie?> GetBySlugAsync(string id)
      {
          using var connection = await _connectionFactoryConnection.CreateConnectionAsync();
          var movie = await connection.QueryFirstOrDefaultAsync<Movie>(new CommandDefinition("""
              SELECT * FROM movies WHERE slug = @Slug
              """, new { Slug = id }));

          if (movie is null)  return null;

          var genres = await connection.QueryAsync<string>(new CommandDefinition("""
              SELECT name FROM genres WHERE movieId = @MovieId
              """, new { MovieId = movie.Id }));
          foreach (var genre in genres)
          {
              movie.Genre.Add(genre);
          }        
          
          return movie;          
      }

      public async Task<bool> UpdateAsync(Movie movie)
      {
          using var connection = await _connectionFactoryConnection.CreateConnectionAsync();
          // Use a transaction as we updates multiple tables in the database
          using var transaction = connection.BeginTransaction();        

          await connection.ExecuteAsync( new CommandDefinition("""
              DELETE FROM genres WHERE movieId = @Id
              """, new { Id = movie.Id }));
              
          foreach (var genre in movie.Genre)
          {
              await connection.ExecuteAsync( new CommandDefinition("""
                  INSERT INTO genres (movieId, name) VALUES (@MovieId, @Name)
                  """, new { MovieId = movie.Id, Name = genre }));
          }

          var result = await connection.ExecuteAsync( new CommandDefinition("""
              UPDATE movies SET title = @Title, slug = @Slug, year = @Year WHERE id = @Id
              """, movie));

          transaction.Commit();

          return result > 0;
      }
  }

  ```

  ## Add Business Logic

  We need to introduce a service that manage the business logic. We do not want to add that logic in the existing repository as
  its responsibility is to persist the data in the database. We also do not waht this logic in the controller, resulting in *fat controllers*. 

  Add a service between the controller and the repository, where the business logic sits.

  