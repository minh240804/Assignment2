using System;
using System.Collections.Generic;
using VuQuangMinh_ass2_He180094.DataAccess.Models;

namespace VuQuangMinh_ass2_He180094.DataAccess.Repositories
{
    public interface INewsArticleRepository
    {
        IEnumerable<NewsArticle> GetAll(bool? active = null);
        NewsArticle? Get(string id);
        public string GetNextId();

        void Add(NewsArticle news);
        void Update(NewsArticle news);
        void Delete(NewsArticle news);

        IEnumerable<NewsArticle> GetByStaff(short staffId);
        IEnumerable<NewsArticle> GetReport(DateTime? start, DateTime? end);


        IEnumerable<NewsArticle> Search(
            string? keyword,
            DateTime? from,
            DateTime? to,
            short? categoryId,
            bool? status);

        IEnumerable<NewsArticle> GetRelatedArticles(string currentArticleId, short? categoryId, int take = 3);
    }
}
