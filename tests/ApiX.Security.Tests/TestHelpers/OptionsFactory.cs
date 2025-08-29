using ApiX.Security.Cryptography.Hashing;
using Microsoft.Extensions.Options;

namespace ApiX.Security.Tests.TestHelpers;

internal static class OptionsFactory
{
    public static IOptions<PasswordHashingOptions> Create(PasswordHashingOptions? o = null)
        => Options.Create(o ?? new PasswordHashingOptions());
}
