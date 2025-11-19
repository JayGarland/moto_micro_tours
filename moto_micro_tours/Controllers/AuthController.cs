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
    /// Register a new user with the specified role (Motard or Participant)
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>User information if successful</returns>
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponseDto>> Register([FromBody] RegisterRequestDto request)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new { message = "Username is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Password is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Role) ||
            (request.Role != "Motard" && request.Role != "Participant"))
        {
            return BadRequest(new { message = "Role must be either 'Motard' or 'Participant'" });
        }

        // Check if username already exists
        var existingUser = await _userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Username is already taken" });
        }

        // Check if email already exists
        existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Email is already registered" });
        }

        // Create new user
        var user = new AppUser
        {
            UserName = request.Username,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("User registration failed: {Errors}", errors);
            return BadRequest(new { message = $"Registration failed: {errors}" });
        }

        // Assign role
        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            _logger.LogError("Failed to assign role '{Role}' to user '{Username}'", request.Role, user.UserName);
            // Rollback user creation if role assignment fails
            await _userManager.DeleteAsync(user);
            return BadRequest(new { message = "Failed to assign role to user" });
        }

        _logger.LogInformation("User '{Username}' registered successfully with role '{Role}'", user.UserName, request.Role);

        // Auto sign-in after registration
        await _signInManager.SignInAsync(user, isPersistent: true);

        return Ok(new LoginResponseDto
        {
            UserId = user.Id,
            Username = user.UserName,
            Email = user.Email,
            Roles = new List<string> { request.Role }
        });
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
public record RegisterRequestDto
{
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string Role { get; init; } = null!; // "Motard" or "Participant"
}

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
