namespace ExceptionHandling.Services
{
    public interface ICurrentUserService
    {
        public string UserId { get; }
        public bool IsAdmin { get; }
    }
}
