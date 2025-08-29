namespace ApiX.Security.Cryptography.Hashing;

/// <summary>
/// Defines a contract for creating password hashers and detecting algorithms.
/// </summary>
/// <remarks>
/// This abstraction centralizes the selection of the current default
/// <see cref="IPasswordHasher"/> and allows detection of which algorithm was used
/// to produce an existing stored hash.
/// </remarks>
public interface IPasswordHasherFactory
{
    /// <summary>
    /// Creates an <see cref="IPasswordHasher"/> based on the currently configured
    /// default algorithm and parameters.
    /// </summary>
    /// <returns>
    /// An <see cref="IPasswordHasher"/> implementation suitable for hashing new passwords.
    /// </returns>
    IPasswordHasher Create();

    /// <summary>
    /// Attempts to detect which algorithm a stored hash string uses.
    /// </summary>
    /// <param name="stored">The stored hash string to inspect.</param>
    /// <param name="alg">
    /// When this method returns <c>true</c>, contains the detected <see cref="PasswordAlgorithm"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the stored hash could be recognized and mapped to a known algorithm;
    /// otherwise <c>false</c>.
    /// </returns>
    bool TryDetect(string stored, out PasswordAlgorithm alg);
}
