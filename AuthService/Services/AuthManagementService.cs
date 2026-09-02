using AuthService.DTOs;
using AuthService.Entities;
using AuthService.Exceptions;
using AuthService.Repositories;
using AuthService.Security;
using Shared.Constants;
using Shared.Exceptions;

namespace AuthService.Services;

public class AuthManagementService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<AuthManagementService> _logger;

    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        Roles.Admin, Roles.User
    };

    public AuthManagementService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<AuthManagementService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.UsernameExistsAsync(request.Username))
        {
            throw new DuplicateUsernameException(request.Username);
        }

        var role = string.IsNullOrWhiteSpace(request.Role) ? Roles.User : request.Role.Trim().ToUpperInvariant();
        if (!ValidRoles.Contains(role))
        {
            throw new InvalidRoleException(request.Role ?? string.Empty);
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _userRepository.AddAsync(user);
        _logger.LogInformation("Registered new user {Username} with role {Role}", created.Username, created.Role);

        return Map(created);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for username {Username}", request.Username);
            throw new InvalidCredentialsException();
        }

        var (token, expiresAtUtc) = _jwtTokenGenerator.GenerateToken(user);
        _logger.LogInformation("User {Username} logged in", user.Username);

        return new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.UserId,
            Username = user.Username,
            Role = user.Role
        };
    }

    public async Task<UserResponse> GetByIdAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundAppException($"User with id '{userId}' was not found.");
        }

        return Map(user);
    }

    private static UserResponse Map(User user) => new()
    {
        UserId = user.UserId,
        Username = user.Username,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}
