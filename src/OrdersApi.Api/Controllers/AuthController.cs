using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OrdersApi.Domain.Configuration;

namespace OrdersApi.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(IOptions<JwtSettings> jwtSettings) : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var (isValid, role) = ValidateCredentials(request.Username, request.Password);

        if (!isValid)
        {
            return Unauthorized(new { Message = "Invalid email or password" });
        }

        var token = GenerateJwtToken(request.Username, role!);

        return Ok(new
        {
            Token = token,
            UserName = request.Username,
            Role = role,
            ExpiresIn = jwtSettings.Value.ExpirationInMinutes * 60
        });
    }

    private (bool isValid, string? role) ValidateCredentials(string email, string password)
    {
        return (email, password) switch
        {
            ("admin@admin.com", "Admin123!") => (true, "Admin"),
            ("user@someuser.com", "User123!") => (true, "User"),
            _ => (false, null)
        };
    }

    private string GenerateJwtToken(string userName, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtSettings.Value.Secret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Email, userName),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Sub, userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescription = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(jwtSettings.Value.ExpirationInMinutes),
            Issuer = jwtSettings.Value.Issuer,
            Audience = jwtSettings.Value.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescription);
        return tokenHandler.WriteToken(token);
    }
    public record LoginRequest(string Username, string Password);
}