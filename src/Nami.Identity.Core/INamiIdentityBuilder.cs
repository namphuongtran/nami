using Microsoft.Extensions.DependencyInjection;

namespace Nami.Identity.Core;

/// <summary>
/// The value <c>AddNamiIdentity</c> returns, which opt-in modules extend.
/// </summary>
/// <remarks>
/// <para>
/// <b>One member is the decision, not the minimum</b> (ADR-0096). Every
/// <c>.Add…</c> and <c>.Use…</c> call in design 01 section 3.2 ships in a package
/// that depends on this assembly, so none of them can be declared here without
/// inverting the dependency rule. They are extension methods on this interface,
/// declared in their own packages, and an extension method needs somewhere to
/// register into and nothing else.
/// </para>
/// <para>
/// Keeping it to one member also means adding a module later adds no member to
/// Nami's own surface, so ADR-0044 parameter B never has to classify it.
/// </para>
/// </remarks>
public interface INamiIdentityBuilder
{
    /// <summary>The service collection the modules register into.</summary>
    IServiceCollection Services { get; }
}
