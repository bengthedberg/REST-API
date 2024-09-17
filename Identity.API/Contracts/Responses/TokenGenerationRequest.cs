namespace Identity.API.Contracts.Responses;

public class TokenGenerationRequest
{
    public required Guid UserId { get; set; }

    public required string Email { get; set; }

    public Dictionary<string, object> CustomClaims { get; set; } = new();
}