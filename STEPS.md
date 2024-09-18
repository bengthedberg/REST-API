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

dotnet sln add (ls -r **/*.csproj)
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

1. Add `Services\IMovieService.cs` 
    ```csharp
    using Movies.Application.Models;

    namespace Movies.Application.Services;

    public interface IMovieService
    {
        Task<Movie?> GetByIdAsync(Guid id);
        Task<Movie?> GetBySlugAsync(string id);
        Task<IEnumerable<Movie>> GetAllAsync();
        Task<bool> CreateAsync(Movie movie); // true if movie was created successfully
        Task<Movie?> UpdateAsync(Movie movie); // true if movie was updated successfully
        Task<bool> DeleteAsync(Guid id); // true if movie was deleted successfully
    }
    ```

2. Implement in interface in `Services\MovieService.cs`  
    ```csharp
    using Movies.Application.Models;
    using Movies.Application.Repositories;

    namespace Movies.Application.Services;

    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _movieRepository;

        public MovieService(IMovieRepository movieRepository)
        {
            _movieRepository = movieRepository;
        }

        public async Task<bool> CreateAsync(Movie movie)
        {
            return await _movieRepository.CreateAsync(movie);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _movieRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<Movie>> GetAllAsync()
        {
            return await _movieRepository.GetAllAsync();
        }

        public async Task<Movie?> GetByIdAsync(Guid id)
        {
            return await _movieRepository.GetByIdAsync(id);
        }

        public async Task<Movie?> GetBySlugAsync(string id)
        {
            return await _movieRepository.GetBySlugAsync(id);
        }

        public async Task<Movie?> UpdateAsync(Movie movie)
        {
            var exists = await _movieRepository.ExistByIdAsync(movie.Id);
            if (exists)
            {
                if (await _movieRepository.UpdateAsync(movie))
                {
                    return movie;
                }
            }
            return null;
        }
    }
    ```

3. Add the service in the `ServiceExtension.cs`
    ```csharp
    using Microsoft.Extensions.DependencyInjection;
    using Movies.Application.Database;
    using Movies.Application.Repositories;
    using Movies.Application.Services;

    public static class ServiceExtension
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<IMovieRepository, MovieRepository>();
            services.AddSingleton<IMovieService, MovieService>();
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

4. Update the controller `MovieController.cs` to use the service instead of the repository:
    ```csharp
    using Microsoft.AspNetCore.Mvc;
    using Movies.API.Mapping;
    using Movies.Application.Services;
    using Movies.Contracts.Requests;

    namespace Movies.API.Controllers;

    [ApiController]
    public class MovieController : ControllerBase
    {
        private readonly IMovieService _movieService;
        public MovieController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        [HttpPost(APIEndpoints.Movies.Create)]
        public async Task<IActionResult> Create([FromBody] CreateMovieRequest request)
        {
            var movie = request.ToMovie();
            await _movieService.CreateAsync(movie);
            return CreatedAtAction(nameof(Get), new { identity = movie.Id },
                movie.ToMovieResponse());
        }

        [HttpGet(APIEndpoints.Movies.Get)]
        public async Task<IActionResult> Get([FromRoute] string identity)
        {
            var movie = Guid.TryParse(identity, out var id)
                ? await _movieService.GetByIdAsync(id)
                : await _movieService.GetBySlugAsync(identity);
            if (movie is null)
            {
                return NotFound();
            }
            return Ok(movie.ToMovieResponse());
        }

        [HttpGet(APIEndpoints.Movies.GetAll)]
        public async Task<IActionResult> GetAll()
        {
            var movies = await _movieService.GetAllAsync();
            return Ok(movies.ToMoviesResponse());
        }

        [HttpPut(APIEndpoints.Movies.Update)]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMovieRequest request)
        {
            var movie = request.ToMovie(id);
            var updateMovie = await _movieService.UpdateAsync(movie);
            if (updateMovie is null)
            {
                return NotFound();
            }
            return Ok(updateMovie.ToMovieResponse());
        }

        [HttpDelete(APIEndpoints.Movies.Delete)]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var deleted = await _movieService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
    ```

## Note 

Currently we are *leaking* the `Movie` model to external clients through the API. Normally you would implement MediatR between the Contyroller and Service, using DTO objects instead of the `Movie` class directly.

A Data Transfer Object is an object that is used to encapsulate data, and send it from one subsystem of an application to another.

DTOs are most commonly used by the Services layer in an N-Tier application to transfer data between itself and the UI layer. By encapsulating the serialization like this, the DTOs keep this logic out of the rest of the code and also provide a clear point to change serialization should you wish.

As this solution does not cover clean architecture, etc. then this will be ignored.

## Validation

There are 2 types of validations

- API Validation for incoming requests
- Business logic validation

In this section we will implement business logic validations, using Fluent Validation.

1. Add Fluent Validation to the Application project.

    `dotnet add .\Movies.Application\Movies.Application.csproj package FluentValidation.DependencyInjectionExtensions`

2. Create `Validators\MovieValidator.cs`
    ```csharp
    using FluentValidation;
    using Movies.Application.Models;
    using Movies.Application.Repositories;

    namespace Movies.Application.Validators;

    public class MovieValidator : AbstractValidator<Movie>
    {
        private readonly IMovieRepository _movieRepository;

        public MovieValidator(IMovieRepository movieRepository)
        {
            _movieRepository = movieRepository;

            RuleFor(x => x.Id)
                .NotEmpty();
            RuleFor(x => x.Genre)
                .NotEmpty();
            RuleFor(x => x.Title)
                .NotEmpty();
            RuleFor(x => x.Year)
                .LessThanOrEqualTo(DateTime.UtcNow.Year);
            RuleFor(x => x.Slug)
                .MustAsync(BeUniqueSlug)
                .WithMessage("The movie already exists.");
        }

        private async Task<bool> BeUniqueSlug(string slug, CancellationToken cancellationToken)
        {
            var movie = await _movieRepository.GetBySlugAsync(slug);
            return movie is null;
        }
    }
    ```     

3. Update `Services\MovireService.cs` to use the validator:
    ```csharp
    using FluentValidation;
    using Movies.Application.Models;
    using Movies.Application.Repositories;

    namespace Movies.Application.Services;

    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _movieRepository;
        private readonly IValidator<Movie> _movieValidator;

        public MovieService(IMovieRepository movieRepository, IValidator<Movie> movieValidator)
        {
            _movieRepository = movieRepository;
            _movieValidator = movieValidator;
        }

        public async Task<bool> CreateAsync(Movie movie)
        {
            await _movieValidator.ValidateAndThrowAsync(movie);
            return await _movieRepository.CreateAsync(movie);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _movieRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<Movie>> GetAllAsync()
        {
            return await _movieRepository.GetAllAsync();
        }

        public async Task<Movie?> GetByIdAsync(Guid id)
        {
            return await _movieRepository.GetByIdAsync(id);
        }

        public async Task<Movie?> GetBySlugAsync(string id)
        {
            return await _movieRepository.GetBySlugAsync(id);
        }

        public async Task<Movie?> UpdateAsync(Movie movie)
        {
            await _movieValidator.ValidateAndThrowAsync(movie);
            var exists = await _movieRepository.ExistByIdAsync(movie.Id);
            if (exists)
            {
                if (await _movieRepository.UpdateAsync(movie))
                {
                    return movie;
                }
            }
            return null;
        }
    }
    ```    

4. Create an assembly marker, that is used to mark the `Application` project when loading specific implemention within that assembly.
    ```csharp
    namespace Movies.Application;

    public interface IApplicationMarker
    {

    }
    ```

5. Register all validators by updating the `ServiceExtension.cs` class:
    ```csharp
    using FluentValidation;
    using Microsoft.Extensions.DependencyInjection;
    using Movies.Application;
    using Movies.Application.Database;
    using Movies.Application.Repositories;
    using Movies.Application.Services;

    public static class ServiceExtension
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<IMovieRepository, MovieRepository>();
            services.AddSingleton<IMovieService, MovieService>();
            // Validators are singleton as it is used in the services, i.e. the MovieService.cs which is a singleton.
            services.AddValidatorsFromAssemblyContaining<IApplicationMarker>(ServiceLifetime.Singleton);
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

The service throws an exception if a validation failed. It is then up to the API to handle this and responed with the validation failure details. 

Lets use a middleware to implement this.

6. Add a middleware in `Mapping\ValidationMappingMiddleware.cs` 
    ```csharp
    using System.Text.Json;
    using FluentValidation;
    using Movies.Contracts.Responses;

    namespace Movies.API.Mapping;

    public class ValidationMappingMiddleware
    {
        private readonly RequestDelegate _next;

        public ValidationMappingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                var validationFailureResponse = new ValidationFailureResponse
                {
                    Errors = ex.Errors.Select(x => new ValidationResponse { PropertyName = x.PropertyName, ErrorMessage = x.ErrorMessage })
                };
                await context.Response.WriteAsJsonAsync(validationFailureResponse);
            }
        }
    }
    ```
7. Add `app.UseMiddleware<ValidationMappingMiddleware>();` to `Program.cs` 
    ```csharp
    using Movies.API.Mapping;
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

    app.UseMiddleware<ValidationMappingMiddleware>();
    app.MapControllers();

    var dbMigration = app.Services.GetRequiredService<DatabaseMigration>();
    await dbMigration.Migrate();

    app.Run();

    ```    

8. Finally, create a new response type in the `Contract` project by adding file `Responses\ValidationFailureResponse.cs`
    ```csharp
    namespace Movies.Contracts.Responses;

    public class ValidationFailureResponse
    {
        public required IEnumerable<ValidationResponse> Errors { get; init; } = Array.Empty<ValidationResponse>();
    }

    public class ValidationResponse
    {
        public required string PropertyName { get; init; }
        public required string ErrorMessage { get; init; }
    }
    ```

## Cancellation Token

For a REST API a cancellation token is triggered when the client cancel an ongoing request.

If you handle these cancellation then it is important that the cancellation token is progressed through the calls, i.e. from the controller to the service and then to the repository and any outstanding database requests.
If not then you are really not managing the cancellation logic as you only cancel some steps.

1. Add `CancellationToken` to all endpoints and pass that down to the movie service:
    ```csharp
    using Microsoft.AspNetCore.Mvc;
    using Movies.API.Mapping;
    using Movies.Application.Services;
    using Movies.Contracts.Requests;

    namespace Movies.API.Controllers;

    [ApiController]
    public class MovieController : ControllerBase
    {
        private readonly IMovieService _movieService;
        public MovieController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        [HttpPost(APIEndpoints.Movies.Create)]
        public async Task<IActionResult> Create([FromBody] CreateMovieRequest request, CancellationToken token)
        {
            var movie = request.ToMovie();
            await _movieService.CreateAsync(movie, token);
            return CreatedAtAction(nameof(Get), new { identity = movie.Id },
                movie.ToMovieResponse());
        }

        [HttpGet(APIEndpoints.Movies.Get)]
        public async Task<IActionResult> Get([FromRoute] string identity, CancellationToken token)
        {
            var movie = Guid.TryParse(identity, out var id)
                ? await _movieService.GetByIdAsync(id, token)
                : await _movieService.GetBySlugAsync(identity, token);
            if (movie is null)
            {
                return NotFound();
            }
            return Ok(movie.ToMovieResponse());
        }

        [HttpGet(APIEndpoints.Movies.GetAll)]
        public async Task<IActionResult> GetAll(CancellationToken token)
        {
            var movies = await _movieService.GetAllAsync(token);
            return Ok(movies.ToMoviesResponse());
        }

        [HttpPut(APIEndpoints.Movies.Update)]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMovieRequest request, CancellationToken token)
        {
            var movie = request.ToMovie(id);
            var updateMovie = await _movieService.UpdateAsync(movie, token);
            if (updateMovie is null)
            {
                return NotFound();
            }
            return Ok(updateMovie.ToMovieResponse());
        }

        [HttpDelete(APIEndpoints.Movies.Delete)]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken token)
        {
            var deleted = await _movieService.DeleteAsync(id, token);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
    }

    ```

2. Update the `IMovieService.cs` to accept the cancellation tokens
    ```csharp
    using Movies.Application.Models;

    namespace Movies.Application.Services;

    public interface IMovieService
    {
        Task<Movie?> GetByIdAsync(Guid id, CancellationToken token = default);
        Task<Movie?> GetBySlugAsync(string id, CancellationToken token = default);
        Task<IEnumerable<Movie>> GetAllAsync(CancellationToken token = default);
        Task<bool> CreateAsync(Movie movie, CancellationToken token = default); // true if movie was created successfully
        Task<Movie?> UpdateAsync(Movie movie, CancellationToken token = default); // true if movie was updated successfully
        Task<bool> DeleteAsync(Guid id, CancellationToken token = default); // true if movie was deleted successfully
    }

    ```

3. Manage the token in the `MovieService`, basically accept the token and pass it to validation and repositories.
    ```csharp
    using FluentValidation;
    using Movies.Application.Models;
    using Movies.Application.Repositories;

    namespace Movies.Application.Services;

    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _movieRepository;
        private readonly IValidator<Movie> _movieValidator;

        public MovieService(IMovieRepository movieRepository, IValidator<Movie> movieValidator)
        {
            _movieRepository = movieRepository;
            _movieValidator = movieValidator;
        }

        public async Task<bool> CreateAsync(Movie movie, CancellationToken token = default)
        {
            await _movieValidator.ValidateAndThrowAsync(movie, token);
            return await _movieRepository.CreateAsync(movie, token);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken token = default)
        {
            return await _movieRepository.DeleteAsync(id, token);
        }
        public async Task<IEnumerable<Movie>> GetAllAsync(CancellationToken token = default)
        {
            return await _movieRepository.GetAllAsync(token);
        }

        public async Task<Movie?> GetByIdAsync(Guid id, CancellationToken token = default)
        {
            return await _movieRepository.GetByIdAsync(id, token);
        }

        public async Task<Movie?> GetBySlugAsync(string id, CancellationToken token = default)
        {
            return await _movieRepository.GetBySlugAsync(id, token);
        }

        public async Task<Movie?> UpdateAsync(Movie movie, CancellationToken token = default)
        {
            await _movieValidator.ValidateAndThrowAsync(movie, token);
            var exists = await _movieRepository.ExistByIdAsync(movie.Id, token);
            if (exists)
            {
                if (await _movieRepository.UpdateAsync(movie, token))
                {
                    return movie;
                }
            }
            return null;
        }
    }

    ```

4. Update the `IMovieRepository.cs` to accept tokens:
    ```csharp
    using Movies.Application.Models;

    namespace Movies.Application.Repositories;

    public interface IMovieRepository
    {
    Task<Movie?> GetByIdAsync(Guid id, CancellationToken token = default);
    Task<Movie?> GetBySlugAsync(string id, CancellationToken token = default);
    Task<IEnumerable<Movie>> GetAllAsync(CancellationToken token = default);
    Task<bool> CreateAsync(Movie movie, CancellationToken token = default); // true if movie was created successfully
    Task<bool> UpdateAsync(Movie movie, CancellationToken token = default); // true if movie was updated successfully
    Task<bool> DeleteAsync(Guid id, CancellationToken token = default); // true if movie was deleted successfully
    Task<bool> ExistByIdAsync(Guid id, CancellationToken token = default);
    }
    ```

5. Modify the `MovireRepository.cs` to accept and pass the toekn to Dapper.
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

        public async Task<bool> CreateAsync(Movie movie, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            // Use a transaction as we updates multiple tables in the database
            using var transaction = connection.BeginTransaction();

            var result = await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO movies (id, title, slug, year) VALUES (@Id, @Title, @Slug, @Year)
                """, movie, cancellationToken: token));
            if (result > 0)
            {
                foreach (var genre in movie.Genre)
                {
                    result = await connection.ExecuteAsync(new CommandDefinition("""
                        INSERT INTO genres (movieId, name) VALUES (@MovieId, @Name)
                        """, new { MovieId = movie.Id, Name = genre }));
                }
            }

            transaction.Commit();
            return result > 0;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var result = await connection.ExecuteAsync(new CommandDefinition("""
                DELETE FROM movies WHERE id = @Id
                """, new { Id = id }, cancellationToken: token));
            return result > 0;
        }

        public async Task<bool> ExistByIdAsync(Guid id, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
                SELECT COUNT(*) FROM movies WHERE id = @Id
                """, new { Id = id }, cancellationToken: token));
            return exists;
        }

        public async Task<IEnumerable<Movie>> GetAllAsync(CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var result = await connection.QueryAsync(new CommandDefinition("""
                SELECT m.*, string_agg(g.name, ',') as genres 
                FROM movies m LEFT JOIN genres g ON m.id = g.movieId GROUP BY id
                """, cancellationToken: token));
            return result.Select(x => new Movie()
            {
                Id = x.id,
                Title = x.title,
                Year = x.year,
                Genre = Enumerable.ToList(x.genres.Split(','))
            });
        }

        public async Task<Movie?> GetByIdAsync(Guid id, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var movie = await connection.QueryFirstOrDefaultAsync<Movie>(new CommandDefinition("""
                SELECT * FROM movies WHERE id = @Id
                """, new { Id = id }, cancellationToken: token));

            if (movie is null) return null;

            var genres = await connection.QueryAsync<string>(new CommandDefinition("""
                SELECT name FROM genres WHERE movieId = @MovieId
                """, new { MovieId = id }, cancellationToken: token));
            foreach (var genre in genres)
            {
                movie.Genre.Add(genre);
            }

            return movie;
        }

        public async Task<Movie?> GetBySlugAsync(string id, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var movie = await connection.QueryFirstOrDefaultAsync<Movie>(new CommandDefinition("""
                SELECT * FROM movies WHERE slug = @Slug
                """, new { Slug = id }, cancellationToken: token));

            if (movie is null) return null;

            var genres = await connection.QueryAsync<string>(new CommandDefinition("""
                SELECT name FROM genres WHERE movieId = @MovieId
                """, new { MovieId = movie.Id }, cancellationToken: token));
            foreach (var genre in genres)
            {
                movie.Genre.Add(genre);
            }

            return movie;
        }

        public async Task<bool> UpdateAsync(Movie movie, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            // Use a transaction as we updates multiple tables in the database
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(new CommandDefinition("""
                DELETE FROM genres WHERE movieId = @Id
                """, new { Id = movie.Id }, cancellationToken: token));

            foreach (var genre in movie.Genre)
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                    INSERT INTO genres (movieId, name) VALUES (@MovieId, @Name)
                    """, new { MovieId = movie.Id, Name = genre }, cancellationToken: token));
            }

            var result = await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE movies SET title = @Title, slug = @Slug, year = @Year WHERE id = @Id
                """, movie, cancellationToken: token));

            transaction.Commit();

            return result > 0;
        }
    }

    ```

6. Also add the cancellation toekn when we create database connections:

- `IDatabaseConnectionFactory.cs`
    ```csharp
    using System.Data;

    namespace Movies.Application.Database;

    public interface IDatabaseConnectionFactory
    {
        Task<IDbConnection> CreateConnectionAsync(CancellationToken token = default);
    }
    ```
- `NpgSqlConnectionFactory.cs`
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

        public async Task<IDbConnection> CreateConnectionAsync(CancellationToken token = default)
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(token);
            return connection;
        }
    }
    ``` 

## Authentication and Authorization

**Authentication** is the process of verifying a user's identity to ensure they are who they claim to be. 

> Verifying **WHO** the user is.

**Authorization** is the process of granting the authenticated user permission to access specific resources or perform certain actions.

> Verifying **WHAT** the user can do.

REST API will use a token for this. The token itself is not created in this API, but rather a service specifically dceveloped for managing user authentication and access levels.  

This API will only validate that user token (authentication) and check the policies in it to determine what the user is authorised to do.

### JWT - JSON Web Token

In its compact form, JSON Web Tokens consist of three parts separated by dots (.), which are:

- Header
- Payload
- Signature

Therefore, a JWT typically looks like the following.

`xxxxx.yyyyy.zzzzz`

#### Header
The header typically consists of two parts: the type of the token, which is JWT, and the signing algorithm being used, such as HMAC SHA256 or RSA.


#### Payload
The second part of the token is the payload, which contains the claims. Claims are statements about an entity (typically, the user) and additional data.

Some of the claims are standard claims and others are customisable. 

> Do not put any secret data in the payload.


#### Signature
To create the signature part you have to take the encoded header, the encoded payload, a secret, the algorithm specified in the header, and sign that.

**How do JSON Web Tokens work?**

In authentication, when the user successfully logs in using their credentials, a JSON Web Token will be returned. 

Whenever the user wants to access a protected route or resource, the user agent should send the JWT, typically in the **Authorization** header using the Bearer schema. 

#### Pre-Requisite

The `Identity.API` will dummy up a local identity server which will generate a valid JWT based on the request. This will be used in the `API` project. 

#### Add Authentication using JWT

1. Add required nuget packages to `API` project:     
    `dotnet add .\Movies.API\Movies.API.csproj package Microsoft.AspNetCore.Authentication.JwtBearer`

2. Update the `Program.cs` file to configure and use authentication:
    ```csharp
    using System.Text;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.IdentityModel.Tokens;
    using Movies.API.Mapping;
    using Movies.Application.Database;

    var builder = WebApplication.CreateBuilder(args);
    var config = builder.Configuration;
    // Add services to the container.

    builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(x =>
    {
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,  // Check that the signing key is valid
            ValidateLifetime = true,          // Check that the token is not expired
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!)),
            ValidateIssuer = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = config["Jwt:Audience"]
        };
    });

    builder.Services.AddAuthorization();

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

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<ValidationMappingMiddleware>();
    app.MapControllers();

    var dbMigration = app.Services.GetRequiredService<DatabaseMigration>();
    await dbMigration.Migrate();

    app.Run();
    ```

3. Mark `MovieController.cs` to be accessible only for authorized users using the ` [Authorize]` attribute:
    ```csharp
    ...
    namespace Movies.API.Controllers;

    [ApiController]
    [Authorize]
    public class MovieController : ControllerBase
    {
        private readonly IMovieService _movieService;
    ...

    ```

4. Make some endpoint publicly accessible using the `[AllowAnonymous]` attribute:
    ```csharp

        [HttpGet(APIEndpoints.Movies.GetAll)]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(CancellationToken token)
        {
            var movies = await _movieService.GetAllAsync(token);
            return Ok(movies.ToMoviesResponse());
        }

    ```    

5. Add the confiration to `appsettings.json` (these values comes from the `Identity.API` project)
    ```json
        {
        "Database" : {
            "ConnectionString": "Server=localhost;Port=5432;Database=movies;User Id=demo;Password=demo;"
        },
        "Jwt": {
            "Secret": "ThisSecretKeyIsOnlyUsedForLocalDevelopment",
            "Issuer": "https://id.localhost.com",
            "Audience": "https://movies.localhost.com"
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


#### Add Authorization using Claims

Use claims to limit certain endpoints to users with specific roles.

Limit the `Create`, `Delete` and `Update` to `Admin` users. 

1. Add Policy to `Program.cs`
    ```csharp
    builder.Services.AddAuthorization(x => {
        x.AddPolicy("Admin", p => p.RequireClaim("admin", "true"));    
    });
    ```

2. Add the `[Authorize("Admin")]` attributes to the `Create`, `Delete` and `Update` endpoints. 


## Add User Rating

A user can rate a movie from 1 to 5. 

A user comes from the JWT token and a rating id done through a specific endpoint.

1. Add the required endpoints by modifying `APIEndpoints.cs`.
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
            public const string AddRating = $"{Base}/{{id:guid}}/rating";
            public const string DeleteRating = $"{Base}/{{id:guid}}/rating";    
        }

        public static class Rating
        {
            private const string Base = $"{BaseURL}/rating";
            public const string GetUserRatings =  $"{Base}/me";
        }

        }
    ```

    We need an endpoint for creating and adding ratings for a specific movie.

    We also need a different endpoint to get all ratings for a user.

2. Modify the response to return ratings for the user as well as average rating. These are optional.      
    `Responses\MovieResponse.cs`
    ```csharp
        namespace Movies.Contracts.Responses;

        public class MovieResponse
        {
        public required Guid Id { get; init; }
        public required string Title { get; init; }
        public required string Slug { get; init; }
        public required int Year { get; init; }
        public int? UserRating { get; init; }
        public float? AverageRating { get; init; }
        public required IEnumerable<string> Genre { get; init; } = Enumerable.Empty<string>();

        }
    ```
3. Add a new rating table in the database by adding the following to `Database\DatabaseMigration.cs`
    ```csharp
        await connection.ExecuteAsync(
            """
            CREATE TABLE IF NOT EXISTS ratings (
                userId UUID NOT NULL,
                movieId UUID REFERENCES movies(id) ON DELETE CASCADE,
                rating INTEGER NOT NULL,
                PRIMARY KEY(userId, movieId)
            );
            """);    
    ```

4. Grab the user id from the JWT by adding a new extension method called `IdentityExtension`
    ```csharp
    namespace Movies.API.Auth;

    public static class IdentityExtension
    {
        public static Guid? GetUserId(this HttpContext httpContext)
        {
            var userId = httpContext.User.Claims.SingleOrDefault(c => c.Type == APIAuthorizationConstants.UserIdClaimName)?.Value;
            if (userId == null)
            {
                return null;
            }

            return Guid.Parse(userId);
        }
    }
    ```

    Note that we added `UserIdClaimName` to `APIAuthorizationConstants` 
    ```csharp
        public const string UserIdClaimName = "userid";
    ```

5. Extend the `MovieController` to get current user and pass it to the service and repository    
    `IMovieService.cs` 
    ```csharp
    using Movies.Application.Models;

    namespace Movies.Application.Services;

    public interface IMovieService
    {
        Task<Movie?> GetByIdAsync(Guid id, Guid? userId = default, CancellationToken token = default);
        Task<Movie?> GetBySlugAsync(string id, Guid? userId = default, CancellationToken token = default);
        Task<IEnumerable<Movie>> GetAllAsync(Guid? userId = default, CancellationToken token = default);
        Task<bool> CreateAsync(Movie movie, CancellationToken token = default); // true if movie was created successfully
        Task<Movie?> UpdateAsync(Movie movie, Guid? userId = default, CancellationToken token = default); // true if movie was updated successfully
        Task<bool> DeleteAsync(Guid id, CancellationToken token = default); // true if movie was deleted successfully
    }
    ```
    `MovieController.cs`
    ```csharp
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Movies.API.Auth;
    using Movies.API.Mapping;
    using Movies.Application.Services;
    using Movies.Contracts.Requests;

    namespace Movies.API.Controllers;

    [ApiController]
    public class MovieController : ControllerBase
    {
        private readonly IMovieService _movieService;
        public MovieController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        [HttpPost(APIEndpoints.Movies.Create)]
        [Authorize(APIAuthorizationConstants.TrustedUserPolicyName)]
        public async Task<IActionResult> Create([FromBody] CreateMovieRequest request, CancellationToken token)
        {
            var movie = request.ToMovie();
            await _movieService.CreateAsync(movie, token);
            return CreatedAtAction(nameof(Get), new { identity = movie.Id },
                movie.ToMovieResponse());
        }

        [HttpGet(APIEndpoints.Movies.Get)]
        [AllowAnonymous]
        public async Task<IActionResult> Get([FromRoute] string identity, CancellationToken token)
        {
            var userId = HttpContext.GetUserId();
            var movie = Guid.TryParse(identity, out var id)
                ? await _movieService.GetByIdAsync(id, userId, token)
                : await _movieService.GetBySlugAsync(identity, userId, token);
            if (movie is null)
            {
                return NotFound();
            }
            return Ok(movie.ToMovieResponse());
        }

        [HttpGet(APIEndpoints.Movies.GetAll)]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(CancellationToken token)
        {
            var userId = HttpContext.GetUserId();
            var movies = await _movieService.GetAllAsync(userId, token);
            return Ok(movies.ToMoviesResponse());
        }

        [HttpPut(APIEndpoints.Movies.Update)]
        [Authorize(APIAuthorizationConstants.TrustedUserPolicyName)]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMovieRequest request, CancellationToken token)
        {
            var userId = HttpContext.GetUserId();
            var movie = request.ToMovie(id);
            var updateMovie = await _movieService.UpdateAsync(movie, userId, token);
            if (updateMovie is null)
            {
                return NotFound();
            }
            return Ok(updateMovie.ToMovieResponse());
        }

        [HttpDelete(APIEndpoints.Movies.Delete)]
        [Authorize(APIAuthorizationConstants.AdminUserPolicyName)]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken token)
        {
            var deleted = await _movieService.DeleteAsync(id, token);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
    ```
    `IMovieRepository.cs`
    ```csharp
    using Movies.Application.Models;

    namespace Movies.Application.Repositories;

    public interface IMovieRepository
    {
    Task<Movie?> GetByIdAsync(Guid id, Guid? userId = default, CancellationToken token = default);
    Task<Movie?> GetBySlugAsync(string id, Guid? userId = default, CancellationToken token = default);
    Task<IEnumerable<Movie>> GetAllAsync(Guid? userId = default, CancellationToken token = default);
    Task<bool> CreateAsync(Movie movie, CancellationToken token = default); // true if movie was created successfully
    Task<bool> UpdateAsync(Movie movie, Guid? userId = default, CancellationToken token = default); // true if movie was updated successfully
    Task<bool> DeleteAsync(Guid id, CancellationToken token = default); // true if movie was deleted successfully
    Task<bool> ExistByIdAsync(Guid id, CancellationToken token = default);
    }
    ```
    `MovieService.cs`    
    ```csharp
    using FluentValidation;
    using Movies.Application.Models;
    using Movies.Application.Repositories;

    namespace Movies.Application.Services;

    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _movieRepository;
        private readonly IValidator<Movie> _movieValidator;

        public MovieService(IMovieRepository movieRepository, IValidator<Movie> movieValidator)
        {
            _movieRepository = movieRepository;
            _movieValidator = movieValidator;
        }

        public async Task<bool> CreateAsync(Movie movie, CancellationToken token = default)
        {
            await _movieValidator.ValidateAndThrowAsync(movie, token);
            return await _movieRepository.CreateAsync(movie, token);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken token = default)
        {
            return await _movieRepository.DeleteAsync(id, token);
        }
        public async Task<IEnumerable<Movie>> GetAllAsync(Guid? userId = default, CancellationToken token = default)
        {
            return await _movieRepository.GetAllAsync(userId, token);
        }

        public async Task<Movie?> GetByIdAsync(Guid id, Guid? userId = default, CancellationToken token = default)
        {
            return await _movieRepository.GetByIdAsync(id, userId, token);
        }

        public async Task<Movie?> GetBySlugAsync(string id, Guid? userId = default, CancellationToken token = default)
        {
            return await _movieRepository.GetBySlugAsync(id, userId, token);
        }

        public async Task<Movie?> UpdateAsync(Movie movie, Guid? userId = default, CancellationToken token = default)
        {
            await _movieValidator.ValidateAndThrowAsync(movie, token);
            var exists = await _movieRepository.ExistByIdAsync(movie.Id, token);
            if (exists)
            {
                if (await _movieRepository.UpdateAsync(movie, userId, token))
                {
                    return movie;
                }
            }
            return null;
        }
    }
    ```
    `MovieRepository.cs`    
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

        public async Task<bool> CreateAsync(Movie movie, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            // Use a transaction as we updates multiple tables in the database
            using var transaction = connection.BeginTransaction();

            var result = await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO movies (id, title, slug, year) VALUES (@Id, @Title, @Slug, @Year)
                """, movie, cancellationToken: token));
            if (result > 0)
            {
                foreach (var genre in movie.Genre)
                {
                    result = await connection.ExecuteAsync(new CommandDefinition("""
                        INSERT INTO genres (movieId, name) VALUES (@MovieId, @Name)
                        """, new { MovieId = movie.Id, Name = genre }));
                }
            }

            transaction.Commit();
            return result > 0;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var result = await connection.ExecuteAsync(new CommandDefinition("""
                DELETE FROM movies WHERE id = @Id
                """, new { Id = id }, cancellationToken: token));
            return result > 0;
        }

        public async Task<bool> ExistByIdAsync(Guid id, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
                SELECT COUNT(*) FROM movies WHERE id = @Id
                """, new { Id = id }, cancellationToken: token));
            return exists;
        }

        public async Task<IEnumerable<Movie>> GetAllAsync(Guid? userId = default, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var result = await connection.QueryAsync(new CommandDefinition("""
                SELECT m.*, string_agg(g.name, ',') as genres 
                FROM movies m LEFT JOIN genres g ON m.id = g.movieId GROUP BY id
                """, cancellationToken: token));
            return result.Select(x => new Movie()
            {
                Id = x.id,
                Title = x.title,
                Year = x.year,
                Genre = Enumerable.ToList(x.genres.Split(','))
            });
        }

        public async Task<Movie?> GetByIdAsync(Guid id, Guid? userId = default, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var movie = await connection.QueryFirstOrDefaultAsync<Movie>(new CommandDefinition("""
                SELECT * FROM movies WHERE id = @Id
                """, new { Id = id }, cancellationToken: token));

            if (movie is null) return null;

            var genres = await connection.QueryAsync<string>(new CommandDefinition("""
                SELECT name FROM genres WHERE movieId = @MovieId
                """, new { MovieId = id }, cancellationToken: token));
            foreach (var genre in genres)
            {
                movie.Genre.Add(genre);
            }

            return movie;
        }

        public async Task<Movie?> GetBySlugAsync(string id, Guid? userId = default, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var movie = await connection.QueryFirstOrDefaultAsync<Movie>(new CommandDefinition("""
                SELECT * FROM movies WHERE slug = @Slug
                """, new { Slug = id }, cancellationToken: token));

            if (movie is null) return null;

            var genres = await connection.QueryAsync<string>(new CommandDefinition("""
                SELECT name FROM genres WHERE movieId = @MovieId
                """, new { MovieId = movie.Id }, cancellationToken: token));
            foreach (var genre in genres)
            {
                movie.Genre.Add(genre);
            }

            return movie;
        }

        public async Task<bool> UpdateAsync(Movie movie, Guid? userId = default, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            // Use a transaction as we updates multiple tables in the database
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(new CommandDefinition("""
                DELETE FROM genres WHERE movieId = @Id
                """, new { Id = movie.Id }, cancellationToken: token));

            foreach (var genre in movie.Genre)
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                    INSERT INTO genres (movieId, name) VALUES (@MovieId, @Name)
                    """, new { MovieId = movie.Id, Name = genre }, cancellationToken: token));
            }

            var result = await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE movies SET title = @Title, slug = @Slug, year = @Year WHERE id = @Id
                """, movie, cancellationToken: token));

            transaction.Commit();

            return result > 0;
        }
    }
    ```

6. Extend the domain model `Movie.cs`    
   ```csharp
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

   ```
7. Add a Rating Repository     
    `Repository\IRatingRepository.cs`
    ```csharp
    namespace Movies.Application.Repositories;

    public interface IRatingRepository
    {
        Task<float?> GetRatingAsync(Guid movieId, CancellationToken token = default);
        Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid movieId, Guid userId, CancellationToken token = default);
    }
    ```

    `Repository\RatingRepository.cs`
    ```csharp
    using Dapper;
    using Movies.Application.Database;

    namespace Movies.Application.Repositories;

    public class RatingRepository : IRatingRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactoryConnection;

        public RatingRepository(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _connectionFactoryConnection = databaseConnectionFactory;
        }

        public async Task<float?> GetRatingAsync(Guid movieId, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            return await connection.QuerySingleOrDefaultAsync<float?>(new CommandDefinition("""
                SELECT round(avg(rating), 1) FROM ratings WHERE id = @MovieId                      
                """, new { MovieId = movieId }, cancellationToken: token));
        }

        public async Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            return await connection.QuerySingleOrDefaultAsync<(float? Rating, int? UserRating)>(new CommandDefinition("""
                SELECT round(avg(r.rating), 1) as rating, ur.rating as userrating 
                FROM movies m
                LEFT JOIN ratings ur ON m.id = r.movieId AND r.userId = @UserId 
                LEFT JOIN ratings r ON m.id = r.movieId 
                WHERE id = @MovieId                     
                """, new { MovieId = movieId, @UserId = userId }, cancellationToken: token));
        }
    }
    ```

8. Add RatingRepository to Depencency Injections, `ServiceExtension.cs`
    ```csharp
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<IRatingRepository, RatingRepository>();
            services.AddSingleton<IMovieRepository, MovieRepository>();
            services.AddSingleton<IMovieService, MovieService>();
            // Validators are singleton as it is used in the services, i.e. the MovieService.cs which is a singleton.
            services.AddValidatorsFromAssemblyContaining<IApplicationMarker>(ServiceLifetime.Singleton);
            return services;
        }
    ```    

9. Add rating functionality to repository `MovieRepository.cs`
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

        public async Task<bool> CreateAsync(Movie movie, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            // Use a transaction as we updates multiple tables in the database
            using var transaction = connection.BeginTransaction();

            var result = await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO movies (id, title, slug, year) VALUES (@Id, @Title, @Slug, @Year)
                """, movie, cancellationToken: token));
            if (result > 0)
            {
                foreach (var genre in movie.Genre)
                {
                    result = await connection.ExecuteAsync(new CommandDefinition("""
                        INSERT INTO genres (movieId, name) VALUES (@MovieId, @Name)
                        """, new { MovieId = movie.Id, Name = genre }));
                }
            }

            transaction.Commit();
            return result > 0;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var result = await connection.ExecuteAsync(new CommandDefinition("""
                DELETE FROM movies WHERE id = @Id
                """, new { Id = id }, cancellationToken: token));
            return result > 0;
        }

        public async Task<bool> ExistByIdAsync(Guid id, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
                SELECT COUNT(*) FROM movies WHERE id = @Id
                """, new { Id = id }, cancellationToken: token));
            return exists;
        }

        public async Task<IEnumerable<Movie>> GetAllAsync(Guid? userId = default, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var result = await connection.QueryAsync(new CommandDefinition("""
                SELECT m.*, string_agg(distinct g.name, ',') as genres, round(avg(r.rating), 2) as rating, ur.rating as userrating  
                FROM movies m 
                LEFT JOIN genres g ON m.id = g.movieId 
                LEFT JOIN ratings ur ON m.id = r.movieId AND r.userId = @UserId 
                LEFT JOIN ratings r ON m.id = r.movieId              
                GROUP BY id
                """, new { UserId = userId }, cancellationToken: token));
            return result.Select(x => new Movie()
            {
                Id = x.id,
                Title = x.title,
                Year = x.year,
                Rating = (float?)x.rating,
                UserRating = (int?)x.userrating,
                Genre = Enumerable.ToList(x.genres.Split(','))
            });
        }

        public async Task<Movie?> GetByIdAsync(Guid id, Guid? userId = default, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var movie = await connection.QueryFirstOrDefaultAsync<Movie>(new CommandDefinition("""
                SELECT m.*, round(avg(r.rating), 2) as rating, ur.rating as userrating 
                FROM movies m
                LEFT JOIN ratings ur ON m.id = r.movieId AND r.userId = @UserId 
                LEFT JOIN ratings r ON m.id = r.movieId 
                WHERE id = @Id 
                """, new { Id = id, UserId = userId }, cancellationToken: token));

            if (movie is null) return null;

            var genres = await connection.QueryAsync<string>(new CommandDefinition("""
                SELECT name FROM genres WHERE movieId = @MovieId
                """, new { MovieId = id }, cancellationToken: token));
            foreach (var genre in genres)
            {
                movie.Genre.Add(genre);
            }

            return movie;
        }

        public async Task<Movie?> GetBySlugAsync(string id, Guid? userId = default, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var movie = await connection.QueryFirstOrDefaultAsync<Movie>(new CommandDefinition("""
                SELECT m.*, round(avg(r.rating), 2) as rating, ur.rating as userrating 
                FROM movies m
                LEFT JOIN ratings ur ON m.id = r.movieId AND r.userId = @UserId 
                LEFT JOIN ratings r ON m.id = r.movieId 
                WHERE id = @Id 
                """, new { Id = id, UserId = userId }, cancellationToken: token));

            if (movie is null) return null;

            var genres = await connection.QueryAsync<string>(new CommandDefinition("""
                SELECT name FROM genres WHERE movieId = @MovieId
                """, new { MovieId = movie.Id }, cancellationToken: token));
            foreach (var genre in genres)
            {
                movie.Genre.Add(genre);
            }

            return movie;
        }

        public async Task<bool> UpdateAsync(Movie movie, Guid? userId = default, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            // Use a transaction as we updates multiple tables in the database
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(new CommandDefinition("""
                DELETE FROM genres WHERE movieId = @Id
                """, new { Id = movie.Id }, cancellationToken: token));

            foreach (var genre in movie.Genre)
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                    INSERT INTO genres (movieId, name) VALUES (@MovieId, @Name)
                    """, new { MovieId = movie.Id, Name = genre }, cancellationToken: token));
            }

            var result = await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE movies SET title = @Title, slug = @Slug, year = @Year WHERE id = @Id
                """, movie, cancellationToken: token));

            transaction.Commit();

            return result > 0;
        }
    }    
   ```

10. Add RatingRepository to our `MovieService.cs` 
    ```csharp
    using FluentValidation;
    using Movies.Application.Models;
    using Movies.Application.Repositories;

    namespace Movies.Application.Services;

    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _movieRepository;
        private readonly IValidator<Movie> _movieValidator;
        private readonly IRatingRepository _ratingRepository;

        public MovieService(IMovieRepository movieRepository, IValidator<Movie> movieValidator, IRatingRepository ratingRepository)
        {
            _movieRepository = movieRepository;
            _movieValidator = movieValidator;
            _ratingRepository = ratingRepository;
        }

        public async Task<bool> CreateAsync(Movie movie, CancellationToken token = default)
        {
            await _movieValidator.ValidateAndThrowAsync(movie, token);
            return await _movieRepository.CreateAsync(movie, token);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken token = default)
        {
            return await _movieRepository.DeleteAsync(id, token);
        }
        public async Task<IEnumerable<Movie>> GetAllAsync(Guid? userId = default, CancellationToken token = default)
        {
            return await _movieRepository.GetAllAsync(userId, token);
        }

        public async Task<Movie?> GetByIdAsync(Guid id, Guid? userId = default, CancellationToken token = default)
        {
            return await _movieRepository.GetByIdAsync(id, userId, token);
        }

        public async Task<Movie?> GetBySlugAsync(string id, Guid? userId = default, CancellationToken token = default)
        {
            return await _movieRepository.GetBySlugAsync(id, userId, token);
        }

        public async Task<Movie?> UpdateAsync(Movie movie, Guid? userId = default, CancellationToken token = default)
        {
            await _movieValidator.ValidateAndThrowAsync(movie, token);
            var exists = await _movieRepository.ExistByIdAsync(movie.Id, token);
            if (!exists)
            {
                return null;
            }

            await _movieRepository.UpdateAsync(movie, userId, token);

            if (userId.HasValue)
            {
                (movie.Rating, movie.UserRating) = await _ratingRepository.GetRatingAsync(movie.Id, userId.Value, token);
            }
            else
            {
                movie.Rating = await _ratingRepository.GetRatingAsync(movie.Id, token);
            }

            return movie;
        }
    }
    ```

## Implement User Ratings

1. Extend the Ratiing Repository.    
    `Repository\IRatingRepository.cs`
    ```csharp
    namespace Movies.Application.Repositories;

    public interface IRatingRepository
    {
        Task<bool> UpsertRatingAsync(Guid movieId, Guid userId, int rating, CancellationToken token = default);
        Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token = default);
        Task<float?> GetRatingAsync(Guid movieId, CancellationToken token = default);
        Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid movieId, Guid userId, CancellationToken token = default);
    }
    ```

    `Repository\RatingRepository.cs`
    ```csharp
    using Dapper;
    using Movies.Application.Database;

    namespace Movies.Application.Repositories;

    public class RatingRepository : IRatingRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactoryConnection;

        public RatingRepository(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _connectionFactoryConnection = databaseConnectionFactory;
        }

        public async Task<bool> UpsertRatingAsync(Guid movieId, Guid userId, int rating, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var result = await connection.ExecuteAsync(new CommandDefinition("""
                        INSERT INTO ratings (userId, movieId, rating) VALUES (@UserId, @MovieId, @Rating)
                        ON CONFLICT (userId, movieId) DO UPDATE SET rating = @Rating
                        """, new { UserId = userId, MovieId = movieId, Rating = rating }, cancellationToken: token));
            return result > 0;
        }

        public async Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var result = await connection.ExecuteAsync(new CommandDefinition("""
                        DELETE FROM ratings WHERE movieId = @MovieId AND userId = @UserId
                        """, new { MovieId = movieId, UserId = userId }, cancellationToken: token));
            return result > 0;
        }

        public async Task<float?> GetRatingAsync(Guid movieId, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            return await connection.QuerySingleOrDefaultAsync<float?>(new CommandDefinition("""
                SELECT round(avg(rating), 1) FROM ratings WHERE id = @MovieId                      
                """, new { MovieId = movieId }, cancellationToken: token));
        }

        public async Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            return await connection.QuerySingleOrDefaultAsync<(float? Rating, int? UserRating)>(new CommandDefinition("""
                SELECT round(avg(r.rating), 1) as rating, min(ur.rating) as userrating 
                FROM movies m
                LEFT JOIN ratings ur ON m.id = ur.movieId AND ur.userId = @UserId 
                LEFT JOIN ratings r ON m.id = r.movieId 
                WHERE m.id = @MovieId                     
                """, new { MovieId = movieId, @UserId = userId }, cancellationToken: token));
        }
    }
    ```

2. Add Rating Service     
    `Services\IRatingService.cs`
    ```csharp
    namespace Movies.Application.Services;

    public interface IRatingService
    {
        Task<bool> RateMovieAsync(Guid movieId, Guid userId, int rating, CancellationToken token = default);
        Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token = default);
    }
    ```        
    `Services\RatingService.cs`
    ```csharp
    using System.Runtime.CompilerServices;
    using FluentValidation;
    using FluentValidation.Results;
    using Movies.Application.Repositories;

    namespace Movies.Application.Services;

    public class RatingService : IRatingService
    {
        private readonly IRatingRepository _ratingRepository;
        private readonly IMovieRepository _movieRepository;


        public RatingService(IRatingRepository ratingRepository, IMovieRepository movieRepository)
        {
            _ratingRepository = ratingRepository;
            _movieRepository = movieRepository;
        }

        public async Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
        {
            var movieExists = await _movieRepository.ExistByIdAsync(movieId, token);
            if (!movieExists)
            {
                return false;
            }
            return await _ratingRepository.DeleteRatingAsync(movieId, userId, token);
        }

        public async Task<bool> RateMovieAsync(Guid movieId, Guid userId, int rating, CancellationToken token = default)
        {
            if (rating < 1 || rating > 5)
            {
                throw new ValidationException([
                    new ValidationFailure("rating", "Rating must be between 1 and 5")
                ]);
            }
            if (userId == Guid.Empty)
            {
                throw new ValidationException([
                    new ValidationFailure("userId", "User ID must be provided")
                ]);
            }
            var movieExists = await _movieRepository.ExistByIdAsync(movieId, token);
            if (!movieExists)
            {
                return false;
            }

            return await _ratingRepository.UpsertRatingAsync(movieId, userId, rating, token);
        }
    }
    ```

3. Add a new request `RateMovieRequest.cs`
    ```csharp
    namespace Movies.Contracts.Requests;

    public class RateMovieRequest
    {
        public required int Rating { get; init; }
    }
    ```

4. Add a `RatingController.cs`
    ```csharp
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
    ```

5. Update the `ContractMappings.cs`
    ```csharp
    public static MovieResponse ToMovieResponse(this Movie movie)
    {
        return new MovieResponse()
        {
        Id = movie.Id,
        Title = movie.Title,
        Slug = movie.Slug,
        Year = movie.Year,
        UserRating = movie.UserRating,
        AverageRating = movie.Rating,
        Genre = movie.Genre
        };
    }
    ```

6. Update `ServiceExtension.cs` 
    ```csharp
        services.AddSingleton<IRatingService, RatingService>();
    ```

## Return All Ratings for a User

1. Start by adding anew model     
    `Models\MovieRating.cs`
    ```csharp
    using System;

    namespace Movies.Application.Models;

    public class MovieRating
    {
        public required Guid MovieId { get; init; }
        public required string Slug { get; init; }
        public required int Rating { get; init; }
    }
    ```
   
2. Add some new Contracts     
    `Responses\MovieRatingResponse.cs`
    ```csharp
    namespace Movies.Contracts.Responses;

    public class MovieRatingResponse
    {
        public required Guid MovieId { get; init; }
        public required string Slug { get; init; }
        public required int Rating { get; init; }
    }
    ```
    `Responses\MovieRatingsResponse.cs`
    ```csharp
    namespace Movies.Contracts.Responses;

    public class MovieRatingsResponse
    {
        public IEnumerable<MovieRatingResponse> MovieRatings { get; init; } = Enumerable.Empty<MovieRatingResponse>();
    }
    ```

3. Update the Rating Repository     
    `Repository\IRatingRepository.cs`
    ```csharp
    using Movies.Application.Models;

    namespace Movies.Application.Repositories;

    public interface IRatingRepository
    {
        Task<bool> UpsertRatingAsync(Guid movieId, Guid userId, int rating, CancellationToken token = default);
        Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token = default);
        Task<float?> GetRatingAsync(Guid movieId, CancellationToken token = default);
        Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid movieId, Guid userId, CancellationToken token = default);
        Task<IEnumerable<MovieRating>> GetRatingsByUserAsync(Guid userId, CancellationToken token = default);
    }
    ```
    `Repository\RatingRepository.cs`
    ```csharp
    using Dapper;
    using Movies.Application.Database;
    using Movies.Application.Models;

    namespace Movies.Application.Repositories;

    public class RatingRepository : IRatingRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactoryConnection;

        public RatingRepository(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _connectionFactoryConnection = databaseConnectionFactory;
        }

        public async Task<bool> UpsertRatingAsync(Guid movieId, Guid userId, int rating, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var result = await connection.ExecuteAsync(new CommandDefinition("""
                        INSERT INTO ratings (userId, movieId, rating) VALUES (@UserId, @MovieId, @Rating)
                        ON CONFLICT (userId, movieId) DO UPDATE SET rating = @Rating
                        """, new { UserId = userId, MovieId = movieId, Rating = rating }, cancellationToken: token));
            return result > 0;
        }

        public async Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var result = await connection.ExecuteAsync(new CommandDefinition("""
                        DELETE FROM ratings WHERE movieId = @MovieId AND userId = @UserId
                        """, new { MovieId = movieId, UserId = userId }, cancellationToken: token));
            return result > 0;
        }

        public async Task<float?> GetRatingAsync(Guid movieId, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            return await connection.QuerySingleOrDefaultAsync<float?>(new CommandDefinition("""
                SELECT round(avg(rating), 1) FROM ratings WHERE id = @MovieId                      
                """, new { MovieId = movieId }, cancellationToken: token));
        }

        public async Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            return await connection.QuerySingleOrDefaultAsync<(float? Rating, int? UserRating)>(new CommandDefinition("""
                SELECT round(avg(r.rating), 1) as rating, min(ur.rating) as userrating 
                FROM movies m
                LEFT JOIN ratings ur ON m.id = ur.movieId AND ur.userId = @UserId 
                LEFT JOIN ratings r ON m.id = r.movieId 
                WHERE m.id = @MovieId                     
                """, new { MovieId = movieId, @UserId = userId }, cancellationToken: token));
        }

        public async Task<IEnumerable<MovieRating>> GetRatingsByUserAsync(Guid userId, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var result = await connection.QueryAsync(new CommandDefinition("""
                SELECT m.id as movieid, m.slug as slug, r.rating as rating
                FROM movies m
                JOIN ratings r ON m.id = r.movieId
                WHERE r.userId = @UserId
                """, new { UserId = userId }, cancellationToken: token));
            return result.Select(r => new MovieRating()
            {
                MovieId = r.movieid,
                Slug = r.slug,
                Rating = (int)r.rating
            });
        }
    }
    ```

4. Udpaet the Repository Service      
    `Services\IRatingServices.cs`
    ```csharp
    using Movies.Application.Models;

    namespace Movies.Application.Services;

    public interface IRatingService
    {
        Task<bool> RateMovieAsync(Guid movieId, Guid userId, int rating, CancellationToken token = default);
        Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token = default);
        Task<IEnumerable<MovieRating>> GetRatingsByUserAsync(Guid userId, CancellationToken token = default);
    }
    ```
    `Services\RatingServices.cs`
    ```csharp
    using System.Runtime.CompilerServices;
    using FluentValidation;
    using FluentValidation.Results;
    using Movies.Application.Models;
    using Movies.Application.Repositories;

    namespace Movies.Application.Services;

    public class RatingService : IRatingService
    {
        private readonly IRatingRepository _ratingRepository;
        private readonly IMovieRepository _movieRepository;


        public RatingService(IRatingRepository ratingRepository, IMovieRepository movieRepository)
        {
            _ratingRepository = ratingRepository;
            _movieRepository = movieRepository;
        }

        public async Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
        {
            var movieExists = await _movieRepository.ExistByIdAsync(movieId, token);
            if (!movieExists)
            {
                return false;
            }
            return await _ratingRepository.DeleteRatingAsync(movieId, userId, token);
        }

        public async Task<IEnumerable<MovieRating>> GetRatingsByUserAsync(Guid userId, CancellationToken token = default)
        {
            return await _ratingRepository.GetRatingsByUserAsync(userId, token);
        }

        public async Task<bool> RateMovieAsync(Guid movieId, Guid userId, int rating, CancellationToken token = default)
        {
            if (rating < 1 || rating > 5)
            {
                throw new ValidationException([
                    new ValidationFailure("rating", "Rating must be between 1 and 5")
                ]);
            }
            if (userId == Guid.Empty)
            {
                throw new ValidationException([
                    new ValidationFailure("userId", "User ID must be provided")
                ]);
            }
            var movieExists = await _movieRepository.ExistByIdAsync(movieId, token);
            if (!movieExists)
            {
                return false;
            }

            return await _ratingRepository.UpsertRatingAsync(movieId, userId, rating, token);
        }
    }
    ```

5. Update the Rating Controller
    ```csharp
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Movies.API.Auth;
    using Movies.API.Mapping;
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

        [Authorize]
        [HttpGet(APIEndpoints.Rating.GetUserRatings)]
        public async Task<IActionResult> GetUserRatings([FromRoute] Guid id, CancellationToken token)
        {
            var userId = HttpContext.GetUserId();
            var result = await _ratingService.GetRatingsByUserAsync(userId!.Value, token);
            return Ok(result.ToMovieRatingsResponse());
        }
    }
    ```

## Filtering

Add filtering options when retriving data. In this case the get all movies can be filtered on either title and year.     

1. Create a new request `Request\GetAllMoviesRequest.cs` with optional parameters:
    ```csharp
    namespace Movies.Contracts.Requests;

    public class GetAllMoviesRequest
    {
        public required string? Title { get; init; }
        public required int? Year { get; init; }
    }
    ```

2. Add a new model for passing the request into the application layer `Models\GetAllMoviesOptions.cs`     
    ```csharp
    namespace Movies.Application.Models;

    public class GetAllMoviesOptions
    {
        public required string? Title { get; set; }
        public required int? Year { get; set; }
        public Guid? UserId { get; set; }
    }
    ```

3. Add some mapping between the request and model, not that the model will also include the current user id parameter as well.      
   Update `Mapping\ContractMapping.cs`
   ```csharp
        public static GetAllMoviesOptions ToGetAllMoviesOptions(this GetAllMoviesRequest request)
        {
            return new GetAllMoviesOptions()
            {
            Title = request.Title,
            Year = request.Year
            };
        }

        public static GetAllMoviesOptions WithUserId(this GetAllMoviesOptions options, Guid? userId)
        {
            options.UserId = userId;
            return options;
        }
   ```

4. Update the `MovieController.cs` so the `GetAll` accept the response, transforms it to the model and pass it to the Service.     
    ```csharp
        [HttpGet(APIEndpoints.Movies.GetAll)]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] GetAllMoviesRequest request, CancellationToken token)
        {
            var userId = HttpContext.GetUserId();
            var options = request.ToGetAllMoviesOptions().WithUserId(userId);
            var movies = await _movieService.GetAllAsync(options, token);
            return Ok(movies.ToMoviesResponse());
        }
    ```

5. Add some validation on the model, which we can use in the service.       
    Create `Validators\GetAllMoviesOptionsValidator.cs`
    ```csharp
    using FluentValidation;
    using Movies.Application.Models;

    namespace Movies.Application.Validators;

    public class GetAllMoviesOptionsValidator : AbstractValidator<GetAllMoviesOptions>
    {
        public GetAllMoviesOptionsValidator()
        {
            RuleFor(x => x.Year)
                .LessThanOrEqualTo(DateTime.UtcNow.Year);
        }

    }
    ```

6. Update the Movie Service     
    Update `Services\IMoveService.cs`
    ```csharp
        Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken token = default);
    ```    
    Update `Services\MoveService.cs`
    ```csharp
        public async Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken token = default)
        {
            await _getAllMoviesOptionsValidator.ValidateAndThrowAsync(options, token);
            return await _movieRepository.GetAllAsync(options, token);
        }
    ```

7. Update the Movie Repository
    Update `Repository\IMovieRepository.cs`
    ```csharp
        Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken token = default);
    ```
    Update `RepositoryIMovieRepository.cs`
    ```csharp
        public async Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);
            var result = await connection.QueryAsync(new CommandDefinition("""
                SELECT m.*, string_agg(distinct g.name, ',') as genres, round(avg(r.rating), 1) as rating, min(ur.rating) as userrating  
                FROM movies m 
                LEFT JOIN genres g ON m.id = g.movieId 
                LEFT JOIN ratings ur ON m.id = ur.movieId AND ur.userId = @UserId 
                LEFT JOIN ratings r ON m.id = r.movieId    
                WHERE (@Year IS NULL OR m.year = @Year) 
                AND (@Title IS NULL OR m.title ILIKE ( '%' || @Title || '%' ))          
                GROUP BY m.id
                """, new { UserId = options.UserId, Year = options.Year, Title = options.Title }, cancellationToken: token));
            return result.Select(x => new Movie()
            {
                Id = x.id,
                Title = x.title,
                Year = x.year,
                Rating = (float?)x.rating,
                UserRating = (int?)x.userrating,
                Genre = Enumerable.ToList(x.genres.Split(','))
            });
        }
    ```

## Sort

Sorting the result by fields. Avoid slow performance by only sorting on fields that are backed by database index or keys.

1. Update the request `GetAllMoviesRequest.cs`
    ```csharp
    namespace Movies.Contracts.Requests;

    public class GetAllMoviesRequest
    {
        public required string? Title { get; init; }
        public required int? Year { get; init; }
        public required string? SortBy { get; init; }
    }
    ```

2. Update the model `GetAllMoviesOptions.cs`
    ```csharp
    namespace Movies.Application.Models;

    public class GetAllMoviesOptions
    {
        public required string? Title { get; set; }
        public required int? Year { get; set; }
        public Guid? UserId { get; set; }

        public string? SortField { get; set; }
        public SortOrder? SortOrder { get; set; }
    }

    public enum SortOrder
    {
        Unsorted,
        Ascending,
        Descending
    }
    ```

3. Add the logic to the contract mapping `ContractMapping.cs`
    ```csharp
        public static GetAllMoviesOptions ToGetAllMoviesOptions(this GetAllMoviesRequest request)
        {
            return new GetAllMoviesOptions()
            {
            Title = request.Title,
            Year = request.Year,
            SortField = request.SortBy?.Trim('+', '-'),
            SortOrder = request.SortBy is null ? SortOrder.Unsorted :
                request.SortBy?.StartsWith('-') == true ? SortOrder.Descending : SortOrder.Ascending
            };
        }
    ```

4. Add the sorting to the repository `MovieRepository.cs`
    ```csharp
        public async Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken token = default)
        {
            using var connection = await _connectionFactoryConnection.CreateConnectionAsync(token);

            // Sort statement is safe as long as the SortField is validated by the application in GetAllMoviesOptionsValidator
            var sortOrder = options.SortOrder == SortOrder.Ascending ? "asc" : "desc";
            var sortStatement = options.SortField is not null ? $"ORDER BY m.{options.SortField} {sortOrder}" : string.Empty;

            var result = await connection.QueryAsync(new CommandDefinition($"""
                SELECT m.*, string_agg(distinct g.name, ',') as genres, round(avg(r.rating), 1) as rating, min(ur.rating) as userrating  
                FROM movies m 
                LEFT JOIN genres g ON m.id = g.movieId 
                LEFT JOIN ratings ur ON m.id = ur.movieId AND ur.userId = @UserId 
                LEFT JOIN ratings r ON m.id = r.movieId    
                WHERE (@Year IS NULL OR m.year = @Year) 
                AND (@Title IS NULL OR m.title ILIKE ( '%' || @Title || '%' ))                       
                GROUP BY m.id 
                {sortStatement} 
                """, new { UserId = options.UserId, Year = options.Year, Title = options.Title }, cancellationToken: token));
            return result.Select(x => new Movie()
            {
                Id = x.id,
                Title = x.title,
                Year = x.year,
                Rating = (float?)x.rating,
                UserRating = (int?)x.userrating,
                Genre = Enumerable.ToList(x.genres.Split(','))
            });
        }
    ```

5. Validation is required of the sort fields as the sort statement is just added to the SQL statement. This will prevent any sql injection attempts.     
    `Validators\GetAllMoviesOptionsValidatorcs`
    ```csharp
    using FluentValidation;
    using Movies.Application.Models;

    namespace Movies.Application.Validators;

    public class GetAllMoviesOptionsValidator : AbstractValidator<GetAllMoviesOptions>
    {
        private static readonly string[] AllowedSortFields = { "title", "year" };

        public GetAllMoviesOptionsValidator()
        {
            RuleFor(x => x.Year)
                .LessThanOrEqualTo(DateTime.UtcNow.Year);

            RuleFor(x => x.SortField)
                .Must(x => AllowedSortFields.Contains(x.ToLowerInvariant()))
                .When(x => x.SortField != null)
                .WithMessage($"Invalid sort field. Allowed values are: {string.Join(", ", AllowedSortFields)}");
        }

    }
    ```

