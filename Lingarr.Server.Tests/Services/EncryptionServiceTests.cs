using Lingarr.Server.Services;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class EncryptionServiceTests
{
    private readonly EncryptionService _encryptionService = new(new EphemeralDataProtectionProvider());

    [Fact]
    public void IsEncrypted_EncryptedValue_ReturnsTrue()
    {
        var encrypted = _encryptionService.Encrypt("sk-secret-key");

        Assert.True(_encryptionService.IsEncrypted(encrypted));
    }

    [Theory]
    [InlineData("sk-secret-key")]
    [InlineData("abcde")]
    [InlineData("")]
    public void IsEncrypted_PlaintextValue_ReturnsFalse(string value)
    {
        Assert.False(_encryptionService.IsEncrypted(value));
    }
}
