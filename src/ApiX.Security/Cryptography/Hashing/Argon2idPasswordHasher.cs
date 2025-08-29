using Microsoft.Extensions.Options;

namespace ApiX.Security.Cryptography.Hashing;

/// <summary>
/// Provides password hashing and verification using the Argon2id algorithm.
/// <para>
/// This implementation leverages configuration supplied through 
/// <see cref="PasswordHashingOptions"/> to determine Argon2id parameters 
/// such as memory cost, iterations, parallelism, salt size, and hash size.
/// </para>
/// </summary>
public sealed class Argon2idPasswordHasher : IPasswordHasher
{
    private readonly PasswordHashingOptions _root;

    /// <summary>
    /// Initializes a new instance of the <see cref="Argon2idPasswordHasher"/> class
    /// using the specified hashing options.
    /// </summary>
    /// <param name="opts">
    /// A configuration snapshot containing <see cref="PasswordHashingOptions"/>,
    /// which defines Argon2id parameters and the algorithm to use.
    /// </param>
    public Argon2idPasswordHasher(IOptions<PasswordHashingOptions> opts) => _root = opts.Value;

    /// <summary>
    /// Generates an Argon2id hash for the specified plain-text password.
    /// </summary>
    /// <param name="password">The plain-text password to hash.</param>
    /// <returns>
    /// A formatted string containing the Argon2id hash, including algorithm 
    /// identifier, parameters, salt, and derived key.
    /// </returns>
    public string Hash(string password)
    {
        var o = _root.Argon2Id;
        return password.HashPassword(
            memoryKb: o.MemoryKb,
            iterations: o.Iterations,
            parallelism: o.Parallelism,
            saltSize: o.SaltSize,
            hashSize: o.HashSize
        ); // uses your Argon2id helper (versioned APX$2...)
    }

    /// <summary>
    /// Verifies that a provided plain-text password matches a previously 
    /// generated Argon2id hash.
    /// </summary>
    /// <param name="password">The plain-text password to verify.</param>
    /// <param name="stored">The stored hash string to compare against.</param>
    /// <param name="needsRehash">
    /// Set to <c>true</c> if the password was valid but the stored hash was 
    /// generated with weaker parameters than the current configuration, 
    /// indicating that the password should be re-hashed; otherwise <c>false</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the password matches the stored hash; otherwise <c>false</c>.
    /// </returns>
    public bool Verify(string password, string stored, out bool needsRehash)
    {
        needsRehash = false;

        if (!PasswordHasherInspector.TryParseHeader(stored, out var alg, out var meta) || alg != PasswordAlgorithm.Argon2id)
            return false;

        var ok = PasswordHashHelperArgon2id.VerifyPassword(password, stored);

        if (ok && meta is Argon2idMeta a)
        {
            var target = _root.Argon2Id;
            if (a.MemoryKb < target.MemoryKb || a.Iterations < target.Iterations || a.Parallelism < target.Parallelism
                || a.SaltLen != target.SaltSize || a.HashLen != target.HashSize
                || _root.Algorithm != PasswordAlgorithm.Argon2id)
            {
                needsRehash = true;
            }
        }

        return ok;
    }
}
