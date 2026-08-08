using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace Nami.Identity.Core;

/// <summary>
/// Copies the four <see cref="NamiIdentityOptions"/> members the protocol engine
/// consumes onto <see cref="OpenIddictServerOptions"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a class and not four builder calls.</b> The builder's setters take a
/// literal at registration time, and these four values are not known then: they
/// arrive from configuration and from the caller's delegate, both of which the
/// options pattern resolves later. An
/// <see cref="IConfigureOptions{TOptions}"/> is the mechanism that bridges the
/// two, and it is this repository's established one rather than a new idea:
/// ADR-0011 makes a custom <c>IConfigureOptions&lt;OpenIddictServerOptions&gt;</c>
/// the archetypal seam, and design 12 section 3 shows the same shape with a
/// key store as its dependency instead of these options.
/// </para>
/// <para>
/// <b>Nothing here overlaps the builder chain, and that is deliberate.</b> The
/// chain in <c>OpenIddictWiring</c> sets only what a decision fixes, so no
/// property is written twice and the registration order of two
/// <see cref="IConfigureOptions{TOptions}"/> instances never has to be reasoned
/// about. A value belongs in exactly one of the two places.
/// </para>
/// <para>
/// <b>Internal, because it is a mechanism rather than a surface.</b> A consumer
/// configures Nami through <see cref="NamiIdentityOptions"/>, so making this
/// public would add a type name and a constructor to what ADR-0044 versions for
/// no capability gained. That is the same reasoning recorded on
/// <c>NamiIdentityBuilder</c>.
/// </para>
/// </remarks>
internal sealed class ConfigureServerOptionsFromNamiOptions
    : IConfigureOptions<OpenIddictServerOptions>
{
    private readonly IOptions<NamiIdentityOptions> _options;

    internal ConfigureServerOptionsFromNamiOptions(IOptions<NamiIdentityOptions> options)
        => _options = options;

    /// <summary>Writes the four values onto the engine's options.</summary>
    /// <remarks>
    /// <para>
    /// <b>Three of these four tighten the engine's own default, and one matches
    /// it.</b> Every figure below was read at
    /// <c>OpenIddictServerOptions</c> at 7.6.0 rather than assumed, because this
    /// repository treats a value that is set and a default that is known as two
    /// separate claims. A missing assignment here is therefore not a no-op: it
    /// would hand back the engine's looser value.
    /// </para>
    /// <list type="table">
    ///   <listheader><term>Property</term><description>Nami / engine default</description></listheader>
    ///   <item><term><c>AccessTokenLifetime</c></term><description>15 minutes / <b>1 hour</b></description></item>
    ///   <item><term><c>RefreshTokenLifetime</c></term><description>8 hours / <b>14 days</b></description></item>
    ///   <item><term><c>RefreshTokenReuseLeeway</c></term><description>30 seconds / 30 seconds, the one that matches</description></item>
    ///   <item><term><c>DisableAccessTokenEncryption</c></term><description>set true / <c>false</c>, so the engine encrypts unless told otherwise</description></item>
    /// </list>
    /// <para>
    /// The refresh-token row is the one worth staring at. Losing that single
    /// assignment turns an 8-hour ceiling into a 14-day one with every gate green.
    /// </para>
    /// </remarks>
    public void Configure(OpenIddictServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        NamiIdentityOptions nami = _options.Value;

        // Engine default 1 hour (ADR-0004 fixes 15 minutes).
        options.AccessTokenLifetime = nami.AccessTokenLifetime;

        // Engine default 14 DAYS (ADR-0004 fixes an 8-hour ceiling).
        options.RefreshTokenLifetime = nami.RefreshTokenLifetime;

        // Engine default 30 seconds, the same value ADR-0004:34 fixes and says is
        // "the OpenIddict default". Set anyway, so an upstream change shows in the diff.
        options.RefreshTokenReuseLeeway = nami.RefreshTokenReuseLeeway;

        // The engine's flag is DisableAccessTokenEncryption, so the sense is
        // inverted against Nami's AccessTokenEncryption, and the engine's default
        // of false means it encrypts unless told not to. ADR-0005 wants a plain
        // signed JWT, so the common case writes true here.
        options.DisableAccessTokenEncryption = !nami.AccessTokenEncryption;
    }
}
