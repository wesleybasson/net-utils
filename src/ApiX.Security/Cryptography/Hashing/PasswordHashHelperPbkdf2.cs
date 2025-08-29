using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

[assembly: InternalsVisibleTo("ApiX.Security.Tests")]

namespace ApiX.Security.Cryptography.Hashing;

internal static class PasswordHashHelperPbkdf2
{
    // Tunables – consider flowing from IOptions<SecurityOptions>

    public const int SaltSize = 16;         // 128-bit
    public const int HashSize = 32;         // 256-bit
    public const int DefaultIterations = 200_000; // tune to ~100ms on your servers

    // Format: APX$1$PBKDF2-SHA256$iter=<n>$s=<base64>$h=<base64>
    private const string Prefix = "APX";
    private const string Version = "1";
    private const string Alg = "PBKDF2-SHA256";

    /// <summary>
    /// Hashes a password (PBKDF2-HMAC-SHA256) with random salt in a versioned, self-describing format.
    /// </summary>
    public static string HashPassword(this string password, int? iterations = null)
    {
        var iters = iterations ?? DefaultIterations;

        Span<byte> salt = stackalloc byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        Span<byte> hash = stackalloc byte[HashSize];
        // .NET 6+: Rfc2898DeriveBytes.Pbkdf2
        Rfc2898DeriveBytes.Pbkdf2(
            password: Encoding.UTF8.GetBytes(password),
            salt: salt,
            iterations: iters,
            hashAlgorithm: HashAlgorithmName.SHA256,
            destination: hash);

        // Base64 encode salt & hash
        string sB64 = Convert.ToBase64String(salt.ToArray());
        string hB64 = Convert.ToBase64String(hash.ToArray());

        return $"{Prefix}${Version}${Alg}$iter={iters}$s={sB64}$h={hB64}";
    }

    /// <summary>
    /// Verifies a password against a stored APX format string.
    /// </summary>
    public static bool VerifyPassword(this string password, string stored)
    {
        try
        {
            // Parse: APX$1$PBKDF2-SHA256$iter=...$s=...$h=...
            var parts = stored.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 6) return false;
            if (!parts[0].Equals(Prefix, StringComparison.Ordinal)) return false;
            if (!parts[1].Equals(Version, StringComparison.Ordinal)) return false;
            if (!parts[2].Equals(Alg, StringComparison.Ordinal)) return false;

            if (!parts[3].StartsWith("iter=", StringComparison.Ordinal)) return false;
            if (!int.TryParse(parts[3].AsSpan(5), out int iters) || iters <= 0) return false;

            if (!parts[4].StartsWith("s=", StringComparison.Ordinal)) return false;
            if (!parts[5].StartsWith("h=", StringComparison.Ordinal)) return false;

            var sB64 = parts[4].Substring(2);
            var hB64 = parts[5].Substring(2);

            var salt = Convert.FromBase64String(sB64);
            var expected = Convert.FromBase64String(hB64);

            if (salt.Length < 8 || expected.Length != HashSize) return false;

            Span<byte> actual = stackalloc byte[HashSize];
            Rfc2898DeriveBytes.Pbkdf2(
                password: Encoding.UTF8.GetBytes(password),
                salt: salt,
                iterations: iters,
                hashAlgorithm: HashAlgorithmName.SHA256,
                destination: actual);

            var equal = CryptographicOperations.FixedTimeEquals(expected, actual.ToArray());
            return equal;
        }
        catch
        {
            // Malformed input -> not a match.
            return false;
        }
    }
}
