using Microsoft.Extensions.Options;

namespace ApiX.Security.Cryptography.Hashing;

/// <summary>
/// Default implementation of <see cref="IPasswordHasherFactory"/> that
/// provides configured password hasher instances and algorithm detection.
/// </summary>
/// <remarks>
/// This factory uses <see cref="PasswordHashingOptions"/> to determine which
/// algorithm should be returned by <see cref="Create"/>. It also exposes
/// <see cref="TryDetect"/> to identify the algorithm used in an existing stored hash.
/// </remarks>
public sealed class PasswordHasherFactory : IPasswordHasherFactory
{
    private readonly PasswordHashingOptions _opts;
    private readonly Pbkdf2PasswordHasher _pbkdf2;
    private readonly Argon2idPasswordHasher _argon2id;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordHasherFactory"/> class.
    /// </summary>
    /// <param name="opts">The configured <see cref="PasswordHashingOptions"/>.</param>
    /// <param name="pbkdf2">The PBKDF2 hasher implementation.</param>
    /// <param name="argon2id">The Argon2id hasher implementation.</param>
    public PasswordHasherFactory(
        IOptions<PasswordHashingOptions> opts,
        Pbkdf2PasswordHasher pbkdf2,
        Argon2idPasswordHasher argon2id)
    {
        _opts = opts.Value;
        _pbkdf2 = pbkdf2;
        _argon2id = argon2id;
    }

    /// <summary>
    /// Creates the currently configured default <see cref="IPasswordHasher"/>.
    /// </summary>
    /// <returns>An <see cref="IPasswordHasher"/> based on the configured algorithm.</returns>
    public IPasswordHasher Create() =>
        _opts.Algorithm switch
        {
            PasswordAlgorithm.Pbkdf2Sha256 => _pbkdf2,
            PasswordAlgorithm.Argon2id => _argon2id,
            _ => _argon2id
        };

    /// <summary>
    /// Attempts to detect which password hashing algorithm a stored hash uses.
    /// </summary>
    /// <param name="stored">The stored hash string to inspect.</param>
    /// <param name="alg">
    /// When this method returns <c>true</c>, contains the detected <see cref="PasswordAlgorithm"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the stored hash could be parsed and mapped to a known algorithm;
    /// otherwise <c>false</c>.
    /// </returns>
    public bool TryDetect(string stored, out PasswordAlgorithm alg)
    {
        var ok = PasswordHasherInspector.TryParseHeader(stored, out var detected, out _);
        alg = detected;
        return ok;
    }
}
