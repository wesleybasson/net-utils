using ApiX.Security.Cryptography.Hashing;
using Microsoft.Extensions.Options;

namespace ApiX.Security.Tests.Password;

public class Pbkdf2PasswordHasherTests
{
    private static Pbkdf2PasswordHasher Make(int iterations = 50_000, int salt = 16, int hash = 32)
    {
        var opts = new PasswordHashingOptions
        {
            Algorithm = PasswordAlgorithm.Pbkdf2Sha256,
            Pbkdf2 = new Pbkdf2Options
            {
                Iterations = iterations,
                SaltSize = salt,
                HashSize = hash
            }
        };
        return new Pbkdf2PasswordHasher(Options.Create(opts));
    }

    [Fact]
    public void Hash_And_Verify_Succeeds()
    {
        var hasher = Make();
        var pwd = "Sup3r$ecret!";
        var stored = hasher.Hash(pwd);

        Assert.True(hasher.Verify(pwd, stored, out var needsRehash));
        Assert.False(needsRehash); // matches current policy
        Assert.StartsWith("APX$1$PBKDF2-SHA256$", stored);
    }

    [Fact]
    public void Verify_Fails_When_Tampered()
    {
        var hasher = Make();
        var pwd = "Sup3r$ecret!";
        var stored = hasher.Hash(pwd);

        // Tamper one character in the stored string (hash segment)
        var tampered = stored.Replace("h=", "h=X", comparisonType: StringComparison.Ordinal);
        Assert.False(hasher.Verify(pwd, tampered, out _));
    }

    [Theory]
    [InlineData("not-base64")]
    [InlineData("APX$1$PBKDF2-SHA256$iter=abc$s=AA$h=BB")] // bad iter
    [InlineData("APX$1$PBKDF2-SHA256$iter=1")] // incomplete
    public void Verify_False_On_Malformed(string stored)
    {
        var hasher = Make();
        Assert.False(hasher.Verify("x", stored, out _));
    }

    [Fact]
    public void Verify_Signals_Rehash_When_Iterations_Too_Low()
    {
        var weakHasher = Make(iterations: 20_000);
        var pwd = "Sup3r$ecret!";
        var storedWeak = weakHasher.Hash(pwd);

        // New policy is stronger
        var strongHasher = Make(iterations: 200_000);

        // The hasher verifies only PBKDF2 APX strings; ok should be true,
        // and needsRehash should be true because policy got stronger.
        Assert.True(strongHasher.Verify(pwd, storedWeak, out var needsRehash));
        Assert.True(needsRehash);
    }
}
