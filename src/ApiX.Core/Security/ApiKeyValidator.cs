using System.Security.Cryptography;

namespace ApiX.Core.Security;

/// <summary>
/// Enum for defining the format of an API Key.
/// </summary>
public enum ApiKeyFormat
{
    /// <summary>Opaque string — compare with string equality.</summary>
    OpaqueString = 0,
    /// <summary>Base64-encoded bytes — compare with fixed-time byte equality.</summary>
    Base64 = 1,
    /// <summary>Hex-encoded bytes — compare with fixed-time byte equality.</summary>
    Hex = 2
}

/// <summary>Result of a key validation.</summary>
public readonly record struct KeyValidation(bool IsValid, string? Reason = null)
{
    /// <summary>
    /// Creates a successful <see cref="KeyValidation"/> result.
    /// </summary>
    /// <returns>A <see cref="KeyValidation"/> indicating a valid key, with no failure reason.</returns>
    public static KeyValidation Ok() => new(true, null);

    /// <summary>
    /// Creates a failed <see cref="KeyValidation"/> result with a specified reason.
    /// </summary>
    /// <param name="reason">A descriptive message indicating why validation failed.</param>
    /// <returns>A <see cref="KeyValidation"/> indicating an invalid key and containing the failure reason.</returns>
    public static KeyValidation Fail(string reason) => new(false, reason);
}

/// <summary>
/// Provides helper methods for validating API keys in various formats, 
/// including opaque string tokens, Base64-encoded keys, and hex-encoded keys.
/// </summary>
/// <remarks>
/// Methods in this class support both simple ordinal string comparisons 
/// and timing-safe byte comparisons for encoded keys to help prevent timing attacks.
/// </remarks>
public static class ApiKeyValidator
{
    // -------- Public string-based validations (opaque tokens) --------

    /// <summary>
    /// Validates an API key against an expected value using a string comparison
    /// (opaque token). Uses Ordinal comparison by default.
    /// </summary>
    public static bool IsApiKeyValid(
        string? provided,
        string? expected,
        bool ignoreCase = false,
        bool requirePresent = true)
    {
        if (requirePresent && string.IsNullOrWhiteSpace(provided))
            return false;

        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(provided ?? string.Empty, expected ?? string.Empty, comparison);
    }

    /// <summary>
    /// Validates and returns <see cref="KeyValidation"/> with a reason for failure (opaque token).
    /// </summary>
    public static KeyValidation ValidateOpaque(
        string? provided,
        string? expected,
        bool ignoreCase = false,
        bool requirePresent = true)
    {
        if (requirePresent && string.IsNullOrWhiteSpace(provided))
            return KeyValidation.Fail("Missing or empty key.");
        if (expected is null)
            return KeyValidation.Fail("Expected key is null (misconfiguration).");

        var ok = IsApiKeyValid(provided, expected, ignoreCase, requirePresent: false);
        return ok ? KeyValidation.Ok() : KeyValidation.Fail("Key mismatch.");
    }

    // -------- Public byte-equality validations (Base64 / Hex) --------

    /// <summary>
    /// Validates keys encoded as Base64 or Hex using timing-safe byte equality.
    /// </summary>
    public static KeyValidation ValidateEncoded(
        string? provided,
        string? expected,
        ApiKeyFormat format,
        bool requirePresent = true)
    {
        if (format is ApiKeyFormat.OpaqueString)
            return ValidateOpaque(provided, expected);

        if (requirePresent && string.IsNullOrWhiteSpace(provided))
            return KeyValidation.Fail("Missing or empty key.");
        if (string.IsNullOrWhiteSpace(expected))
            return KeyValidation.Fail("Expected key is missing or empty (misconfiguration).");

        if (!TryDecode(format, provided!, out var providedBytes, out var reasonP))
            return KeyValidation.Fail($"Provided key invalid: {reasonP}");
        if (!TryDecode(format, expected!, out var expectedBytes, out var reasonE))
            return KeyValidation.Fail($"Expected key invalid: {reasonE}");

        var equal = CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
        return equal ? KeyValidation.Ok() : KeyValidation.Fail("Key mismatch.");
    }

    /// <summary>
    /// Convenience wrapper: Base64 timing-safe validation.
    /// </summary>
    public static KeyValidation ValidateBase64(string? provided, string? expected, bool requirePresent = true) =>
        ValidateEncoded(provided, expected, ApiKeyFormat.Base64, requirePresent);

    /// <summary>
    /// Convenience wrapper: Hex timing-safe validation.
    /// </summary>
    public static KeyValidation ValidateHex(string? provided, string? expected, bool requirePresent = true) =>
        ValidateEncoded(provided, expected, ApiKeyFormat.Hex, requirePresent);

    // -------- Internal helpers --------

    private static bool TryDecode(ApiKeyFormat format, string s, out byte[] bytes, out string? reason)
    {
        switch (format)
        {
            case ApiKeyFormat.Base64:
                var base64Span = s.AsSpan();
                bytes = new byte[GetBase64MaxBytes(base64Span)];
                if (Convert.TryFromBase64String(s, bytes, out var written))
                {
                    if (written != bytes.Length) Array.Resize(ref bytes, written);
                    reason = null;
                    return true;
                }
                reason = "Not valid Base64.";
                bytes = Array.Empty<byte>();
                return false;

            case ApiKeyFormat.Hex:
                return TryFromHex(s, out bytes, out reason);

            default:
                reason = "Unsupported format for decoding.";
                bytes = Array.Empty<byte>();
                return false;
        }
    }

    private static int GetBase64MaxBytes(ReadOnlySpan<char> s)
    {
        // Worst-case buffer size for TryFromBase64String
        // 3 bytes per 4 chars, rounding down; pad a little for safety
        var len = s.Length;
        return Math.Max(0, (len / 4) * 3 + 3);
    }

    private static bool TryFromHex(string hex, out byte[] bytes, out string? reason)
    {
        if (hex.Length == 0 || (hex.Length % 2) != 0)
        {
            bytes = Array.Empty<byte>();
            reason = "Hex length must be even and non-empty.";
            return false;
        }

        bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            var hi = FromHexNibble(hex[2 * i]);
            var lo = FromHexNibble(hex[2 * i + 1]);
            if (hi < 0 || lo < 0)
            {
                bytes = Array.Empty<byte>();
                reason = $"Invalid hex character at position {2 * i} or {2 * i + 1}.";
                return false;
            }
            bytes[i] = (byte)((hi << 4) | lo);
        }
        reason = null;
        return true;

        static int FromHexNibble(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return -1;
        }
    }
}

// -------- Back-compat shim for existing callers --------

/// <summary>
/// Legacy extension methods for API key validation.
/// </summary>
/// <remarks>
/// This class has been superseded by <see cref="ApiKeyValidator"/> and is retained 
/// only for backward compatibility. New code should prefer <see cref="ApiKeyValidator"/>.
/// </remarks>
public static class KeyCheck
{
    /// <summary>Obsolete. Use <see cref="ApiKeyValidator.IsApiKeyValid"/> / <see cref="ApiKeyValidator.ValidateOpaque"/> instead.</summary>
    [Obsolete("Use ApiKeyValidator.IsApiKeyValid(...) or ApiKeyValidator.ValidateOpaque(...).")]
    public static bool AesApiKeyNotValid(this string apiKey, string apiKeyFromConfig) =>
        !ApiKeyValidator.IsApiKeyValid(apiKey, apiKeyFromConfig, ignoreCase: true);

    /// <summary>Obsolete. Use <see cref="ApiKeyValidator.IsApiKeyValid"/> / <see cref="ApiKeyValidator.ValidateOpaque"/> instead.</summary>
    [Obsolete("Use ApiKeyValidator.IsApiKeyValid(...) or ApiKeyValidator.ValidateOpaque(...).")]
    public static bool MasterApiKeyNotValid(this string apiKey, string apiKeyFromConfig)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return true;
        return !ApiKeyValidator.IsApiKeyValid(apiKey, apiKeyFromConfig, ignoreCase: true);
    }
}
