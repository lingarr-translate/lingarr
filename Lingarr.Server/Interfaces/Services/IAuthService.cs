using Lingarr.Core.Entities;

namespace Lingarr.Server.Interfaces.Services;

public interface IAuthService
{
    string GenerateApiKey();
    bool VerifyPassword(string password, string passwordHash);
    Task<User?> GetUserByUsername(string username);
    Task<User> CreateUser(string username, string password);
    Task<bool> HasAnyUsers();
    Task<bool> ValidateApiKey(string apiKey);
}
