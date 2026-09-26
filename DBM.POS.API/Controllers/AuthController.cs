using System.Security.Claims;
using DBM.POS.API.DTOs.Auth;
using DBM.POS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBM.POS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    // ==========================================
    // LOGIN
    // ==========================================

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Username and password are required."
            });
        }

        var result = await _authService.LoginAsync(request);

        if (result == null)
        {
            return Unauthorized(new
            {
                message = "Invalid username or password."
            });
        }

        return Ok(result);
    }


    // ==========================================
    // CURRENT USER
    // ==========================================

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        var username =
            User.FindFirstValue(
                ClaimTypes.Name);

        var fullName =
            User.FindFirstValue("fullName");

        var companyId =
            User.FindFirstValue("companyId");

        var roles = User
            .FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Distinct()
            .ToList();

        var permissions = User
            .FindAll("permission")
            .Select(x => x.Value)
            .Distinct()
            .ToList();

        return Ok(new
        {
            userId,
            username,
            fullName,
            companyId,
            roles,
            permissions
        });
    }
}