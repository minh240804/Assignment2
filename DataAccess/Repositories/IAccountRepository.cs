using System.Collections.Generic;
using VuQuangMinh_ass2_He180094.DataAccess.Models;

namespace VuQuangMinh_ass2_He180094.DataAccess.Repositories
{
    public interface IAccountRepository
    {
        IEnumerable<SystemAccount> GetAll(int? role = null);
        SystemAccount? Get(short id);
        SystemAccount? GetByEmail(string email);
        void Add(SystemAccount acc);
        void Update(SystemAccount acc);
        void Delete(SystemAccount acc);
        public short GetNextId();
    }
}