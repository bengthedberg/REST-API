namespace Identity.API.Contracts.Responses;

public class TokenGenerationRequest
{
    public Guid UserId { get; set; }

    public string Email { get; set; }

    public Dictionary<string, object> CustomClaims { get; set; } = new();
}