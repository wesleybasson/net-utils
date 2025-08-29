namespace ApiX.Security.Cryptography.Hashing;

// Lightweight metadata views

/// <summary>
/// Base type for parsed metadata describing the parameters
/// used to generate a stored password hash.
/// </summary>
/// <remarks>
/// Instances of <see cref="HashMeta"/> are produced by the
/// internal <c>PasswordHasherInspector</c> when decoding the
/// self-describing APX hash format.
/// </remarks>
public abstract record HashMeta;

/// <summary>
/// Metadata describing the parameters of a PBKDF2-HMAC-SHA256 hash.
/// </summary>
/// <param name="Iterations">The iteration count used when deriving the key.</param>
/// <param name="SaltSize">The length of the salt in bytes.</param>
/// <param name="HashSize">The length of the derived key (hash) in bytes.</param>
public sealed record Pbkdf2Meta(int Iterations, int SaltSize, int HashSize) : HashMeta;

/// <summary>
/// Metadata describing the parameters of an Argon2id hash.
/// </summary>
/// <param name="MemoryKb">The memory cost in KiB used during hashing.</param>
/// <param name="Iterations">The time cost (number of iterations).</param>
/// <param name="Parallelism">The degree of parallelism (lanes/threads).</param>
/// <param name="SaltLen">The length of the salt in bytes.</param>
/// <param name="HashLen">The length of the derived key (hash) in bytes.</param>
public sealed record Argon2idMeta(int MemoryKb, int Iterations, int Parallelism, int SaltLen, int HashLen) : HashMeta;

internal static class PasswordHasherInspector
{
    // Recognizes:
    // APX$1$PBKDF2-SHA256$iter=<n>$s=<b64>$h=<b64>
    // APX$2$ARGON2ID$m=<KB>,t=<i>,p=<p>$sl=<sl>,hl=<hl>$s=<b64>$h=<b64>
    public static bool TryParseHeader(string stored, out PasswordAlgorithm alg, out HashMeta? meta)
    {
        alg = default;
        meta = null;
        try
        {
            var parts = stored.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4 || parts[0] != "APX") return false;

            var version = parts[1];
            var algName = parts[2];

            if (version == "1" && algName == "PBKDF2-SHA256")
            {
                // PBKDF2: parts[3] = "iter=<n>", later parts carry s= and h=
                if (parts.Length < 6) return false;
                if (!parts[3].StartsWith("iter=", StringComparison.Ordinal)) return false;
                if (!int.TryParse(parts[3].AsSpan(5), out var iters)) return false;

                // We can infer sizes after base64 decode, but keep it quick with a soft parse:
                // Decode base64 lengths for salt/hash quickly:
                var sB64 = parts[4].StartsWith("s=") ? parts[4].Substring(2) :
                           parts[5].StartsWith("s=") ? parts[5].Substring(2) : null;
                var hB64 = parts[4].StartsWith("h=") ? parts[4].Substring(2) :
                           parts[5].StartsWith("h=") ? parts[5].Substring(2) : null;
                if (sB64 is null || hB64 is null) return false;

                var saltLen = Convert.FromBase64String(sB64).Length;
                var hashLen = Convert.FromBase64String(hB64).Length;

                alg = PasswordAlgorithm.Pbkdf2Sha256;
                meta = new Pbkdf2Meta(iters, saltLen, hashLen);
                return true;
            }

            if (version == "2" && algName == "ARGON2ID")
            {
                // parts[3] = "m=<KB>,t=<i>,p=<p>"
                // parts[4] = "sl=<sl>,hl=<hl>"
                if (parts.Length < 6) return false;

                if (!TryTriplet(parts[3], out var m, out var t, out var p)) return false;
                if (!TryLens(parts[4], out var sl, out var hl)) return false;

                alg = PasswordAlgorithm.Argon2id;
                meta = new Argon2idMeta(m, t, p, sl, hl);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryTriplet(string s, out int m, out int t, out int p)
    {
        m = t = p = 0;
        var x = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (x.Length != 3) return false;
        return x[0].StartsWith("m=") && int.TryParse(x[0].AsSpan(2), out m)
            && x[1].StartsWith("t=") && int.TryParse(x[1].AsSpan(2), out t)
            && x[2].StartsWith("p=") && int.TryParse(x[2].AsSpan(2), out p);
    }

    private static bool TryLens(string s, out int sl, out int hl)
    {
        sl = hl = 0;
        var x = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (x.Length != 2) return false;
        return x[0].StartsWith("sl=") && int.TryParse(x[0].AsSpan(3), out sl)
            && x[1].StartsWith("hl=") && int.TryParse(x[1].AsSpan(3), out hl);
    }
}
