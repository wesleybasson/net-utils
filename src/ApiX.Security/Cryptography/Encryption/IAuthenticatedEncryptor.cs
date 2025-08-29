namespace ApiX.Security.Cryptography.Encryption;

/// <summary>
/// Authenticated encryption service (AEAD).
/// </summary>
public interface IAuthenticatedEncryptor
{
    /// <summary>
    /// Encrypts plaintext (bytes) with optional AAD and returns a versioned envelope (bytes).
    /// </summary>
    byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad = default);

    /// <summary>
    /// Decrypts a versioned envelope (bytes) with optional AAD and returns plaintext (bytes).
    /// </summary>
    byte[] Decrypt(ReadOnlySpan<byte> envelope, ReadOnlySpan<byte> aad = default);

    /// <summary>
    /// Encrypts a UTF-8 string and returns a Base64Url-encoded envelope.
    /// </summary>
    string EncryptToBase64Url(string plaintext, ReadOnlySpan<byte> aad = default);

    /// <summary>
    /// Decrypts a Base64Url-encoded envelope and returns a UTF-8 string.
    /// </summary>
    string DecryptFromBase64Url(string b64url, ReadOnlySpan<byte> aad = default);
}
