namespace ApiX.Security.Cryptography.Encryption;

/// <summary>
/// Configuration options for AEAD encryption (AES-GCM).
/// </summary>
public sealed class AeadEncryptionOptions
{
    /// <summary>
    /// Raw AES key (16/24/32 bytes). Prefer 32 bytes (AES-256).
    /// </summary>
    public byte[]? RawKey { get; set; }

    /// <summary>
    /// Hex representation of RawKey (alternative to <see cref="RawKey"/>).
    /// </summary>
    public string? RawKeyHex { get; set; }

    /// <summary>
    /// Optional KDF secret. If provided and RawKey/RawKeyHex are null, the runtime derives a process key with PBKDF2.
    /// </summary>
    public string? KdfSecret { get; set; }

    /// <summary>
    /// PBKDF2 iteration count when deriving a key from <see cref="KdfSecret"/>.
    /// </summary>
    public int KdfIterations { get; set; } = 200_000;

    /// <summary>
    /// Size of the nonce (IV) in bytes. 12 is recommended for AES-GCM.
    /// </summary>
    public int NonceSize { get; set; } = 12;

    /// <summary>
    /// Authentication tag size in bytes. 16 (128-bit) is standard.
    /// </summary>
    public int TagSize { get; set; } = 16;

    /// <summary>
    /// Envelope format version for future migrations.
    /// </summary>
    public byte Version { get; set; } = 1;
}
