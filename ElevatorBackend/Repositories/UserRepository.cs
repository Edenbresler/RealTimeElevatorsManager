using Dapper;
using ElevatorBackend.Data;
using ElevatorBackend.Models;
using System.Data;

namespace ElevatorBackend.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DapperContext _context;

        public UserRepository(DapperContext context)
        {
            _context = context;
        }

        // Get user by email (used for checking if user already exists)
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var query = "SELECT * FROM Users WHERE Email = @Email";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(query, new { Email = email });
        }

        // Add a new user to the database
        public async Task AddUserAsync(User user)
        {
            var query = "INSERT INTO Users (Email, Password) VALUES (@Email, @Password)";
            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync(query, user);
        }

        // Authenticate user by email and password
        public async Task<User?> LoginAsync(string email, string password)
        {
            var query = "SELECT * FROM Users WHERE Email = @Email AND Password = @Password";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(query, new { Email = email, Password = password });
        }
    }
}
