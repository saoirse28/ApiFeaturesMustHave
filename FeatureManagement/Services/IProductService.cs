namespace FeatureManagement.Services
{
    public interface IProductService
    {
        public Task<IEnumerable<string>> GetMLRecommendationsAsync(
            string productId, CancellationToken ct);
        public Task<IEnumerable<string>> GetRuleBasedRecommendationsAsync(
            string productId, CancellationToken ct);
        public Task<IEnumerable<string>> SemanticSearchAsync(
            string query, CancellationToken ct);
        public Task<IEnumerable<string>> KeywordSearchAsync(
            string query, CancellationToken ct);
    }
}
