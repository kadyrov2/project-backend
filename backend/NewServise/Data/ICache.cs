using Service.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    public interface ICache
    {
        IEnumerable<User> GetAllUsers();
        User? GetUserById(int id);
        void AddUser(User User);
        User? UpdateUser(User User);
        User? DeleteUser(int id);
    }
}
