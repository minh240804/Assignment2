using Assignment2.DataAccess.Models;

namespace Assignment2.BusinessLogic
{
    public interface IArticleRecommendationService
    {
        /// <summary>
        /// Get articles similar to the specified article using AI-based content analysis
        /// </summary>
        /// <param name="articleId">The ID of the article to find similar articles for</param>
        /// <param name="topN">Number of similar articles to return (default: 5)</param>
        /// <returns>List of similar articles ordered by similarity score</returns>
        List<NewsArticle> GetSimilarArticles(string articleId, int topN = 5);
    }
}
