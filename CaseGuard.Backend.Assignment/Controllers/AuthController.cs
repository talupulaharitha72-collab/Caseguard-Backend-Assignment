using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CaseGuard.Backend.Assignment.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CaseGuard.Backend.Assignment.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IConfiguration config) : ControllerBase
{
    private static Guid DeterministicGuid(string email)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(email.ToLowerInvariant()));
        return new Guid(bytes);
    }

    [HttpGet("claims")]
    [Authorize]
    public IActionResult Claims()
    {
        var claims = HttpContext.User.Claims
            .Select(c => new { c.Type, c.Value });
        return Ok(claims);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Role))
            return BadRequest(new { error = "Email and Role are required" });

        if (req.Role != "Admin" && req.Role != "User")
            return BadRequest(new { error = "Role must be 'Admin' or 'User'" });

        var userId = DeterministicGuid(req.Email);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, req.Email),
            new Claim(ClaimTypes.Role, req.Role),
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), userId, req.Email, req.Role));
    }
}
