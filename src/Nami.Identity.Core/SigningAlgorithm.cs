namespace Nami.Identity.Core;

/// <summary>
/// The signing algorithm the issuance pipeline uses.
/// </summary>
/// <remarks>
/// <para>
/// The domain is closed at two values by ADR-0005, which fixes the baseline at
/// <c>RS256</c> "with ES256 selectable by configuration through the signing
/// credential source". ADR-0096 makes it an enum rather than a string,
/// because this repository has already recorded the string form of a closed
/// domain as a defect: an unrecognized value falls back silently to whichever
/// branch the reader happens to take.
/// </para>
/// <para>
/// <b>The ordinals are explicit on purpose.</b> Reordering the members of an
/// enum whose values are implicit repoints every previously written value while
/// the public-API diff shows only <c>= 0</c> becoming <c>= 1</c>, which does not
/// read as a security change. The configuration binder also accepts a numeric
/// string for an enum member, so a settings file can carry a bare ordinal.
/// </para>
/// </remarks>
public enum SigningAlgorithm
{
    /// <summary>RSASSA-PKCS1-v1_5 using SHA-256, the baseline.</summary>
    RS256 = 0,

    /// <summary>ECDSA using P-256 and SHA-256, selectable by configuration.</summary>
    ES256 = 1,
}
