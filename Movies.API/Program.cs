using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Movies.API.Auth;
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

builder.Services.AddAuthorization(x =>
{
    x.AddPolicy(APIAuthorizationConstants.AdminUserPolicyName, p => p.RequireClaim(APIAuthorizationConstants.AdminUserClaimName, "true"));
    x.AddPolicy(APIAuthorizationConstants.TrustedUserPolicyName, p => p.RequireAssertion(ctx =>
        ctx.User.HasClaim(APIAuthorizationConstants.TrustedUserClaimName, "true") ||
        ctx.User.HasClaim(APIAuthorizationConstants.AdminUserClaimName, "true")
    ));
});

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
