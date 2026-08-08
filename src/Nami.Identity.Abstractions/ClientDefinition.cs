namespace Nami.Identity.Abstractions;

/// <summary>
/// A client as an operator declares it, before it becomes an engine descriptor.
/// </summary>
/// <remarks>
/// <para>
/// The seventeen members and their nullability are transcribed from the class diagram in
/// design 23 section 3, which that document declares its own implementer source of record.
/// The diagram annotates nullable members explicitly, so an unannotated member here is
/// non-nullable by statement rather than by assumption.
/// </para>
/// <para>
/// <b>The defaults are the security argument, not the shape.</b> Design 23 section 8 puts
/// it that way: every field whose wrong value would weaken a client defaults to the safe
/// value, so a consumer who never reads that document still gets a safe client.
/// </para>
/// <para>
/// <b>Six defaults are stated in section 3</b>, in the prose directly under the class
/// diagram. Four are written as initializers below. The other two, <see cref="RequireConsent"/>
/// and <see cref="IsNativeApp"/>, are <c>false</c>, which is already the C# default for
/// <see cref="bool"/>, so no initializer is written for them.
/// </para>
/// <para>
/// <b>The member count is contested, and this type takes the diagram's answer.</b> Design 23
/// section 4 carries a <c>BackchannelLogoutUri</c> row in its "Definition field" column that
/// the section 3 diagram does not declare. Design 15 puts that field on the admin DTOs and
/// ADR-0019 calls it a field on the Application, so it is not written here. The
/// contradiction is filed against design 23 rather than resolved, and <c>src/CLAUDE.md</c>
/// carries the full reading.
/// </para>
/// <para>
/// <b>Public against confidential is derived, never declared.</b> There is deliberately no
/// member for it. Design 23 section 5.1 invariant 1 derives it from the credential: a
/// client is confidential if it has a secret or a JSON Web Key set, and public otherwise,
/// so a consumer cannot mislabel one because there is no label to set.
/// </para>
/// <para>
/// <b>Adding a member later is a versioned act.</b> Design 23 section 7 states it: additive
/// with a default is a minor version, and changing an existing default is a behaviour break
/// even though the shape is unchanged (ADR-0044).
/// </para>
/// </remarks>
public sealed class ClientDefinition
{
    /// <summary>The client identifier, which is unique per tenant rather than globally.</summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// The shared secret, in plaintext. Null means no secret was supplied, which is one
    /// half of the public-client derivation above.
    /// </summary>
    /// <remarks>
    /// This is the one credential that does not go to the secret store, and design 23
    /// section 5.6 exists to stop the natural assumption that it does. The value is passed
    /// to the engine's manager as plaintext and the manager stores a hash, so nothing may
    /// read a secret back and rotation is generate-and-show-once rather than retrieve
    /// (ADR-0035). An external identity provider's secret is the opposite case and does go
    /// to the store (ADR-0009), because there Nami is the client rather than the server.
    /// </remarks>
    public string? ClientSecret { get; set; }

    /// <summary>The human-readable name shown on a consent screen.</summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// The flow this client uses, which the mapper translates into the engine's permission
    /// sets (design 23 section 5.2).
    /// </summary>
    /// <remarks>
    /// <b>No source states a default for this member.</b> Design 23 section 3 states a
    /// default for six members in the prose under its diagram and states none for this one.
    /// The initializer therefore writes down what C# would do anyway, and it does not make
    /// the value sourced. Do not cite this line as evidence of a decision.
    /// </remarks>
    public ClientFlow Flow { get; set; } = ClientFlow.Code;

    /// <summary>
    /// The redirect URIs, matched exactly. A wildcard throws at mapping rather than being
    /// narrowed (design 23 section 5.1 invariant 4).
    /// </summary>
    /// <remarks>
    /// Empty rather than required, and the reason is half sourced and half inferred, so the
    /// two are separated here. Design 23 section 5.2 gives
    /// <see cref="ClientFlow.ClientCredentials"/> the token endpoint only and no response
    /// type. It says nothing about redirect URIs, because its table has no column for them.
    /// A flow that never reaches a browser cannot use one, so requiring a value would reject
    /// a client the design calls legal. That last step is the inference.
    /// </remarks>
    public string[] RedirectUris { get; set; } = [];

    /// <summary>
    /// The post-logout redirect URIs. Design 23 section 4 parses these to <see cref="Uri"/>
    /// as it does the redirect URIs, so a malformed value fails at mapping. The wildcard
    /// invariant at section 5.1 is written for redirect URIs only, and no source extends it
    /// here.
    /// </summary>
    public string[] PostLogoutRedirectUris { get; set; } = [];

    /// <summary>
    /// The scopes this client may request. Empty grants nothing, which is the
    /// deny-by-default value.
    /// </summary>
    /// <remarks>
    /// <c>openid</c> is skipped rather than mapped to a permission, and design 23 section
    /// 5.1 invariant 5 gives both reasons. It is the request marker for an OIDC flow rather
    /// than a claim family, and the engine's scope-permission constants are exactly five
    /// with no member for it. Audience is not set here at all: it comes from
    /// <see cref="ScopeDefinition.Resources"/>, so it is declared once per API rather than
    /// repeated on every client.
    /// </remarks>
    public string[] AllowedScopes { get; set; } = [];

    /// <summary>
    /// The cross-origin request origins for this client. An origin is scheme, host, and
    /// port with no path (design 04 section on CORS, and design 15).
    /// </summary>
    /// <remarks>
    /// <b>This field is the declaration, not the system of record.</b> Design 23 section 1
    /// names it "a definition field whose system of record is the application's property
    /// bag", and ADR-0050 parameter A puts the origins in
    /// <c>Application.Properties['cors_origins']</c>. This value is surfaced through the
    /// mapper into that bag. The policy provider reads a derived cache on a preflight and
    /// never either value directly (design 23 section 4).
    /// </remarks>
    public string[] AllowedCorsOrigins { get; set; } = [];

    /// <summary>
    /// Whether the proof key for code exchange is required. Defaults to <c>true</c>.
    /// </summary>
    /// <remarks>
    /// A public client using the code grant is forced to the proof key regardless, and
    /// setting this to <c>false</c> for one throws rather than being quietly overridden
    /// (design 23 section 5.1 invariant 2). Silent correction would leave the declaration
    /// and the behaviour disagreeing, and the reader believes the declaration.
    /// </remarks>
    public bool RequirePkce { get; set; } = true;

    /// <summary>
    /// Whether consent is explicit. Defaults to <c>false</c>. The engine models consent as
    /// a type rather than a flag, so this lands as the descriptor's consent type.
    /// </summary>
    public bool RequireConsent { get; set; }

    /// <summary>
    /// The tenant this client belongs to. It lands in the engine's property bag as control
    /// metadata, because tenancy is not an engine concept (design 23 section 4).
    /// </summary>
    /// <remarks>
    /// Null means the definition is untenanted, which is what the host start-up form of the
    /// seeder handles. The per-tenant form runs inside provisioning under that tenant's
    /// ambient context, because a client identifier is unique per tenant (ADR-0001).
    /// </remarks>
    public string? TenantId { get; set; }

    /// <summary>
    /// Whether this is a native application. Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// Setting it lands as the engine's native application type, which buys the built-in
    /// loopback relaxation. Design 23 section 5.1 invariant 7 states why that matters:
    /// writing a handler for loopback redirects instead would reimplement a native
    /// behaviour, which is the error class ADR-0021 calls out.
    /// </remarks>
    public bool IsNativeApp { get; set; }

    /// <summary>
    /// Whether the refresh grant is permitted. Defaults to <c>true</c>, and a
    /// machine-to-machine client should set it <c>false</c>.
    /// </summary>
    public bool IssueRefreshToken { get; set; } = true;

    /// <summary>
    /// The access token type, <c>jwt</c> or <c>reference</c>. Defaults to <c>jwt</c>.
    /// </summary>
    /// <remarks>
    /// It is a built property rather than a native engine setting, because the engine's
    /// reference-token option is a single global flag with no per-client form (design 23
    /// section 5.5). The consequence belongs with the declaration rather than buried in the
    /// handler: opting a client into reference tokens forces that client's resource server
    /// onto introspection, because an opaque token cannot be validated locally.
    /// </remarks>
    public string AccessTokenType { get; set; } = "jwt";

    /// <summary>
    /// The absolute ceiling on this client's refresh lifetime, bounded by the system ceiling
    /// in ADR-0004 (design 23 section 4).
    /// </summary>
    /// <remarks>
    /// Design 23 states no meaning for the null case. The external design corpus does, at
    /// <c>13-configuration-dx.md:92</c>, where the same member is annotated "null = the
    /// system default". That is the corpus and not this repository's design layer, so it is
    /// recorded as the corpus reading rather than as a stated decision here.
    /// </remarks>
    public TimeSpan? AbsoluteRefreshLifetime { get; set; }

    /// <summary>
    /// How this client authenticates. Defaults to
    /// <see cref="ClientAuthMethod.PrivateKeyJwt"/>, which design 23 section 3 states.
    /// </summary>
    public ClientAuthMethod AuthMethod { get; set; } = ClientAuthMethod.PrivateKeyJwt;

    /// <summary>
    /// The public JSON Web Key set for the private-key-JWT path. Null when no key set is
    /// supplied. Design 23 section 5.1 invariant 1 makes a client confidential on a secret
    /// <b>or</b> a key set, so it does not forbid both being present.
    /// </summary>
    /// <remarks>
    /// This lands on the descriptor's own JSON Web Key set property, not in the property
    /// bag. Design 23 section 5.5 records that the property-bag reading was suspected and
    /// refuted at source: the descriptor declares a settable, nullable property directly.
    /// </remarks>
    public string? JwksJson { get; set; }
}
