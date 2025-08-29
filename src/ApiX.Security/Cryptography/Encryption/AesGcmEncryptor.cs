using System.Security.Cryptography;
using System.Text;

namespace ApiX.Security.Cryptography.Encryption;

/// <summary>
/// AES-GCM implementation with per-message random nonce and versioned envelope.
/// Envelope layout: [ver(1)|alg(1=GCM)|flags(1)|nonceLen(1)|nonce|tag|cipher].
/// </summary>
public sealed class AesGcmEncryptor : IAuthenticatedEncryptor
{
    private readonly byte[] _key; // immutable
    private readonly AeadEncryptionOptions _opt;

    /// <summary>
    /// Creates a new AES-GCM encryptor instance.
    /// </summary>
    public AesGcmEncryptor(AeadEncryptionOptions options)
    {
        _opt = options ?? throw new ArgumentNullException(nameof(options));
        _key = ResolveKey(_opt);
        if (_key.Length is not (16 or 24 or 32))
            throw new ArgumentException("AES key must be 16, 24, or 32 bytes.", nameof(options));
        if (_opt.NonceSize <= 0) throw new ArgumentOutOfRangeException(nameof(options.NonceSize));
        if (_opt.TagSize <= 0) throw new ArgumentOutOfRangeException(nameof(options.TagSize));
    }

    /// <inheritdoc />
    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad = default)
    {
        var nonce = RandomNumberGenerator.GetBytes(_opt.NonceSize);
        var cipher = new byte[plaintext.Length];
        var tag = new byte[_opt.TagSize];

        using var gcm = new AesGcm(_key, _opt.TagSize);
        gcm.Encrypt(nonce, plaintext, cipher, tag, aad);

        var envelope = new byte[1 + 1 + 1 + 1 + nonce.Length + tag.Length + cipher.Length];
        int o = 0;
        envelope[o++] = _opt.Version;     // ver
        envelope[o++] = 1;                // alg = AES-GCM
        envelope[o++] = 0;                // flags reserved
        envelope[o++] = (byte)nonce.Length;
        Buffer.BlockCopy(nonce, 0, envelope, o, nonce.Length); o += nonce.Length;
        Buffer.BlockCopy(tag, 0, envelope, o, tag.Length); o += tag.Length;
        Buffer.BlockCopy(cipher, 0, envelope, o, cipher.Length);
        return envelope;
    }

    /// <inheritdoc />
    public byte[] Decrypt(ReadOnlySpan<byte> envelope, ReadOnlySpan<byte> aad = default)
    {
        if (envelope.Length < 4) throw new CryptographicException("Envelope too short.");
        int o = 0;
        var ver = envelope[o++];
        var alg = envelope[o++]; if (alg != 1) throw new CryptographicException("Unsupported algorithm.");
        var flags = envelope[o++]; _ = flags;
        var nLen = envelope[o++];

        if (envelope.Length < 4 + nLen + _opt.TagSize) throw new CryptographicException("Envelope malformed.");
        var nonce = envelope.Slice(o, nLen).ToArray(); o += nLen;
        var tag = envelope.Slice(o, _opt.TagSize).ToArray(); o += _opt.TagSize;
        var cipher = envelope[o..].ToArray();

        var plain = new byte[cipher.Length];
        using var gcm = new AesGcm(_key, _opt.TagSize);
        gcm.Decrypt(nonce, cipher, tag, plain, aad);
        return plain;
    }

    /// <inheritdoc />
    public string EncryptToBase64Url(string plaintext, ReadOnlySpan<byte> aad = default)
    {
        var bytes = Encrypt(Encoding.UTF8.GetBytes(plaintext), aad);
        return Base64UrlEncode(bytes);
    }

    /// <inheritdoc />
    public string DecryptFromBase64Url(string b64url, ReadOnlySpan<byte> aad = default)
    {
        var bytes = Base64UrlDecode(b64url);
        var plain = Decrypt(bytes, aad);
        return Encoding.UTF8.GetString(plain);
    }

    private static byte[] ResolveKey(AeadEncryptionOptions opt)
    {
        if (opt.RawKey is { Length: > 0 }) return (byte[])opt.RawKey.Clone();
        if (!string.IsNullOrWhiteSpace(opt.RawKeyHex))
            return Convert.FromHexString(opt.RawKeyHex!);

        if (!string.IsNullOrEmpty(opt.KdfSecret))
        {
            var salt = "ApiX.Security.AesGcmEncryptor.v1"u8.ToArray(); // domain separator
            using var pbkdf2 = new Rfc2898DeriveBytes(opt.KdfSecret!, salt, opt.KdfIterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32);
        }

        throw new ArgumentException("Provide RawKey/RawKeyHex or KdfSecret in AeadEncryptionOptions.");
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        var s = Convert.ToBase64String(bytes);
        return s.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
    private static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight((s.Length + 3) & ~3, '='));
    }
}
