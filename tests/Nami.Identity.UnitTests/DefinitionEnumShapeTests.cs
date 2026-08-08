using Nami.Identity.Abstractions;
using Xunit;

namespace Nami.Identity.UnitTests;

/// <summary>
/// The shape of the two enums the definition model binds: their members, their ordinals, and
/// the one member that is deliberately absent.
/// </summary>
/// <remarks>
/// <para>
/// <b>An ordinal here is load-bearing rather than cosmetic, for two reasons.</b> The first is
/// the CLR default of an enum type being ordinal 0, so the member sitting there is what an
/// uninitialized field or a <c>default</c> expression produces. That one is verified by the
/// first fact below. The second is the configuration binder accepting a numeric string for an
/// enum member, so a settings file may carry <c>"Flow": 3</c> and a reorder would repoint every
/// such entry. <b>That second reason is recorded in <c>ClientFlow</c> and is not verified in
/// this repository</b>, which holds no configuration code and no binder package yet. It is
/// carried as the reason the ordinals were written out, not as a measured fact.
/// </para>
/// <para>
/// <b>The public-API gate stops a reorder, and that is not the same as catching it.</b>
/// Measured 2026-08-08: swapping the two <see cref="ClientAuthMethod"/> ordinals fails the
/// build with two <c>RS0016</c> and two <c>RS0017</c> errors, because
/// <c>PublicAPI.Unshipped.txt</c> records <c>PrivateKeyJwt = 0</c> and <c>ClientSecret = 1</c>.
/// The same commit that reorders the enum updates that file, which is the ordinary way to
/// clear those errors. Measured again with the file updated to match: the build is green, and
/// the diff is two numbers changing. Nothing in it says which of the two is the safe
/// credential, so it does not read as a security change. Two facts below failed on that run.
/// </para>
/// <para>
/// The ordinal facts assert names, values, order, and count in one list each, so a member added
/// anywhere fails rather than passing on a partial match.
/// </para>
/// </remarks>
public sealed class DefinitionEnumShapeTests
{
    /// <summary>
    /// The CLR default of <see cref="ClientAuthMethod"/> is the asymmetric credential.
    /// </summary>
    /// <remarks>
    /// Design 23 section 3 states that <see cref="ClientDefinition.AuthMethod"/> defaults to
    /// <see cref="ClientAuthMethod.PrivateKeyJwt"/>, "so the secure choice is the one you get by
    /// omission". <see cref="ClientDefinitionDefaultsTests"/> asserts that on the property,
    /// where the initializer puts it. This fact asserts it on the enum, which is the second
    /// place the value comes from and the one that answers for any future member the property
    /// initializer does not reach.
    /// </remarks>
    [Fact]
    public void GivenNoAuthenticationMethodIsDeclared_WhenTheEnumDefaultIsTaken_ThenItIsTheAsymmetricCredential() =>
        Assert.Equal(ClientAuthMethod.PrivateKeyJwt, default);

    /// <summary>
    /// The two members of <see cref="ClientAuthMethod"/>, in the order the class diagram in
    /// design 23 section 3 gives, at the ordinals the enum writes out.
    /// </summary>
    /// <remarks>
    /// <b>The diagram gives the order and states no ordinal.</b> It lists bare member names, so
    /// the numbers asserted here are the recorded choice <c>src/CLAUDE.md</c> carries rather
    /// than a design statement. Pinning them is what keeps a reorder from being silent.
    /// </remarks>
    [Fact]
    public void GivenTheAuthenticationMethodEnum_WhenItsMembersAreRead_ThenTheyAreTheTwoMembersAtTheirRecordedOrdinals()
    {
        string[] expected = ["PrivateKeyJwt=0", "ClientSecret=1"];

        string[] actual = [.. Enum.GetValues<ClientAuthMethod>().Select(m => $"{m}={(int)m}")];

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// The four members of <see cref="ClientFlow"/>, in the order the class diagram in design 23
    /// section 3 gives, at the ordinals the enum writes out.
    /// </summary>
    /// <remarks>
    /// The same split as the fact above: the diagram states the order and no ordinal. Note also
    /// that this fact asserts the whole member list, so <b>adding</b> a flow fails it, even though
    /// ADR-0044 parameter B makes an additive change a MINOR version. That is deliberate and
    /// matches how <c>PublicAPI.Unshipped.txt</c> behaves. (Design 23 section 7 says the same of
    /// a field added to a <em>definition</em>, which is a different subject: an enum member is
    /// not a definition field, so ADR-0044 is the owner here.) Update the expected
    /// list as part of the addition rather than reaching for a looser assertion.
    /// </remarks>
    [Fact]
    public void GivenTheClientFlowEnum_WhenItsMembersAreRead_ThenTheyAreTheFourMembersAtTheirRecordedOrdinals()
    {
        string[] expected =
        [
            "Code=0",
            "ClientCredentials=1",
            "CodeAndClientCredentials=2",
            "DeviceCode=3",
        ];

        string[] actual = [.. Enum.GetValues<ClientFlow>().Select(f => $"{f}={(int)f}")];

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// <see cref="ClientFlow"/> offers no resource-owner password credentials grant.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Design 23 section 3 states both reasons and states the guarantee: the grant is
    /// deprecated in current OAuth best practice, it cannot carry multi-factor, step-up, or a
    /// passkey, and "omitting it from the enum means a consumer cannot request it, which is a
    /// stronger guarantee than documenting that they should not". The guarantee is the absence,
    /// so the absence is what is asserted.
    /// </para>
    /// <para>
    /// <b>An absence asserted by matching one word is an absence asserted by spelling, so the
    /// spellings are enumerated and written down.</b> The first version of this fact matched
    /// <c>Password</c> alone, and review broke it by adding <c>ResourceOwnerCredentials = 4</c>,
    /// which is the grant's own name in RFC 6749: measured 2026-08-08, this fact <b>passed</b> and
    /// only the member-list fact above failed. The three spellings below are the ones a member
    /// adding this grant would plausibly carry, and they are listed rather than counted. The count assertion is what makes the negative
    /// half non-vacuous, since <see cref="Assert.DoesNotContain{T}(System.Collections.Generic.IEnumerable{T}, System.Predicate{T})"/>
    /// holds on an empty sequence by construction.
    /// </para>
    /// <para>
    /// The member-list fact above is the stronger guard, because it fails on any addition
    /// whatever it is called. This fact exists so that the failure message names the reason
    /// rather than only the diff, and it must not be treated as the only guard: updating the
    /// expected list there without reading this would remove the reason from the record.
    /// </para>
    /// </remarks>
    [Fact]
    public void GivenTheClientFlowEnum_WhenItsMembersAreRead_ThenNoneIsAResourceOwnerPasswordGrant()
    {
        // "Credentials" is deliberately NOT here: ClientCredentials and
        // CodeAndClientCredentials are legal members and would match it.
        string[] spellings = ["Password", "Ropc", "ResourceOwner"];

        string[] names = Enum.GetNames<ClientFlow>();

        Assert.Multiple(
            () => Assert.Equal(4, names.Length),
            () => Assert.DoesNotContain(
                names,
                name => spellings.Any(s => name.Contains(s, StringComparison.OrdinalIgnoreCase))));
    }
}
