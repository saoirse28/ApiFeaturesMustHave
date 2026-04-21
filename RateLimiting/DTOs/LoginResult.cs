namespace RateLimiting.DTOs
{
    public class LoginResult
    {
        public bool Succeeded { get; set; }
        public string Token { get; set; }
        public DateTimeOffset ExpiresIn { get; set; }
    }
}
