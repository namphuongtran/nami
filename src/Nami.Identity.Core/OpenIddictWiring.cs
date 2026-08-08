using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace Nami.Identity.Core;

/// <summary>
/// The <c>AddOpenIddict()</c> segments this assembly owns.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two segments of a block that no single assembly can call.</b> Seed S-009
/// settled the split by reading which builder type each call extends: the
/// <c>.AddCore(…)</c> segment and everything inside it needs
/// <c>OpenIddict.Core</c>, <c>.EntityFrameworkCore</c> and <c>.Quartz</c>, which
/// design 01 section 3.1 forbids this assembly from referencing. So only
/// <c>.AddServer</c> and <c>.AddValidation</c> are here, and the persistence
/// adapter calls <c>services.AddOpenIddict().AddCore(…)</c> of its own.
/// </para>
/// <para>
/// <b>Calling <c>AddOpenIddict()</c> from two assemblies is safe, and that was
/// read rather than assumed.</b> At OpenIddict 7.6.0 the method's whole body is
/// <c>return new OpenIddictBuilder(services)</c>: a stateless factory over the
/// service collection, not a registration. <c>AddCore</c> registers only through
/// <c>TryAdd*</c>. So two builders over one collection double-register nothing.
/// </para>
/// <para>
/// <b>Every API name here was read at 7.6.0</b> (seed S-028), against the
/// upstream commit <c>5ce649a5bbbf1340c9be9c4f264197af563ab473</c> that the
/// package declares. Thirty-three names survived the bump from 7.5.0 unchanged
/// and one call in the design did not compile at all, which is why the
/// code-challenge line below goes through <c>Configure</c>.
/// </para>
/// <para>
/// <b>What is NOT here.</b> No value that arrives from
/// <see cref="NamiIdentityOptions"/>, because a builder setter takes a literal at
/// registration time and those values are resolved later.
/// <see cref="ConfigureServerOptionsFromNamiOptions"/> carries those four, and
/// nothing is written in both places. Signing credentials are not here either:
/// design 04 assigns them to the key-management subsystem through the ADR-0011
/// seam.
/// </para>
/// </remarks>
internal static class OpenIddictWiring
{
    /// <summary>
    /// Adds the server and validation segments to <paramref name="services"/>.
    /// </summary>
    /// <param name="services">The collection <c>AddNamiIdentity</c> is building.</param>
    internal static void AddNamiOpenIddictSegments(IServiceCollection services)
    {
        services.AddOpenIddict()

            .AddServer(server =>
            {
                // Only discovery and JWKS are auto-pathed (ADR-0048 parameter A).
                // Every other endpoint needs its Set*EndpointUris call or it does
                // not exist. The paths are literals here because no options member
                // carries them yet; seed S-031 owns that question.
                server.SetAuthorizationEndpointUris("connect/authorize")
                      .SetTokenEndpointUris("connect/token")
                      .SetUserInfoEndpointUris("connect/userinfo")
                      .SetIntrospectionEndpointUris("connect/introspect")
                      .SetRevocationEndpointUris("connect/revoke")
                      .SetEndSessionEndpointUris("connect/endsession")
                      .SetDeviceAuthorizationEndpointUris("connect/device")
                      .SetEndUserVerificationEndpointUris("connect/device/verify")
                      .SetPushedAuthorizationEndpointUris("connect/par")
                      .SetJsonWebKeySetEndpointUris(".well-known/jwks");

                // ADR-0014 scopes the v1 flow set. PKCE is mandatory rather than
                // per-client, which is RFC 9700's guidance and ADR-0035's premise.
                server.AllowAuthorizationCodeFlow()
                      .RequireProofKeyForCodeExchange()
                      .AllowClientCredentialsFlow()
                      .AllowRefreshTokenFlow();

                server.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Email,
                    "api");

                // S256 only, RFC 9700. There is no builder method for this, so it
                // goes through the documented Configure escape hatch, which is how
                // the builder implements its own setters. The design wrote this as
                // `server.CodeChallengeMethods.Remove(…)` and that does not compile:
                // the member is on the options, not on the builder.
                server.Configure(options => options.CodeChallengeMethods.Remove(
                    OpenIddictConstants.CodeChallengeMethods.Plain));

                // Rolling refresh, one-time use, reuse detection and family revoke
                // are all default-on (ADR-0004). Never call
                // DisableRollingRefreshTokens.
                server.UseAspNetCore()
                      .EnableAuthorizationEndpointPassthrough()
                      .EnableTokenEndpointPassthrough()
                      .EnableUserInfoEndpointPassthrough()
                      .EnableEndSessionEndpointPassthrough()
                      .EnableEndUserVerificationEndpointPassthrough()
                      .EnableStatusCodePagesIntegration();
            })

            .AddValidation(validation =>
            {
                // The server and the validation stack share one process here, so
                // validation reads the server's own configuration rather than
                // fetching discovery over HTTP.
                validation.UseLocalServer();
                validation.UseAspNetCore();

                // DB-anchored revocation (ADR-0039). Both are on the VALIDATION
                // builder and not on AddServer, which design 04 flags as the
                // common wrong-API slip in this block.
                validation.EnableTokenEntryValidation();
                validation.EnableAuthorizationEntryValidation();
            });

        // The four option-driven values, in one place and never also in the chain.
        services.AddSingleton<IConfigureOptions<OpenIddictServerOptions>,
                              ConfigureServerOptionsFromNamiOptions>();
    }
}
