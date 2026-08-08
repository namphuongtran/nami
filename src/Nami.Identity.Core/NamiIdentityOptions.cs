namespace Nami.Identity.Core;

/// <summary>
/// The options a consumer supplies to <c>AddNamiIdentity</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every member's type, nullability, and accessor is fixed by ADR-0096. Design
/// 01 section 3.4 states what each option means and states no type, which is
/// what made this class unwritable until that decision existed.
/// </para>
/// <para>
/// <b>The two required options are nullable and carry no <c>required</c>
/// modifier.</b> That reverses what the design table's wording invites, and the
/// reason is mechanical rather than stylistic: <c>required</c> is a contract on
/// an object initializer, and the options system does not write one. Read at
/// source on 2026-08-08 in <c>dotnet/runtime</c> at tag <c>v10.0.0</c>,
/// <c>Microsoft.Extensions.Options/src/OptionsFactory.cs</c> constrains its type
/// parameter as <c>where TOptions : class</c> and creates the instance with
/// <c>Activator.CreateInstance</c>. A missing value is rejected at start-up
/// instead, by the validator this assembly registers.
/// </para>
/// <para>
/// <b>Accessors are <c>set</c> rather than <c>init</c></b>, following
/// <c>ScopeDefinition</c> and <c>ClientDefinition</c> rather than the audit
/// DTOs, because a configuration binder writes this type.
/// </para>
/// </remarks>
public sealed class NamiIdentityOptions
{
    /// <summary>
    /// The PostgreSQL connection string. Required, and enforced at start-up.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// The base issuer. Required, and enforced at start-up. Per-tenant issuers
    /// derive from it.
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>The signing algorithm. Defaults to <c>RS256</c> (ADR-0005).</summary>
    public SigningAlgorithm SigningAlgorithm { get; set; } = SigningAlgorithm.RS256;

    /// <summary>How long an access token is valid. Defaults to 15 minutes (ADR-0004).</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The absolute refresh-token ceiling. Defaults to 8 hours (ADR-0004), which
    /// matches the absolute session limit rather than coinciding with it.
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromHours(8);

    /// <summary>
    /// How long a redeemed rolling refresh token still works, so a client that
    /// retries through a network timeout is not logged out. Defaults to 30
    /// seconds (ADR-0004).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This default equals the engine's own, and that is deliberate rather
    /// than redundant.</b> Read at OpenIddict 7.6.0,
    /// <c>OpenIddictServerOptions.RefreshTokenReuseLeeway</c> is initialised to
    /// <c>TimeSpan.FromSeconds(30)</c> and its own summary says "The default
    /// value is 30 seconds". ADR-0004 line 34 states both halves: the value is 30
    /// seconds, and it is "the OpenIddict default". Setting it explicitly is what
    /// that ADR asks for, and it means an upstream default change cannot move
    /// Nami's behaviour without the diff showing it.
    /// </para>
    /// <para>
    /// <b>Non-nullable here and nullable on the engine.</b> OpenIddict declares
    /// <c>TimeSpan?</c>, where null means "use the engine default". Nami has a
    /// default of its own, so there is no state to express with null, and the two
    /// sibling lifetimes above are non-nullable for the same reason.
    /// </para>
    /// </remarks>
    public TimeSpan RefreshTokenReuseLeeway { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>The sliding session window. Defaults to 1 hour (ADR-0003).</summary>
    public TimeSpan SessionInactivity { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// The absolute session limit, past which re-authentication is required.
    /// Defaults to 8 hours (ADR-0003).
    /// </summary>
    public TimeSpan SessionAbsolute { get; set; } = TimeSpan.FromHours(8);

    /// <summary>
    /// Whether the access token is encrypted. Defaults to <see langword="false"/>,
    /// so the access token is a plain signed JWT (ADR-0005).
    /// </summary>
    /// <remarks>
    /// There is no initializer, and its absence is deliberate. Writing
    /// <c>= false</c> trips <c>CA1805</c>, which ADR-0093 makes an error, so the
    /// default comes from the language.
    /// </remarks>
    public bool AccessTokenEncryption { get; set; }

    /// <summary>
    /// Whether HTTPS is required. Defaults to <see langword="true"/>, relaxed
    /// only in development (ADR-0076).
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// Whether the first signing key is seeded at cold start. Defaults to
    /// <see langword="true"/> (ADR-0012), because a host that did not auto-seed
    /// would come up unable to sign.
    /// </summary>
    public bool AutoSeedFirstKey { get; set; } = true;

    /// <summary>
    /// Whether migrations run at start-up. Defaults to <see langword="false"/>
    /// and is for development only (ADR-0017, ADR-0025), because a host that
    /// migrated at start-up would race its own replicas.
    /// </summary>
    /// <remarks>
    /// There is no initializer, for the same <c>CA1805</c> reason recorded on
    /// <see cref="AccessTokenEncryption"/>.
    /// </remarks>
    public bool MigrateOnStartup { get; set; }

    /// <summary>
    /// The optional free registration key (ADR-0032). A missing key logs once at
    /// information level and blocks nothing.
    /// </summary>
    public string? RegistrationKey { get; set; }
}
