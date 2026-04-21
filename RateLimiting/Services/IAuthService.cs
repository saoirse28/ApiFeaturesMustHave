using Microsoft.AspNetCore.Identity.Data;
using RateLimiting.DTOs;

namespace RateLimiting.Services
{
    public interface IAuthService
    {
        public Task<LoginResult> LoginAsync(DTOs.LoginRequest request, CancellationToken ct);
        public Task<LoginResult> RefreshAsync(string token, CancellationToken ct);
        public Task SendPasswordResetEmailAsync(string email, CancellationToken ct);
        public Task<bool> VerifyOtpAsync(string userid, string code, CancellationToken ct);
    }
}
