using Microsoft.EntityFrameworkCore;
using AuthService.Data;
using AuthService.Entities;

namespace AuthService.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByUsernameAsync(string username)
    {
        return _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public Task<User?> GetByIdAsync(Guid userId)
    {
        return _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public Task<bool> UsernameExistsAsync(string username)
    {
        return _context.Users.AsNoTracking().AnyAsync(u => u.Username == username);
    }
}
