using Microsoft.Extensions.DependencyInjection;

namespace Nami.Identity.Core;

/// <summary>
/// The only implementation of <see cref="INamiIdentityBuilder"/>.
/// </summary>
/// <remarks>
/// Internal on purpose. A consumer receives the interface, so making the class
/// public would put a constructor and a type name into the surface ADR-0044
/// versions, for no capability a consumer gains.
/// </remarks>
internal sealed class NamiIdentityBuilder : INamiIdentityBuilder
{
    internal NamiIdentityBuilder(IServiceCollection services) => Services = services;

    public IServiceCollection Services { get; }
}
