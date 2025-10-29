using System.Collections.Generic;
using VuQuangMinh_ass2_He180094.DataAccess.Models;

namespace VuQuangMinh_ass2_He180094.BusinessLogic
{
    public interface IAccountService
    {
        IEnumerable<SystemAccount> GetAll(int? role = null);
        SystemAccount? Get(short id);
        SystemAccount? Login(string email, string password);
        void Add(SystemAccount acc, string password);
        void Update(SystemAccount acc, string? newPassword = null);
        (bool Success, string Message) Delete(short id);
        bool ExistsEmail(string? email);
    }
}