namespace Caching.Caching;

/// <summary>
/// String constants for output cache policy names.
/// Avoids magic strings when applying [OutputCache(PolicyName = "...")] attributes.
/// </summary>
public static class CachePolicies
{
    public const string Products = "Products";
    public const string Categories = "Categories";
    public const string UserProfile = "UserProfile";
    public const string NoCache = "NoCache";
}