namespace ApiX.Security.Cryptography.Hashing;

/// <summary>
/// Enumerates the supported password hashing algorithms.
/// </summary>
public enum PasswordAlgorithm
{
    /// <summary>
    /// Password-Based Key Derivation Function 2 (PBKDF2) with HMAC-SHA256.
    /// </summary>
    Pbkdf2Sha256 = 1,

    /// <summary>
    /// Argon2id, the hybrid variant of Argon2 combining data-dependent
    /// and data-independent memory access for resistance against GPU and
    /// side-channel attacks.
    /// </summary>
    Argon2id = 2
}
