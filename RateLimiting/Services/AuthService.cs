using RateLimiting.DTOs;

namespace RateLimiting.Services
{
    public class AuthService : IAuthService
    {
        public Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct)
        {
            return Task.FromResult(
                new LoginResult()
                {
                Succeeded = true,
                Token = Guid.NewGuid().ToString(),
                ExpiresIn = DateTimeOffset.UtcNow.AddHours(1)
                });
        }

        public Task<LoginResult> RefreshAsync(string token, CancellationToken ct)
        {
            return Task.FromResult(
                new LoginResult
                {
                    Succeeded = true,
                    Token = Guid.NewGuid().ToString(),
                    ExpiresIn = DateTimeOffset.UtcNow.AddHours(1)
                });
        }

        public Task SendPasswordResetEmailAsync(string email, CancellationToken ct)
        {
            Task.Run(() => { /* send email logic */ }, ct);
            return Task.CompletedTask;
        }

        public Task<bool> VerifyOtpAsync(string userid, string code, CancellationToken ct)
        {
            return Task.FromResult(true);
        }
    }
}
