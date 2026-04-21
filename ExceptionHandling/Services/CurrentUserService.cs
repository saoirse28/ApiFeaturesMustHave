namespace ExceptionHandling.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public string UserId => "ADMIN";

        public bool IsAdmin => false;
    }
}
