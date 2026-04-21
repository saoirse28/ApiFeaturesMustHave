using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Resilience.RateLimiting;

/// <summary>
/// Demonstrates a single dynamic policy that routes each request
/// to a different limiter partition based on the client's subscription tier.
/// One policy handles all tiers — no need for multiple [EnableRateLimiting] decorations.
///
/// Register in AddRateLimiter alongside the other policies.
/// </summary>
public static class TieredRateLimitPolicy
{
    public const string PolicyName = "tiered";

    public static void Register(RateLimiterOptions opts)
    {
        opts.AddPolicy(PolicyName, context =>
        {
            var clientKey = context.GetClientKey();
            var tier = context.GetClientTier();

            return tier switch
            {
                RateLimitPolicies.PremiumTier =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"premium:{clientKey}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit       = 1000,
                            Window            = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            AutoReplenishment = true
                        }),

                RateLimitPolicies.TrustedPartner =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"partner:{clientKey}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit       = 500,
                            Window            = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            AutoReplenishment = true
                        }),

                _ =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"standard:{clientKey}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit       = 100,
                            Window            = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            AutoReplenishment = true
                        })
            };
        });
    }
}