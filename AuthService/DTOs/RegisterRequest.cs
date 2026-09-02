using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    // Optional. Defaults to USER. In a hardened deployment this would be
    // removed entirely from public registration and admin accounts would be
    // seeded/created by an existing admin instead - see README.
    public string? Role { get; set; }
}
