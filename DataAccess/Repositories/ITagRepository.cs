using System.Collections.Generic;
using VuQuangMinh_ass2_He180094.DataAccess.Models;

namespace VuQuangMinh_ass2_He180094.DataAccess.Repositories
{
    public interface ITagRepository
    {
        IEnumerable<Tag> GetAll();
        Tag? Get(int id);
        void Add(Tag tag);
        void Update(Tag tag);
        void Delete(Tag tag);
        IEnumerable<Tag> Search(string? tagName);
        IEnumerable<NewsArticle> GetArticlesByTag(int tagId);
    }
}
