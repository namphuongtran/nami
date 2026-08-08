namespace Nami.Identity.Abstractions;

/// <summary>
/// How a confidential client authenticates to the token endpoint.
/// </summary>
/// <remarks>
/// <para>
/// The two members are transcribed from the class diagram in design 23 section 3, which
/// that document declares its own implementer source of record.
/// </para>
/// <para>
/// <b>The default is written as an initializer and not as an ordinal.</b> Design 23 section
/// 3 states that <see cref="ClientDefinition.AuthMethod"/> defaults to
/// <see cref="PrivateKeyJwt"/>, "so the secure choice is the one you get by omission". That
/// default lives on the property, so reordering this enum no longer moves an undeclared
/// client onto the weaker credential. The ordinals are written out for the same reason
/// <see cref="ClientFlow"/> writes its own: the configuration binder accepts a numeric
/// string for an enum member.
/// </para>
/// <para>
/// The preference is read at the engine's own source rather than asserted as a house style.
/// Design 23 section 5.6 records that the application descriptor documents shared-secret
/// client authentication as <b>not recommended</b> and existing mainly for legacy clients.
/// ADR-0035 carries the same reading of the descriptor. ADR-0009 owns the preference itself,
/// which is <c>private_key_jwt</c> for service clients, and it does not rule on what the
/// descriptor documents.
/// </para>
/// </remarks>
public enum ClientAuthMethod
{
    /// <summary>
    /// Asymmetric authentication with a private key JSON Web Token, proved against the
    /// public key set in <see cref="ClientDefinition.JwksJson"/>. This is the default.
    /// </summary>
    PrivateKeyJwt = 0,

    /// <summary>
    /// A symmetric shared secret. It is legal and sometimes unavoidable, so design 23
    /// section 5.1 invariant 6 warns rather than throws when a machine-to-machine client
    /// uses one.
    /// </summary>
    ClientSecret = 1,
}
