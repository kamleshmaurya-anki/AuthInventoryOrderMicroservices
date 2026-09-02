using AuthService.DTOs;

namespace AuthService.Services;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<UserResponse> GetByIdAsync(Guid userId);
}
