using System.Security.Cryptography;
using DevHabit.Api.Configurations;
using DevHabit.Api.Services;
using Microsoft.Extensions.Options;

namespace DevHabit.UnitTests.Services;

public sealed class EncryptionServiceTests
{
    private readonly EncryptionService _sut;

    public EncryptionServiceTests()
    {
        IOptions<EncryptionOptions> options = Options.Create(new EncryptionOptions()
        {
            Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        });

        _sut = new(options);
    }

    [Fact]
    public void Decrypt_ShouldReturnPlainText_WhenDecryptingCorrectCipherText()
    {
        // Arrange
        const string plainText = "sensitive data";
        string cipherText = _sut.Encrypt(plainText);

        // Act
        string decryptedCipherText = _sut.Decrypt(cipherText);

        // Assert
        Assert.Equal(plainText, decryptedCipherText);
    }
}
