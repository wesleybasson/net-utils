namespace ApiX.Security.Cryptography.Hashing;

/// <summary>
/// Represents the configuration options for password hashing.
/// </summary>
/// <remarks>
/// These options determine which <see cref="PasswordAlgorithm"/> is used
/// and define algorithm-specific parameters for PBKDF2 and Argon2id.
/// </remarks>
public sealed class PasswordHashingOptions
{
    /// <summary>
    /// Gets or sets the algorithm to use when hashing new passwords.
    /// Defaults to <see cref="PasswordAlgorithm.Argon2id"/>.
    /// </summary>
    public PasswordAlgorithm Algorithm { get; set; } = PasswordAlgorithm.Argon2id;

    /// <summary>
    /// Gets or sets the PBKDF2-specific parameters.
    /// </summary>
    public Pbkdf2Options Pbkdf2 { get; set; } = new();

    /// <summary>
    /// Gets or sets the Argon2id-specific parameters.
    /// </summary>
    public Argon2idOptions Argon2Id { get; set; } = new();
}

/// <summary>
/// Defines algorithm parameters for PBKDF2-HMAC-SHA256 password hashing.
/// </summary>
public sealed class Pbkdf2Options
{
    /// <summary>
    /// Gets or sets the number of PBKDF2 iterations.
    /// Higher values increase computational cost. Default: <see cref="PasswordHashHelperPbkdf2.DefaultIterations"/>.
    /// </summary>
    public int Iterations { get; set; } = PasswordHashHelperPbkdf2.DefaultIterations;

    /// <summary>
    /// Gets or sets the size of the salt in bytes. Default: <see cref="PasswordHashHelperPbkdf2.SaltSize"/>.
    /// </summary>
    public int SaltSize { get; set; } = PasswordHashHelperPbkdf2.SaltSize;

    /// <summary>
    /// Gets or sets the size of the derived key (hash) in bytes.
    /// Default: <see cref="PasswordHashHelperPbkdf2.HashSize"/>.
    /// </summary>
    public int HashSize { get; set; } = PasswordHashHelperPbkdf2.HashSize;
}

/// <summary>
/// Defines algorithm parameters for Argon2id password hashing.
/// </summary>
public sealed class Argon2idOptions
{
    /// <summary>
    /// Gets or sets the memory cost in KiB (RAM usage).
    /// Default: <see cref="PasswordHashHelperArgon2id.DefaultMemoryKb"/>.
    /// </summary>
    public int MemoryKb { get; set; } = PasswordHashHelperArgon2id.DefaultMemoryKb;

    /// <summary>
    /// Gets or sets the number of iterations (time cost).
    /// Default: <see cref="PasswordHashHelperArgon2id.DefaultIterations"/>.
    /// </summary>
    public int Iterations { get; set; } = PasswordHashHelperArgon2id.DefaultIterations;

    /// <summary>
    /// Gets or sets the degree of parallelism (number of lanes/threads).
    /// Default: <see cref="PasswordHashHelperArgon2id.DefaultParallelism"/>.
    /// </summary>
    public int Parallelism { get; set; } = PasswordHashHelperArgon2id.DefaultParallelism;

    /// <summary>
    /// Gets or sets the salt length in bytes.
    /// Default: <see cref="PasswordHashHelperArgon2id.DefaultSaltSize"/>.
    /// </summary>
    public int SaltSize { get; set; } = PasswordHashHelperArgon2id.DefaultSaltSize;

    /// <summary>
    /// Gets or sets the size of the derived key (hash) in bytes.
    /// Default: <see cref="PasswordHashHelperArgon2id.DefaultHashSize"/>.
    /// </summary>
    public int HashSize { get; set; } = PasswordHashHelperArgon2id.DefaultHashSize;
}
