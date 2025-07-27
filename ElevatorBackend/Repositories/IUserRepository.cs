using ElevatorBackend.Models;
using System.Threading.Tasks;

namespace ElevatorBackend.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task AddUserAsync(User user);

        Task<User?> LoginAsync(string email, string password);
    }
}
