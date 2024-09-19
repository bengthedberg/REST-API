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

    private async Task<bool> BeUniqueSlug(Movie movie, string slug, CancellationToken cancellationToken)
    {
        var existingMovie = await _movieRepository.GetBySlugAsync(slug);

        if (existingMovie is not null)
        {
            return existingMovie.Id == movie.Id;
        }

        return existingMovie is null;
    }
}
