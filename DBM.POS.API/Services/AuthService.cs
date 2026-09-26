using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DBM.POS.API.DTOs.Auth;
using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DBM.POS.API.Services;

public class AuthService
{
    private readonly POSDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(
        POSDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request)
    {
        var username = request.Username.Trim();

        var user = await _context.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                    .ThenInclude(x => x.RolePermissions)
                        .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x =>
                x.Username == username &&
                x.IsActive);

        if (user == null)
        {
            return null;
        }

        // Password verification
        var passwordHasher = new PasswordHasher<
            DBM.POS.Domain.Entities.User>();

        var passwordResult =
            passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        var passwordValid =
            passwordResult == PasswordVerificationResult.Success ||
            passwordResult == PasswordVerificationResult.SuccessRehashNeeded;

        if (!passwordValid)
        {
            return null;
        }

        // Roles
        var roles = user.UserRoles
            .Where(x =>
                x.IsActive &&
                x.Role.IsActive)
            .Select(x => x.Role.RoleName)
            .Distinct()
            .ToList();

        // Permissions
        var permissions = user.UserRoles
            .Where(x =>
                x.IsActive &&
                x.Role.IsActive)
            .SelectMany(x => x.Role.RolePermissions)
            .Where(x =>
                x.IsActive &&
                x.Permission.IsActive)
            .Select(x => x.Permission.PermissionCode)
            .Distinct()
            .ToList();

        var expiryMinutes =
            _configuration.GetValue<int>(
                "Jwt:ExpiryMinutes");

        var expiresAt =
            DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = GenerateToken(
            user,
            roles,
            permissions,
            expiresAt);

        user.LastLoginAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Roles = roles,
            Permissions = permissions
        };
    }

    private string GenerateToken(
        DBM.POS.Domain.Entities.User user,
        List<string> roles,
        List<string> permissions,
        DateTime expiresAt)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT Key is not configured.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT Issuer is not configured.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT Audience is not configured.");

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new(
                ClaimTypes.Name,
                user.Username),

            new(
                "fullName",
                user.FullName),

            new(
                "companyId",
                user.CompanyId.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(
                new Claim(
                    "permission",
                    permission));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}