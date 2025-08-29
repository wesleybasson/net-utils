using Microsoft.Extensions.Options;

namespace ApiX.Security.Cryptography.Hashing;

/// <summary>
/// Provides password hashing and verification using PBKDF2 with HMAC-SHA256.
/// <para>
/// Parameters (iterations, salt size, and hash size) are taken from
/// <see cref="PasswordHashingOptions"/> at construction time.
/// </para>
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private readonly PasswordHashingOptions _root;

    /// <summary>
    /// Initializes a new instance of the <see cref="Pbkdf2PasswordHasher"/> class
    /// using the supplied hashing options.
    /// </summary>
    /// <param name="opts">
    /// Options snapshot containing <see cref="PasswordHashingOptions"/>, including
    /// PBKDF2 parameters and the currently selected <see cref="PasswordAlgorithm"/>.
    /// </param>
    public Pbkdf2PasswordHasher(IOptions<PasswordHashingOptions> opts) => _root = opts.Value;

    /// <summary>
    /// Generates a PBKDF2 (HMAC-SHA256) hash for the specified plain-text password.
    /// </summary>
    /// <param name="password">The plain-text password to hash.</param>
    /// <returns>
    /// A formatted string containing the PBKDF2 hash in the library’s versioned
    /// <c>APX$...</c> format, including algorithm identifier, parameters, salt,
    /// and derived key.
    /// </returns>
    public string Hash(string password)
        => password.HashPassword(_root.Pbkdf2.Iterations);

    /// <summary>
    /// Verifies that a provided plain-text password matches a previously
    /// generated PBKDF2 hash.
    /// </summary>
    /// <param name="password">The plain-text password to verify.</param>
    /// <param name="stored">The stored PBKDF2 hash string to compare against.</param>
    /// <param name="needsRehash">
    /// Set to <c>true</c> if the password was valid but the stored hash was
    /// produced with weaker parameters than the current configuration (e.g.,
    /// fewer iterations, different salt/hash lengths), or if the configured
    /// algorithm has changed—indicating the hash should be re-computed; otherwise <c>false</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the password matches the stored hash; otherwise <c>false</c>.
    /// </returns>
    public bool Verify(string password, string stored, out bool needsRehash)
    {
        needsRehash = false;

        // Fast-fail: if it is not a PBKDF2 APX string, we cannot verify here.
        if (!PasswordHasherInspector.TryParseHeader(stored, out var alg, out var meta) || alg != PasswordAlgorithm.Pbkdf2Sha256)
            return false;

        var ok = PasswordHashHelperPbkdf2.VerifyPassword(password, stored); // your PBKDF2 verify

        // If ok but the stored params are weaker than current policy, mark for rehash.
        if (ok && meta is Pbkdf2Meta p)
        {
            var target = _root.Pbkdf2;
            if (p.Iterations < target.Iterations || p.SaltSize != target.SaltSize || p.HashSize != target.HashSize
                || _root.Algorithm != PasswordAlgorithm.Pbkdf2Sha256)
            {
                needsRehash = true;
            }
        }

        return ok;
    }
}
