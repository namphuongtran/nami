using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Nami.Identity.Core;

/// <summary>
/// The composition-root entry point, <c>AddNamiIdentity</c>.
/// </summary>
/// <remarks>
/// <b>The namespace is a departure from the usual .NET answer, and it is
/// decided rather than overlooked.</b> An <see cref="IServiceCollection"/>
/// extension is normally placed in <c>Microsoft.Extensions.DependencyInjection</c>
/// so it appears without a <c>using</c>. ADR-0065 requires instead that "a
/// namespace matches its folder and assembly", so this sits under
/// <c>Nami.Identity.Core</c> and a consumer writes the <c>using</c>.
/// </remarks>
public static class NamiIdentityServiceCollectionExtensions
{
    /// <summary>
    /// The configuration section bound onto <see cref="NamiIdentityOptions"/>.
    /// </summary>
    /// <remarks>
    /// Design 04 section 6 owns these key names and states that it is their
    /// origin. Only the three members it names bind from here; ADR-0096
    /// parameter F leaves every other key to the design owning its subject, so
    /// the remaining members are settable in code only. This constant is private
    /// because a public one would be a second public spelling of a contract
    /// design 04 already owns.
    /// </remarks>
    private const string ProtocolSection = "Nami:Protocol";

    /// <summary>
    /// Registers Nami's identity services and returns the builder that opt-in
    /// modules extend.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">
    /// Runs after configuration binding and wins over it (ADR-0096 parameter E),
    /// so a value written here cannot be overridden by an environment variable.
    /// </param>
    /// <returns>The builder described by <see cref="INamiIdentityBuilder"/>.</returns>
    public static INamiIdentityBuilder AddNamiIdentity(
        this IServiceCollection services,
        Action<NamiIdentityOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IValidateOptions<NamiIdentityOptions>, NamiIdentityOptionsValidator>());

        // Order is the decision. BindConfiguration registers its IConfigureOptions
        // first and Configure registers the caller's second, and the options
        // pattern runs them in registration order, so the delegate wins.
        services.AddOptions<NamiIdentityOptions>()
            .BindConfiguration(ProtocolSection)
            .Configure(configure)
            .ValidateOnStart();

        // The engine, after the options are registered rather than before. Nothing
        // in the wiring resolves an option at registration time, so the order does
        // not matter to the compiler; it matters to a reader, who should see what
        // configures what.
        OpenIddictWiring.AddNamiOpenIddictSegments(services);

        return new NamiIdentityBuilder(services);
    }
}
