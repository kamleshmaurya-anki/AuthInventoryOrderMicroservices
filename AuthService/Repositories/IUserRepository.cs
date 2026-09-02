using AuthService.Entities;

namespace AuthService.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(Guid userId);
    Task<User> AddAsync(User user);
    Task<bool> UsernameExistsAsync(string username);
}
