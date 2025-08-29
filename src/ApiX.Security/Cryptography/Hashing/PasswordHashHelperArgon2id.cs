using System.Runtime.CompilerServices;
using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

[assembly: InternalsVisibleTo("ApiX.Security.Tests")]

namespace ApiX.Security.Cryptography.Hashing;

internal static class PasswordHashHelperArgon2id
{
    // Sensible defaults—tune on your hardware. Aim ~100ms per hash on your target servers.

    public const int DefaultMemoryKb = 64 * 1024; // 64 MiB
    public const int DefaultIterations = 3;       // time cost
    public const int DefaultParallelism = 2;      // lanes/threads
    public const int DefaultSaltSize = 16;        // 128-bit
    public const int DefaultHashSize = 32;        // 256-bit

    // APX format: APX$2$ARGON2ID$m=<KB>,t=<iters>,p=<lanes>$sl=<saltLen>,hl=<hashLen>$s=<b64>$h=<b64>
    private const string Prefix = "APX";
    private const string Version = "2";
    private const string Alg = "ARGON2ID";

    /// <summary>
    /// Hashes a password using Argon2id with a random salt, returning a versioned, self-describing APX string.
    /// </summary>
    public static string HashPassword(
        this string password,
        int? memoryKb = null,
        int? iterations = null,
        int? parallelism = null,
        int? saltSize = null,
        int? hashSize = null)
    {
        int m = memoryKb ?? DefaultMemoryKb;
        int t = iterations ?? DefaultIterations;
        int p = parallelism ?? DefaultParallelism;
        int sl = saltSize ?? DefaultSaltSize;
        int hl = hashSize ?? DefaultHashSize;

        if (m <= 0 || t <= 0 || p <= 0 || sl < 8 || hl < 16) // minimal sanity gates
            throw new ArgumentOutOfRangeException("Invalid Argon2id parameter(s).");

        byte[] salt = new byte[sl];
        RandomNumberGenerator.Fill(salt);

        byte[] pwdBytes = Encoding.UTF8.GetBytes(password);
        using var argon2 = new Argon2id(pwdBytes)
        {
            Salt = salt,
            MemorySize = m,               // in KiB
            Iterations = t,
            DegreeOfParallelism = p
        };

        byte[] hash = argon2.GetBytes(hl);

        string sB64 = Convert.ToBase64String(salt);
        string hB64 = Convert.ToBase64String(hash);

        return $"{Prefix}${Version}${Alg}$m={m},t={t},p={p}$sl={sl},hl={hl}$s={sB64}$h={hB64}";
    }

    /// <summary>
    /// Verifies a password against an APX Argon2id string. Returns false on mismatch or malformed input.
    /// </summary>
    public static bool VerifyPassword(this string password, string stored)
    {
        try
        {
            // Expect: APX$2$ARGON2ID$m=...,t=...,p=...$sl=...,hl=...$s=...$h=...
            var parts = stored.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 7) return false;
            if (!parts[0].Equals(Prefix, StringComparison.Ordinal)) return false;
            if (!parts[1].Equals(Version, StringComparison.Ordinal)) return false;
            if (!parts[2].Equals(Alg, StringComparison.Ordinal)) return false;

            // Params group 1: m=...,t=...,p=...
            int m = 0, t = 0, p = 0;
            if (!TryParseTriplet(parts[3], out m, out t, out p)) return false;

            // Params group 2: sl=...,hl=...
            if (!TryParseLengths(parts[4], out int sl, out int hl)) return false;

            // Base64 segments: s=..., h=...
            if (!parts[5].StartsWith("s=", StringComparison.Ordinal) &&
                !parts[6].StartsWith("h=", StringComparison.Ordinal))
            {
                // Defensive: though we expect $s=...$h=..., enforce below when extracting.
            }

            if (!parts[5].StartsWith("s=", StringComparison.Ordinal)) return false;
            if (!parts[6].StartsWith("h=", StringComparison.Ordinal)) return false;

            var sB64 = parts[5].Substring(2);
            var hB64 = parts[6].Substring(2);

            var salt = Convert.FromBase64String(sB64);
            var expected = Convert.FromBase64String(hB64);

            if (salt.Length != sl || expected.Length != hl) return false;
            if (m <= 0 || t <= 0 || p <= 0 || sl < 8 || hl < 16) return false;

            byte[] pwdBytes = Encoding.UTF8.GetBytes(password);
            using var argon2 = new Argon2id(pwdBytes)
            {
                Salt = salt,
                MemorySize = m,
                Iterations = t,
                DegreeOfParallelism = p
            };

            byte[] actual = argon2.GetBytes(hl);

            // constant-time compare
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch
        {
            return false; // malformed inputs or decoding failures => not a match
        }
    }

    private static bool TryParseTriplet(string s, out int m, out int t, out int p)
    {
        m = t = p = 0;
        // Expect "m=...,t=...,p=..."
        var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) return false;

        bool okM = parts[0].StartsWith("m=", StringComparison.Ordinal) && int.TryParse(parts[0].AsSpan(2), out m);
        bool okT = parts[1].StartsWith("t=", StringComparison.Ordinal) && int.TryParse(parts[1].AsSpan(2), out t);
        bool okP = parts[2].StartsWith("p=", StringComparison.Ordinal) && int.TryParse(parts[2].AsSpan(2), out p);
        return okM && okT && okP;
    }

    private static bool TryParseLengths(string s, out int sl, out int hl)
    {
        sl = hl = 0;
        // Expect "sl=...,hl=..."
        var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;

        bool okSL = parts[0].StartsWith("sl=", StringComparison.Ordinal) && int.TryParse(parts[0].AsSpan(3), out sl);
        bool okHL = parts[1].StartsWith("hl=", StringComparison.Ordinal) && int.TryParse(parts[1].AsSpan(3), out hl);
        return okSL && okHL;
    }
}
