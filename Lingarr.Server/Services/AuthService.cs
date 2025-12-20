using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services;

public class AuthService : IAuthService
{
    private readonly LingarrDbContext _context;
    private readonly ISettingService _settingService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(LingarrDbContext context, ISettingService settingService, ILogger<AuthService> logger)
    {
        _context = context;
        _settingService = settingService;
        _logger = logger;
    }

    public string GenerateApiKey()
    {
        var bytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("/", "_").Replace("+", "-").Substring(0, 43);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            var parts = passwordHash.Split(':');
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = parts[1];

            var hashToVerify = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return hashToVerify == storedHash;
        }
        catch
        {
            return false;
        }
    }

    public async Task<User?> GetUserByUsername(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User> CreateUser(string username, string password)
    {
        var user = new User
        {
            Username = username,
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<bool> ValidateApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        var storedApiKey = await _settingService.GetSetting(SettingKeys.Authentication.ApiKey);
        if (string.IsNullOrWhiteSpace(storedApiKey))
            return false;

        return apiKey == storedApiKey;
    }

    private static string HashPassword(string password)
    {
        // random salt
        var salt = new byte[128 / 8];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

        return $"{Convert.ToBase64String(salt)}:{hashed}";
    }
}
