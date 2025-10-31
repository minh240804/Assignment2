using Assignment2.DataAccess.Models;
using Assignment2.DataAccess.Repositories;
using Microsoft.ML;

namespace Assignment2.BusinessLogic
{
    public class ArticleRecommendationService : IArticleRecommendationService
    {
        private readonly INewsArticleRepository _articleRepository;
        private static PredictionEngine<ArticleInput, ArticleOutput>? _predictionEngine;
        private static Dictionary<string, float[]>? _articleFeatureCache;
        private static readonly object _lockObject = new object();
        private static string? _loadedModelPath;

        public ArticleRecommendationService(INewsArticleRepository articleRepository, string modelPath)
        {
            _articleRepository = articleRepository;

            // Initialize model and cache only once (thread-safe)
            lock (_lockObject)
            {
                if (_predictionEngine == null || _loadedModelPath != modelPath)
                {
                    // Load the trained ML model
                    var mlContext = new MLContext();
                    ITransformer trainedModel = mlContext.Model.Load(modelPath, out var modelInputSchema);
                    _predictionEngine = mlContext.Model.CreatePredictionEngine<ArticleInput, ArticleOutput>(trainedModel);
                    _loadedModelPath = modelPath;

                    // Initialize cache
                    _articleFeatureCache = new Dictionary<string, float[]>();

                    // Pre-compute features for all published articles
                    InitializeFeatureCache();
                }
            }
        }

        private void InitializeFeatureCache()
        {
            var allArticles = _articleRepository.GetAll()
                .Where(a => a.NewsStatus == true)
                .ToList();

            foreach (var article in allArticles)
            {
                var content = $"{article.NewsTitle} {article.NewsContent} {article.Headline}";
                var input = new ArticleInput { ArticleId = article.NewsArticleId, Content = content };
                var prediction = _predictionEngine!.Predict(input);
                _articleFeatureCache![article.NewsArticleId] = prediction.Features;
            }
        }

        public List<NewsArticle> GetSimilarArticles(string articleId, int topN = 5)
        {
            // Ensure cache is initialized
            if (_articleFeatureCache == null || _predictionEngine == null)
                return new List<NewsArticle>();

            // Check if article exists in cache
            if (!_articleFeatureCache.ContainsKey(articleId))
            {
                // Article might be new, compute features on-the-fly
                var article = _articleRepository.Get(articleId);
                if (article == null || article.NewsStatus != true)
                    return new List<NewsArticle>();

                var content = $"{article.NewsTitle} {article.NewsContent} {article.Headline}";
                var input = new ArticleInput { ArticleId = article.NewsArticleId, Content = content };
                
                lock (_lockObject)
                {
                    var prediction = _predictionEngine.Predict(input);
                    _articleFeatureCache[article.NewsArticleId] = prediction.Features;
                }
            }

            var targetFeatures = _articleFeatureCache[articleId];
            var similarities = new List<(string ArticleId, float Similarity)>();

            // Calculate cosine similarity with all other articles
            foreach (var kvp in _articleFeatureCache)
            {
                if (kvp.Key == articleId) continue; // Skip the same article

                var similarity = CosineSimilarity(targetFeatures, kvp.Value);
                similarities.Add((kvp.Key, similarity));
            }

            // Get top N most similar articles
            var topArticleIds = similarities
                .OrderByDescending(s => s.Similarity)
                .Take(topN)
                .Select(s => s.ArticleId)
                .ToList();

            // Fetch the actual article objects
            var recommendedArticles = _articleRepository.GetAll()
                .Where(a => topArticleIds.Contains(a.NewsArticleId))
                .ToList();

            // Order by similarity (maintain the order from topArticleIds)
            return topArticleIds
                .Select(id => recommendedArticles.FirstOrDefault(a => a.NewsArticleId == id))
                .Where(a => a != null)
                .ToList()!;
        }

        /// <summary>
        /// Calculate cosine similarity between two feature vectors
        /// </summary>
        private float CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
                return 0f;

            float dotProduct = 0f;
            float magnitudeA = 0f;
            float magnitudeB = 0f;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }

            magnitudeA = (float)Math.Sqrt(magnitudeA);
            magnitudeB = (float)Math.Sqrt(magnitudeB);

            if (magnitudeA == 0f || magnitudeB == 0f)
                return 0f;

            return dotProduct / (magnitudeA * magnitudeB);
        }
    }

    // ML.NET model classes (must match ModelTrainer)
    public class ArticleInput
    {
        public string ArticleId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class ArticleOutput
    {
        public float[] Features { get; set; } = Array.Empty<float>();
    }
}
