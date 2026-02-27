using Lingarr.Server.Interfaces.Services;
using Microsoft.AspNetCore.DataProtection;

namespace Lingarr.Server.Services;

public class EncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;

    public EncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Lingarr");
    }

    public string Encrypt(string value) => _protector.Protect(value);
    public string Decrypt(string value) => _protector.Unprotect(value);
}
