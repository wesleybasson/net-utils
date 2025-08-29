using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiX.Security.Cryptography.Encryption;

/// <summary>
/// Service registration extensions for encryption components.
/// </summary>
public static class EncryptionServiceCollectionExtensions
{
    /// <summary>
    /// Registers authenticated encryption (AES-GCM) using options from configuration.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="config">Application configuration.</param>
    /// <param name="sectionPath">Configuration path. Default: "Security:Encryption".</param>
    public static IServiceCollection AddAeadEncryption(
        this IServiceCollection services,
        IConfiguration config,
        string sectionPath = "Security:Encryption")
    {
        var options = config.GetSection(sectionPath).Get<AeadEncryptionOptions>() ?? new();
        services.AddSingleton(options);

        // Primary GCM encryptor
        services.AddSingleton<IAuthenticatedEncryptor, AesGcmEncryptor>();

        return services;
    }

    /// <summary>
    /// (Optional) Adds a facade that can also decrypt legacy CBC ciphertexts while encrypting with GCM.
    /// Provide legacy key/iv from config section "Security:Encryption:Legacy".
    /// </summary>
    public static IServiceCollection AddAeadEncryptionWithLegacyFallback(
        this IServiceCollection services,
        IConfiguration config,
        string sectionPath = "Security:Encryption",
        string legacySectionPath = "Security:Encryption:Legacy")
    {
        var options = config.GetSection(sectionPath).Get<AeadEncryptionOptions>() ?? new();
        services.AddSingleton(options);

        services.AddSingleton<IAuthenticatedEncryptor, AesGcmEncryptor>();

        var legacy = config.GetSection(legacySectionPath);
        var legacyKeyHex = legacy["KeyHex"];
        var legacyIvHex = legacy["IvHex"];

        if (!string.IsNullOrWhiteSpace(legacyKeyHex) && !string.IsNullOrWhiteSpace(legacyIvHex))
        {
            services.AddSingleton(new LegacyAesCbcDecryptor(legacyKeyHex!, legacyIvHex!));
            services.AddSingleton<IAuthenticatedEncryptor>(sp =>
                new EncryptionFacade(sp.GetRequiredService<AesGcmEncryptor>(),
                                     sp.GetRequiredService<LegacyAesCbcDecryptor>()));
        }

        return services;
    }
}
