using ApiX.Security.Cryptography.Encryption;
using System.Security.Cryptography;
using System.Text;

namespace ApiX.Security.Tests.Cryptography.Encryption;

public class AesGcmEncryptorTests
{
    private static AesGcmEncryptor Create()
    {
        var opt = new AeadEncryptionOptions
        {
            RawKey = RandomNumberGenerator.GetBytes(32),
            NonceSize = 12,
            TagSize = 16,
            Version = 1
        };
        return new AesGcmEncryptor(opt);
    }

    [Fact]
    public void Roundtrip_String_Succeeds()
    {
        var enc = Create();
        var plaintext = "Wesley <wesley@example.com>";
        var b64 = enc.EncryptToBase64Url(plaintext);
        var back = enc.DecryptFromBase64Url(b64);
        Assert.Equal(plaintext, back);
    }

    [Fact]
    public void Roundtrip_WithAAD_Succeeds()
    {
        var enc = Create();
        var data = "PII: 082-123-1234";
        var aad = Encoding.UTF8.GetBytes("route:/customers POST");
        var env = enc.Encrypt(Encoding.UTF8.GetBytes(data), aad);
        var back = enc.Decrypt(env, aad);
        Assert.Equal(data, Encoding.UTF8.GetString(back));
    }

    [Fact]
    public void Tamper_Detects()
    {
        var enc = Create();
        var env = enc.Encrypt("hello"u8);
        // Flip a bit in tag/cipher
        env[^1] ^= 0x01;
        Assert.ThrowsAny<CryptographicException>(() => enc.Decrypt(env));
    }

    [Fact]
    public void Nonce_IsRandom_ProducesDifferentCiphertexts()
    {
        var enc = Create();
        var a = enc.EncryptToBase64Url("same");
        var b = enc.EncryptToBase64Url("same");
        Assert.NotEqual(a, b);
    }
}
