using ApiX.Security.Cryptography.Hashing;
using Microsoft.Extensions.Options;

namespace ApiX.Security.Tests.Password;

public class Argon2idPasswordHasherTests
{
    private static Argon2idPasswordHasher Make(
        int memoryKb = 32 * 1024, // use lower defaults in tests to keep them fast
        int iterations = 2,
        int parallelism = 2,
        int salt = 16,
        int hash = 32)
    {
        var opts = new PasswordHashingOptions
        {
            Algorithm = PasswordAlgorithm.Argon2id,
            Argon2Id = new Argon2idOptions
            {
                MemoryKb = memoryKb,
                Iterations = iterations,
                Parallelism = parallelism,
                SaltSize = salt,
                HashSize = hash
            }
        };
        return new Argon2idPasswordHasher(Options.Create(opts));
    }

    [Fact]
    public void Hash_And_Verify_Succeeds()
    {
        var hasher = Make();
        var pwd = "Sup3r$ecret!";
        var stored = hasher.Hash(pwd);

        Assert.True(hasher.Verify(pwd, stored, out var needsRehash));
        Assert.False(needsRehash);
        Assert.StartsWith("APX$2$ARGON2ID$", stored);
    }

    [Fact]
    public void Verify_Fails_When_Tampered()
    {
        var hasher = Make();
        var pwd = "Sup3r$ecret!";
        var stored = hasher.Hash(pwd);

        // Tamper memory param
        var tampered = stored.Replace("m=", "m=9999999", comparisonType: StringComparison.Ordinal);
        Assert.False(hasher.Verify(pwd, tampered, out _));
    }

    [Theory]
    [InlineData("APX$2$ARGON2ID$m=foo,t=3,p=2$sl=16,hl=32$s=AA$h=BB")]      // bad memory
    [InlineData("APX$2$ARGON2ID$m=65536,t=3,p=2$sl=16,hl=32")]              // missing s/h
    [InlineData("garbage")]
    public void Verify_False_On_Malformed(string stored)
    {
        var hasher = Make();
        Assert.False(hasher.Verify("x", stored, out _));
    }

    [Fact]
    public void Verify_Signals_Rehash_When_Params_Weaker()
    {
        var weak = Make(memoryKb: 8 * 1024, iterations: 1, parallelism: 1);
        var strong = Make(memoryKb: 32 * 1024, iterations: 2, parallelism: 2);

        var pwd = "Sup3r$ecret!";
        var storedWeak = weak.Hash(pwd);

        Assert.True(strong.Verify(pwd, storedWeak, out var needsRehash));
        Assert.True(needsRehash);
    }
}
