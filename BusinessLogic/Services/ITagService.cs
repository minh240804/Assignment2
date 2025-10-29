using System.Collections.Generic;
using VuQuangMinh_ass2_He180094.DataAccess.Models;

namespace VuQuangMinh_ass2_He180094.BusinessLogic
{
    public interface ITagService
    {
        IEnumerable<Tag> GetAll();
        Tag? Get(int id);
        void Add(Tag tag);
        (bool Success, string Message) Update(Tag tag);
        bool Delete(int id);
        IEnumerable<Tag> Search(string? tagName);
        IEnumerable<NewsArticle> GetArticlesByTag(int tagId);
    }
}
