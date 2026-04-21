namespace FeatureManagement.Services
{
    public class ProductService : IProductService
    {
        public Task<IEnumerable<string>> GetMLRecommendationsAsync(string productId, CancellationToken ct)
        {
            return Task.FromResult<IEnumerable<string>>(new List<string>
            {
                $"ML Recommendation 1 for product {productId}",
                $"ML Recommendation 2 for product {productId}",
                $"ML Recommendation 3 for product {productId}"
            });
        }

        public Task<IEnumerable<string>> GetRuleBasedRecommendationsAsync(string productId, CancellationToken ct)
        { 
            return Task.FromResult<IEnumerable<string>>(new List<string>
            {
                $"Rule-Based Recommendation 1 for product {productId}",
                $"Rule-Based Recommendation 2 for product {productId}",
                $"Rule-Based Recommendation 3 for product {productId}"
            });
        }

        public Task<IEnumerable<string>> KeywordSearchAsync(string query, CancellationToken ct)
        {
            return Task.FromResult<IEnumerable<string>>(new List<string>
            {
                $"Keyword Search Result 1 for query '{query}'",
                $"Keyword Search Result 2 for query '{query}'",
                $"Keyword Search Result 3 for query '{query}'"
            });
        }

        public Task<IEnumerable<string>> SemanticSearchAsync(string query, CancellationToken ct)
        {
            return Task.FromResult<IEnumerable<string>>(new List<string>
            {
                $"Semantic Search Result 1 for query '{query}'",
                $"Semantic Search Result 2 for query '{query}'",
                $"Semantic Search Result 3 for query '{query}'"
            });
        }
    }
}
