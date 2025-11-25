using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();


var validIssuer = "Issuer";
var validAudience = "Audience";
var symmetricSecurityKey = "A secure key that's shared between AspNetCore and Azure Functions";

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.IncludeErrorDetails = true;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ClockSkew = TimeSpan.Zero,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = validIssuer,
            ValidAudience = validAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(symmetricSecurityKey!)
            ),
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.MapControllers().RequireAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

app.MapGet("hello", () => "Hello").RequireAuthorization();

app.MapPost("/login", (LoginDTO dto) =>
{
    var claims = new List<Claim>()
    {
        new Claim(ClaimTypes.NameIdentifier, dto.Username)
    };

    claims.AddRange(dto.Claims.Select(x => new Claim(x.Key, x.Value)));

    var token = new JwtSecurityToken(
        validIssuer,
        validAudience,
        claims,
        expires: DateTime.UtcNow.AddMinutes(15),
        signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(symmetricSecurityKey!)
            ),
            SecurityAlgorithms.HmacSha256
        )
    );

    var tokenHandler = new JwtSecurityTokenHandler();
    
    return tokenHandler.WriteToken(token);
});

app.Run();


public class LoginDTO
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;

    //Claims are not added during a user login
    //But it's enough for this sample project.
    public Dictionary<string, string> Claims { get; set; } = default!;
}