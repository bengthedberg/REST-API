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
