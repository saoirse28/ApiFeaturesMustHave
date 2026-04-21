namespace FeatureManagement.FeatureFlags;

/// <summary>
/// Single source of truth for all feature flag names.
///
/// Rules for good feature flags:
///   1. Names are PascalCase strings that match appsettings.json keys exactly.
///   2. Every flag has an XML summary explaining its purpose and owner.
///   3. Flags are temporary — each has a planned removal date in the comment.
///   4. Never hardcode flag name strings anywhere except this class.
///
/// Removal checklist (before deleting a flag):
///   [ ] Flag enabled 100% in production for at least 1 sprint
///   [ ] All feature branches merged and old code paths deleted
///   [ ] Flag removed from appsettings.json and Azure App Config
///   [ ] This constant and its usages deleted
/// </summary>
public static class FeatureFlags
{
    // ── Checkout domain ───────────────────────────────────────────────────────

    /// <summary>
    /// New multi-step checkout flow with address validation.
    /// Owner: @checkout-team | Added: 2025-01-15 | Remove by: 2025-04-01
    /// </summary>
    public const string NewCheckoutFlow = "NewCheckoutFlow";

    /// <summary>
    /// One-click checkout for returning customers with saved payment methods.
    /// Owner: @checkout-team | Added: 2025-02-01 | Remove by: 2025-05-01
    /// </summary>
    public const string OneClickCheckout = "OneClickCheckout";

    /// <summary>
    /// Buy Now Pay Later payment option at checkout.
    /// Owner: @payments-team | Added: 2025-02-10 | Remove by: 2025-06-01
    /// </summary>
    public const string BuyNowPayLater = "BuyNowPayLater";

    // ── Product catalog domain ────────────────────────────────────────────────

    /// <summary>
    /// New product recommendation engine using ML model v2.
    /// Owner: @ml-team | Added: 2025-01-20 | Remove by: 2025-04-15
    /// </summary>
    public const string NewRecommendationEngine = "NewRecommendationEngine";

    /// <summary>
    /// Enhanced product search with semantic similarity.
    /// Owner: @search-team | Added: 2025-03-01 | Remove by: 2025-07-01
    /// </summary>
    public const string SemanticSearch = "SemanticSearch";

    // ── UI / Dashboard domain ─────────────────────────────────────────────────

    /// <summary>
    /// Redesigned dashboard with new KPI widgets.
    /// Owner: @frontend-team | Added: 2025-02-20 | Remove by: 2025-05-15
    /// </summary>
    public const string BetaDashboard = "BetaDashboard";

    /// <summary>
    /// Dark mode toggle in user preferences.
    /// Owner: @frontend-team | Added: 2025-03-10 | Remove by: 2025-06-01
    /// </summary>
    public const string DarkMode = "DarkMode";

    // ── Infrastructure / performance flags ────────────────────────────────────

    /// <summary>
    /// Routes read queries to the read replica instead of the primary DB.
    /// Owner: @infra-team | Added: 2025-01-01 | Remove by: 2025-04-01
    /// </summary>
    public const string ReadReplicaRouting = "ReadReplicaRouting";

    /// <summary>
    /// Enables HybridCache for the product catalog (requires .NET 9 preview).
    /// Owner: @infra-team | Added: 2025-03-01 | Remove by: 2025-08-01
    /// </summary>
    public const string HybridCacheEnabled = "HybridCacheEnabled";

    // ── Kill switches (always defined, default OFF) ───────────────────────────

    /// <summary>
    /// Kill switch: disables all outbound email sending.
    /// Flip to true in appsettings or Azure App Config during email incidents.
    /// </summary>
    public const string DisableEmailSending = "DisableEmailSending";

    /// <summary>
    /// Kill switch: routes all traffic to maintenance page.
    /// </summary>
    public const string MaintenanceMode = "MaintenanceMode";
}