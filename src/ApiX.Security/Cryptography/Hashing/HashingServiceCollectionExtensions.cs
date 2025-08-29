using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiX.Security.Cryptography.Hashing;

/// <summary>
/// Provides extension methods for registering password hashing services
/// into an <see cref="IServiceCollection"/>.
/// </summary>
public static class HashingServiceCollectionExtensions
{
    /// <summary>
    /// Registers password hashing services, algorithms, and factories with the dependency injection container.
    /// </summary>
    /// <param name="services">The DI service collection to add registrations to.</param>
    /// <param name="config">
    /// The application configuration from which <see cref="PasswordHashingOptions"/>
    /// will be bound (e.g., <c>appsettings.json</c>).
    /// </param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the <see cref="PasswordHashingOptions"/>.
    /// Defaults to <c>"Security:PasswordHashing"</c>.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
    /// </returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Registers <see cref="PasswordHashingOptions"/> from configuration.</item>
    /// <item>Registers concrete implementations <see cref="Pbkdf2PasswordHasher"/> and <see cref="Argon2idPasswordHasher"/>.</item>
    /// <item>Registers <see cref="IPasswordHasherFactory"/> for algorithm selection and detection.</item>
    /// <item>Optionally exposes the current default algorithm directly as <see cref="IPasswordHasher"/> via <c>Transient</c> registration.</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddPasswordHashing(
        this IServiceCollection services,
        IConfiguration config,
        string sectionPath = "Security:PasswordHashing")
    {
        services.Configure<PasswordHashingOptions>(config.GetSection(sectionPath));

        // Concrete implementations
        services.AddSingleton<Pbkdf2PasswordHasher>();
        services.AddSingleton<Argon2idPasswordHasher>();

        // Factory
        services.AddSingleton<IPasswordHasherFactory, PasswordHasherFactory>();

        // Optional: inject "current default" directly as IPasswordHasher
        services.AddTransient<IPasswordHasher>(sp =>
            sp.GetRequiredService<IPasswordHasherFactory>().Create());

        return services;
    }
}
