using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VuQuangMinh_ass2_He180094.DataAccess.Models;

namespace VuQuangMinh_ass2_He180094.DataAccess.Repositories
{
    public interface ICategoryRepository
    {
        IEnumerable<Category> GetAll(bool? active = null);
        Category? Get(short id);
        void Add(Category cat);
        void Update(Category cat);
        void Delete(Category cat);
        bool HasNews(short categoryId);



    }
}
