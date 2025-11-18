using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using moto_micro_tours.Domain.Entities;

namespace moto_micro_tours.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager,
        ILogger<AuthController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Login endpoint for testing - creates a cookie-based session
    /// Accepts either username or email as the identifier
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>User information if successful</returns>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.EmailOrUsername) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Email/Username and password are required" });
        }

        // Try to find user by username first, then by email
        var user = await _userManager.FindByNameAsync(request.EmailOrUsername);
        if (user == null)
        {
            user = await _userManager.FindByEmailAsync(request.EmailOrUsername);
        }

        if (user == null)
        {
            _logger.LogWarning("Login failed: user '{Identifier}' not found", request.EmailOrUsername);
            return Unauthorized(new { error = "Invalid email/username or password" });
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: true,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Login failed for user '{Username}'", user.UserName);
            return Unauthorized(new { error = "Invalid email/username or password" });
        }

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);

        _logger.LogInformation("User '{Username}' logged in successfully", user.UserName);

        return Ok(new LoginResponseDto
        {
            UserId = user.Id,
            Username = user.UserName!,
            Email = user.Email,
            Roles = roles.ToList()
        });
    }

    /// <summary>
    /// Logout endpoint - clears the authentication cookie
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out");
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Get current user information (useful for testing if you're logged in)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<LoginResponseDto>> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(new { error = "User not found" });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new LoginResponseDto
        {
            UserId = user.Id,
            Username = user.UserName!,
            Email = user.Email,
            Roles = roles.ToList()
        });
    }
}

// DTOs for Auth endpoints
public record LoginRequestDto
{
    public string EmailOrUsername { get; init; } = null!;
    public string Password { get; init; } = null!;
}

public record LoginResponseDto
{
    public string UserId { get; init; } = null!;
    public string Username { get; init; } = null!;
    public string? Email { get; init; }
    public List<string> Roles { get; init; } = new();
}
