using FeatureManagement.FeatureFlags;
using FeatureManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace FeatureManagement.Controllers;

/// <summary>
/// Demonstrates A/B testing patterns using feature flags.
/// Returns different response shapes for control vs treatment groups.
/// </summary>
[ApiController]
[Route("api/v1/products")]
public class ExperimentController : ControllerBase
{
    private readonly IProductService _products;
    private readonly IFeatureManager _features;

    public ExperimentController(
        IProductService products,
        IFeatureManager features)
    {
        _products = products;
        _features = features;
    }

    /// <summary>
    /// Returns recommendations from the new ML model for the treatment group,
    /// and the legacy rule-based recommendations for the control group.
    /// The A/B split is determined by GradualRolloutFilter (25% treatment).
    /// </summary>
    [HttpGet("{id}/recommendations")]
    public async Task<IActionResult> GetRecommendations(
        string id, CancellationToken ct)
    {
        var useNewEngine = await _features.IsEnabledAsync(
            FeatureFlags.FeatureFlags.NewRecommendationEngine);

        var recommendations = useNewEngine
            ? await _products.GetMLRecommendationsAsync(id, ct)
            : await _products.GetRuleBasedRecommendationsAsync(id, ct);

        // Include experiment metadata in response so analytics can attribute results
        return Ok(new
        {
            productId = id,
            recommendations,
            experiment = new
            {
                flag = FeatureFlags.FeatureFlags.NewRecommendationEngine,
                variant = useNewEngine ? "treatment" : "control"
            }
        });
    }

    /// <summary>
    /// Search endpoint with semantic search treatment.
    /// [FeatureGate] on the NEW endpoint — old endpoint always available.
    /// </summary>
    [HttpGet("search/semantic")]
    [FeatureGate(FeatureFlags.FeatureFlags.SemanticSearch)]
    public async Task<IActionResult> SemanticSearch(
        [FromQuery] string q, CancellationToken ct)
    {
        var results = await _products.SemanticSearchAsync(q, ct);
        return Ok(results);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q, CancellationToken ct)
    {
        // In the same endpoint: route to semantic or keyword search by flag
        var useSemanticSearch = await _features.IsEnabledAsync(FeatureFlags.FeatureFlags.SemanticSearch);

        var results = useSemanticSearch
            ? await _products.SemanticSearchAsync(q, ct)
            : await _products.KeywordSearchAsync(q, ct);

        return Ok(results);
    }
}