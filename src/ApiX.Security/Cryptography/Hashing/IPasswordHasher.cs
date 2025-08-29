namespace ApiX.Security.Cryptography.Hashing;

/// <summary>
/// Defines a contract for password hashing and verification services.
/// </summary>
/// <remarks>
/// Implementations wrap a specific algorithm (e.g., PBKDF2, Argon2id) and its
/// configuration. Consumers should not hardcode algorithms, but instead rely
/// on <see cref="IPasswordHasherFactory"/> to obtain the configured hasher.
/// </remarks>
public interface IPasswordHasher
{
    /// <summary>
    /// Computes a hash of the specified plain-text password using the
    /// algorithm and parameters configured for this hasher.
    /// </summary>
    /// <param name="password">The plain-text password to hash.</param>
    /// <returns>
    /// A versioned, self-describing hash string containing the algorithm
    /// identifier, parameters, salt, and derived key.
    /// </returns>
    string Hash(string password);

    /// <summary>
    /// Verifies that a plain-text password matches a stored hash string.
    /// </summary>
    /// <param name="password">The plain-text password to verify.</param>
    /// <param name="stored">The stored hash string to compare against.</param>
    /// <param name="needsRehash">
    /// Outputs <c>true</c> if the password matched but the stored hash was
    /// generated with weaker parameters (e.g., lower iterations, smaller salt/hash)
    /// or with a different algorithm than is currently configured, indicating that
    /// the password should be re-hashed; otherwise <c>false</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the password matches the stored hash; otherwise <c>false</c>.
    /// </returns>
    bool Verify(string password, string stored, out bool needsRehash);
}
