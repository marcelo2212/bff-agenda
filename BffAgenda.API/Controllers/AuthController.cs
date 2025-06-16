using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BffAgenda.Application.Auth.DTOs;
using BffAgenda.Application.DTOs.Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace BffAgenda.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        if (dto.Username == "admin" && dto.Password == "123456")
        {
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");

            if (string.IsNullOrWhiteSpace(jwtSecret))
                return StatusCode(
                    500,
                    ResponseDto<string>.ErrorResponse("JWT_SECRET não definido.")
                );

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(jwtSecret);
            var expires = DateTime.UtcNow.AddHours(1);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, dto.Username) }),
                Expires = expires,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                ),
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            var response = new AuthResponseDto
            {
                AccessToken = tokenHandler.WriteToken(token),
                ExpiresIn = (int)(expires - DateTime.UtcNow).TotalSeconds,
                User = new AuthUserDto { Username = dto.Username },
            };

            return Ok(
                ResponseDto<AuthResponseDto>.SuccessResponse(
                    response,
                    "Login realizado com sucesso."
                )
            );
        }

        return Unauthorized(ResponseDto<string>.ErrorResponse("Credenciais inválidas."));
    }
}
