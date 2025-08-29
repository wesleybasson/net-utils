using System.Security.Cryptography;
using System.Text;

namespace ApiX.Security.Cryptography.Encryption;

/// <summary>
/// Legacy AES-CBC decryptor for migrating old ciphertexts. Not for new encryptions.
/// </summary>
public sealed class LegacyAesCbcDecryptor
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    /// <summary>
    /// Creates a decryptor using fixed key and IV (legacy).
    /// </summary>
    public LegacyAesCbcDecryptor(string keyHex, string ivHex)
    {
        _key = Convert.FromHexString(keyHex ?? throw new ArgumentNullException(nameof(keyHex)));
        _iv = Convert.FromHexString(ivHex ?? throw new ArgumentNullException(nameof(ivHex)));
    }

    /// <summary>
    /// Decrypts a Base64 string produced by the legacy CBC helper.
    /// </summary>
    public string DecryptFromBase64(string legacyBase64)
    {
        var encryptedBytes = Convert.FromBase64String(legacyBase64);
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        using var ms = new MemoryStream(encryptedBytes);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);
        return sr.ReadToEnd();
    }
}
