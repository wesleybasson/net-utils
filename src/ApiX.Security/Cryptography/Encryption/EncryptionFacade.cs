using System.Security.Cryptography;

namespace ApiX.Security.Cryptography.Encryption;

/// <summary>
/// Tries AES-GCM first; if it fails and a legacy decryptor is configured, falls back to legacy CBC.
/// </summary>
public sealed class EncryptionFacade : IAuthenticatedEncryptor
{
    private readonly IAuthenticatedEncryptor _primary;          // GCM
    private readonly LegacyAesCbcDecryptor? _legacy;            // optional

    /// <summary>
    /// 
    /// </summary>
    /// <param name="primary"></param>
    /// <param name="legacy"></param>
    public EncryptionFacade(IAuthenticatedEncryptor primary, LegacyAesCbcDecryptor? legacy = null)
    {
        _primary = primary;
        _legacy = legacy;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="plaintext"></param>
    /// <param name="aad"></param>
    /// <returns></returns>
    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad = default) =>
        _primary.Encrypt(plaintext, aad);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="envelope"></param>
    /// <param name="aad"></param>
    /// <returns></returns>
    public byte[] Decrypt(ReadOnlySpan<byte> envelope, ReadOnlySpan<byte> aad = default) =>
        _primary.Decrypt(envelope, aad);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="plaintext"></param>
    /// <param name="aad"></param>
    /// <returns></returns>
    public string EncryptToBase64Url(string plaintext, ReadOnlySpan<byte> aad = default) =>
        _primary.EncryptToBase64Url(plaintext, aad);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="b64url"></param>
    /// <param name="aad"></param>
    /// <returns></returns>
    public string DecryptFromBase64Url(string b64url, ReadOnlySpan<byte> aad = default)
    {
        try
        {
            return _primary.DecryptFromBase64Url(b64url, aad);
        }
        catch (CryptographicException) when (_legacy is not null)
        {
            // Legacy values were plain Base64 (not Base64Url + envelope)
            return _legacy!.DecryptFromBase64(b64url);
        }
    }
}
