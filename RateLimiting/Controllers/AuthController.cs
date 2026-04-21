using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RateLimiting.DTOs;
using RateLimiting.RateLimiting;
using RateLimiting.Services;

namespace RateLimiting.Controllers;

/// <summary>
/// Authentication endpoints always use the strictest rate limits.
/// All policies are IP-based here — users are anonymous at login time.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService auth, ILogger<AuthController> logger)
    {
        _auth   = auth;
        _logger = logger;
    }

    // 5 attempts per 15 minutes per IP — brute-force protection
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<IActionResult> Login(
        [FromBody] DTOs.LoginRequest request,
        CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);

        if (!result.Succeeded)
        {
            // Log failed attempt — correlate with rate limit events
            _logger.LogWarning(
                "Failed login attempt for {Username} from {Ip}",
                request.Username, HttpContext.GetClientIp());
            return Unauthorized(new { error = "Invalid credentials" });
        }

        return Ok(new { token = result.Token, expiresIn = result.ExpiresIn });
    }

    // 5 attempts per 15 minutes per IP
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        var result = await _auth.RefreshAsync(request.Token, ct);
        return result is null
            ? Unauthorized()
            : Ok(new { token = result.Token, expiresIn = result.ExpiresIn });
    }

    // 3 attempts per hour per user — password reset abuse prevention
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.SensitiveOperation)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken ct)
    {
        // Always return 200 even if email doesn't exist — prevents user enumeration
        await _auth.SendPasswordResetEmailAsync(request.Email, ct);
        return Ok(new { message = "If the email exists, a reset link has been sent." });
    }

    // 3 attempts per hour — OTP/MFA code validation
    [HttpPost("verify-otp")]
    [EnableRateLimiting(RateLimitPolicies.SensitiveOperation)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] OtpRequest request,
        CancellationToken ct)
    {
        var valid = await _auth.VerifyOtpAsync(request.UserId, request.Code, ct);
        return valid ? Ok() : BadRequest(new { error = "Invalid or expired OTP" });
    }
}