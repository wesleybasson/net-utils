using ApiX.Core.Security;
using System.Globalization;

namespace ApiX.Core.Tests.Security;

public class ApiKeyValidatorTests
{
    [Fact]
    public void Opaque_ExactMatch_Ordinal_Succeeds()
    {
        Assert.True(ApiKeyValidator.IsApiKeyValid("ABC123", "ABC123"));
        Assert.False(ApiKeyValidator.IsApiKeyValid("abc123", "ABC123")); // Ordinal default
    }

    [Fact]
    public void Opaque_IgnoreCase_Succeeds()
    {
        Assert.True(ApiKeyValidator.IsApiKeyValid("abc123", "ABC123", ignoreCase: true));
    }

    [Fact]
    public void Opaque_RequirePresent_FailsOnEmpty()
    {
        var res = ApiKeyValidator.ValidateOpaque("", "k", ignoreCase: false, requirePresent: true);
        Assert.False(res.IsValid);
        Assert.Equal("Missing or empty key.", res.Reason);
    }

    [Fact]
    public void Opaque_Mismatch_ReturnsReason()
    {
        var res = ApiKeyValidator.ValidateOpaque("a", "b");
        Assert.False(res.IsValid);
        Assert.Equal("Key mismatch.", res.Reason);
    }

    [Fact]
    public void Opaque_CultureIndependent()
    {
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            Assert.True(ApiKeyValidator.IsApiKeyValid("i", "i"));                       // Ordinal works same
            Assert.False(ApiKeyValidator.IsApiKeyValid("I", "i"));                      // Ordinal is case-sensitive
            Assert.True(ApiKeyValidator.IsApiKeyValid("I", "i", ignoreCase: true));     // Explicit ignore case
        }
        finally { CultureInfo.CurrentCulture = prior; }
    }

    [Fact]
    public void Base64_Equal_FixedTime_Succeeds()
    {
        var expected = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });
        var provided = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });
        var res = ApiKeyValidator.ValidateBase64(provided, expected);
        Assert.True(res.IsValid);
        Assert.Null(res.Reason);
    }

    [Fact]
    public void Base64_Mismatch_FixedTime_Fails()
    {
        var expected = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });
        var provided = Convert.ToBase64String(new byte[] { 1, 2, 3, 5 });
        var res = ApiKeyValidator.ValidateBase64(provided, expected);
        Assert.False(res.IsValid);
        Assert.Equal("Key mismatch.", res.Reason);
    }

    [Fact]
    public void Base64_InvalidInput_ReturnsReason()
    {
        var res = ApiKeyValidator.ValidateBase64("not base64", "AQIDBA==");
        Assert.False(res.IsValid);
        Assert.StartsWith("Provided key invalid:", res.Reason);
    }

    [Fact]
    public void Hex_Equal_FixedTime_Succeeds()
    {
        var res = ApiKeyValidator.ValidateHex("0a0B0c0D", "0A0B0C0D");
        Assert.True(res.IsValid);
    }

    [Fact]
    public void Hex_InvalidLength_ReturnsReason()
    {
        var res = ApiKeyValidator.ValidateHex("ABC", "ABCD");
        Assert.False(res.IsValid);
        Assert.Equal("Provided key invalid: Hex length must be even and non-empty.", res.Reason);
    }

    [Fact]
    public void Hex_InvalidChar_ReturnsReason()
    {
        var res = ApiKeyValidator.ValidateHex("GG", "00");
        Assert.False(res.IsValid);
        Assert.StartsWith("Provided key invalid:", res.Reason);
    }

    [Fact]
    public void Encoded_MissingProvided_WithRequirePresent_Fails()
    {
        var res = ApiKeyValidator.ValidateEncoded("", "00", ApiKeyFormat.Hex, requirePresent: true);
        Assert.False(res.IsValid);
        Assert.Equal("Missing or empty key.", res.Reason);
    }

    [Fact]
    [Obsolete("Use positive API key checks instead")]
    public void BackCompat_Shim_BehavesLikeOldMethods()
    {
        // AesApiKeyNotValid returns true when not equal (case-insensitive)
        Assert.False("abc".AesApiKeyNotValid("ABC")); // equal ignoring case -> not invalid
        Assert.True("abc".AesApiKeyNotValid("XYZ"));  // mismatch -> invalid

        // MasterApiKeyNotValid returns true on empty / mismatch
        Assert.True("".MasterApiKeyNotValid("x"));
        Assert.False("ABC".MasterApiKeyNotValid("abc")); // equal ignoring case
    }
}
