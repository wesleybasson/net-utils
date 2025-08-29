using ApiX.Security.Cryptography.Hashing;
using ApiX.Security.Tests.TestHelpers;

namespace ApiX.Security.Tests.Password;

public class PasswordHasherFactoryTests
{
    private static IPasswordHasherFactory MakeFactory(PasswordHashingOptions options)
    {
        var pb = new Pbkdf2PasswordHasher(OptionsFactory.Create(options));
        var ar = new Argon2idPasswordHasher(OptionsFactory.Create(options));
        return new PasswordHasherFactory(OptionsFactory.Create(options), pb, ar);
    }

    [Fact]
    public void Factory_Create_Uses_Configured_Default()
    {
        var opts = new PasswordHashingOptions { Algorithm = PasswordAlgorithm.Argon2id };
        var factory = MakeFactory(opts);
        var hasher = factory.Create();

        var hash = hasher.Hash("pw");
        Assert.StartsWith("APX$2$ARGON2ID$", hash);
    }

    [Fact]
    public void TryDetect_Detects_Algorithm()
    {
        // Build both implementations directly
        var pbkdf2 = new Pbkdf2PasswordHasher(OptionsFactory.Create(new PasswordHashingOptions()));
        var argon2 = new Argon2idPasswordHasher(OptionsFactory.Create(new PasswordHashingOptions()));

        var pbkHash = pbkdf2.Hash("pw");
        var arHash = argon2.Hash("pw");

        var opts = new PasswordHashingOptions { Algorithm = PasswordAlgorithm.Argon2id };
        var factory = MakeFactory(opts);

        Assert.True(factory.TryDetect(pbkHash, out var a1));
        Assert.Equal(PasswordAlgorithm.Pbkdf2Sha256, a1);

        Assert.True(factory.TryDetect(arHash, out var a2));
        Assert.Equal(PasswordAlgorithm.Argon2id, a2);
    }

    [Fact]
    public void Migration_Flow_Verify_With_Old_Alg_Then_Rehash_To_Default()
    {
        // Suppose legacy users have PBKDF2 hashes, but new default is Argon2id
        var legacyOpts = new PasswordHashingOptions { Algorithm = PasswordAlgorithm.Pbkdf2Sha256 };
        var legacy = new Pbkdf2PasswordHasher(OptionsFactory.Create(legacyOpts));

        var pwd = "Sup3r$ecret!";
        var legacyStored = legacy.Hash(pwd);

        var modernOpts = new PasswordHashingOptions { Algorithm = PasswordAlgorithm.Argon2id };
        var factory = MakeFactory(modernOpts);

        // App logic: detect algorithm, pick correct verifier
        Assert.True(factory.TryDetect(legacyStored, out var detected));
        Assert.Equal(PasswordAlgorithm.Pbkdf2Sha256, detected);

        var pbkdf2 = new Pbkdf2PasswordHasher(OptionsFactory.Create(modernOpts));

        Assert.True(pbkdf2.Verify(pwd, legacyStored, out var needsRehash));
        Assert.True(needsRehash); // because default is Argon2id now (policy wants upgrade)

        // Perform upgrade
        var argon2 = new Argon2idPasswordHasher(OptionsFactory.Create(modernOpts));
        var upgraded = argon2.Hash(pwd);

        // Sanity: new default hasher (Argon2id) should verify the upgraded value
        var defaultHasher = factory.Create();
        Assert.True(defaultHasher.Verify(pwd, upgraded, out var nr2));
        Assert.False(nr2);
    }
}
