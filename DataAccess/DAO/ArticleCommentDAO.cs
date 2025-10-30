using System;
using System.Collections.Generic;
using System.Linq;
using Assignment2.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Assignment2.DataAccess.DAO
{
    public class ArticleCommentDAO
    {
        private readonly FunewsManagementContext _ctx;

        public ArticleCommentDAO(FunewsManagementContext ctx) => _ctx = ctx;

        public IEnumerable<Comment> GetByArticle(string articleId) =>
            _ctx.Comments
                .Include(c => c.Account)
                .Where(c => c.ArticleId == articleId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

        public Comment? GetById(int id) =>
            _ctx.Comments
                .Include(c => c.Account)
                .Include(c => c.Article)
                .FirstOrDefault(c => c.CommentId == id);

        public void Add(Comment comment)
        {
            _ctx.Comments.Add(comment);
            _ctx.SaveChanges();
        }

        public void Delete(Comment comment, short deletedBy)
        {
            // Soft delete with tracking
            comment.IsDeleted = true;
            comment.DeletedBy = deletedBy;
            comment.DeletedAt = DateTime.Now;
            _ctx.Comments.Update(comment);
            _ctx.SaveChanges();
        }
    }
}
