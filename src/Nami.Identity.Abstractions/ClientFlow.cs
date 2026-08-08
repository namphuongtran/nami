namespace Nami.Identity.Abstractions;

/// <summary>
/// The flow a client uses, which is what the mapper translates into the engine's endpoint,
/// grant-type, and response-type permission sets.
/// </summary>
/// <remarks>
/// <para>
/// The four members and their order are transcribed from the class diagram in design 23
/// section 3, which that document declares its own implementer source of record. Writing
/// one enum value instead of nine lines of permissions is the point of the declaration
/// layer, and design 23 section 5.2 holds the translation table.
/// </para>
/// <para>
/// <b>There is no password grant, and its absence is the guarantee.</b> Design 23 section 3
/// states both reasons. Resource-owner password credentials are deprecated in the current
/// OAuth best practice, and the flow is structurally incompatible with the rest of the
/// design, because it cannot carry multi-factor or step-up and cannot carry a passkey.
/// ADR-0013 owns the step-up mechanism and ADR-0028 owns passkeys. Neither rules on the
/// grant, so the incompatibility is design 23's own reasoning rather than a decision to
/// quote. Omitting the member means a consumer cannot request it, which is stronger than
/// documenting that they should not.
/// </para>
/// <para>
/// <b>The ordinals are explicit because the configuration binder can bind them.</b> The
/// Microsoft binder accepts a numeric string for an enum member, so a settings file may
/// carry <c>"Flow": 3</c>. Writing the values down means a reorder changes a number a
/// reader can see, rather than silently repointing every such entry.
/// <c>PublicAPI.Unshipped.txt</c> already recorded <c>= 0</c> through <c>= 3</c>, so this
/// only makes the source agree with the contract file.
/// </para>
/// </remarks>
public enum ClientFlow
{
    /// <summary>
    /// The authorization code flow. Design 23 section 5.2 permits the authorization,
    /// end-session, and token endpoints, with the authorization code grant plus the refresh
    /// grant when <see cref="ClientDefinition.IssueRefreshToken"/> is set, and the
    /// <c>code</c> response type.
    /// </summary>
    Code = 0,

    /// <summary>
    /// The client credentials flow, for a machine-to-machine client. Design 23 section 5.2
    /// permits the token endpoint only, with the client credentials grant and no response
    /// type. That row states no refresh clause, unlike <see cref="Code"/> and
    /// <see cref="DeviceCode"/>, so <see cref="ClientDefinition.IssueRefreshToken"/> does
    /// not reach this flow.
    /// </summary>
    ClientCredentials = 1,

    /// <summary>
    /// Both of the above. Design 23 section 5.2 gives the endpoints and grant types as the
    /// union of the two, with the <c>code</c> response type and the requirements of
    /// <see cref="Code"/>.
    /// </summary>
    CodeAndClientCredentials = 2,

    /// <summary>
    /// The device authorization flow. Design 23 section 5.2 permits the token and device
    /// authorization endpoints, with the device code grant plus the refresh grant when
    /// <see cref="ClientDefinition.IssueRefreshToken"/> is set.
    /// </summary>
    /// <remarks>
    /// It declares <b>no response type</b>, because it never travels through a browser
    /// authorization response. Design 23 section 5.2 states that adding one is
    /// "harmless-looking and wrong".
    /// </remarks>
    DeviceCode = 3,
}
