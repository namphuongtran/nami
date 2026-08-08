using Nami.Identity.Abstractions;
using Xunit;

namespace Nami.Identity.UnitTests;

/// <summary>
/// The defaults of <see cref="ClientDefinition"/>, which design 23 section 8 calls the entire
/// security argument for the declaration layer: "every field whose wrong value would weaken a
/// client defaults to the safe value".
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing else in this repository sees a changed default.</b> Design 23 section 7 makes
/// changing one a behaviour break under ADR-0044, but the public-API analyzer records
/// <c>RequirePkce.get -&gt; bool</c> and never an initializer, so flipping <c>= true</c> to
/// <c>= false</c> produces no API diff. Measured 2026-08-08 on SDK 10.0.301 with the value
/// flipped: <c>dotnet build</c> stayed green with zero warnings and
/// <c>PublicAPI.Unshipped.txt</c> stayed byte-identical, while the matching fact below failed.
/// These tests are the only gate on the values.
/// </para>
/// <para>
/// <b>What that covers, stated as a boundary rather than left to be assumed.</b>
/// <see cref="ClientDefinition"/> has seventeen members. Fifteen have a default and each has a
/// fact here. The two that do not are <see cref="ClientDefinition.ClientId"/> and
/// <see cref="ClientDefinition.DisplayName"/>, which are <c>required</c>, so there is no default
/// value to pin. The eleven facts below therefore close the initializer class for this type. Four
/// of them were added after review found that the suite had left the nullable members out,
/// including both halves of the credential derivation.
/// </para>
/// <para>
/// <b>Six of the eight facts are sourced and two are not.</b> Design 23 section 3 states a
/// default for six members in the prose under its class diagram. The empty-array fact and the
/// <see cref="ClientDefinition.Flow"/> fact rest on choices this repository made, and each says
/// so in its own remarks. A test is a durable artifact, so an unsourced fact that does not
/// label itself becomes evidence that a decision was taken.
/// </para>
/// <para>
/// No ASVS requirement identifier is written here. ADR-0062 binds a security test to name one,
/// and both that clause and design 20 section 3.2 give <em>negative</em> tests as their
/// examples: the spoofed client-certificate rejection and the cross-tenant validation failure.
/// A positive defaults test is not that shape, and ADR-0062 owns the tagging as a build-time
/// item. Guessing a 5.0 identifier is also the defect design 20 section 7 names, "a 4.x number
/// under a 5.0 label ... presented as a fact". Two sources say the mapping is owed rather than
/// automatic: ADR-0062 records that an earlier ASVS citation "used the 4.x chapter numbering",
/// and design 20 section 10 makes mapping the 4.x numbers onto their 5.0 equivalents part of the
/// self-assessment "rather than assuming they carried over".
/// </para>
/// </remarks>
public sealed class ClientDefinitionDefaultsTests
{
    /// <summary>
    /// A definition carrying nothing but the two members the compiler forces. Every fact below
    /// reads the untouched value of some other member from it.
    /// </summary>
    /// <remarks>
    /// The values are synthetic and the name is neutral, which design 20 section 4 makes a
    /// constraint rather than a convention: a fixture feels private and is not.
    /// </remarks>
    private static ClientDefinition MinimallyDeclaredClient() => new()
    {
        ClientId = "client-a",
        DisplayName = "Client A",
    };

    /// <summary>
    /// Design 23 section 3: "<c>RequirePkce</c> defaults to <b>true</b>".
    /// </summary>
    [Fact]
    public void GivenAClientDefinitionWithOnlyItsRequiredMembersSet_WhenTheProofKeySettingIsRead_ThenItIsRequired() =>
        Assert.True(MinimallyDeclaredClient().RequirePkce);

    /// <summary>
    /// Design 23 section 3: "<c>AuthMethod</c> defaults to <b><c>PrivateKeyJwt</c></b>, so the
    /// secure choice is the one you get by omission".
    /// </summary>
    [Fact]
    public void GivenAClientDefinitionWithOnlyItsRequiredMembersSet_WhenTheAuthenticationMethodIsRead_ThenItIsPrivateKeyJwt() =>
        Assert.Equal(ClientAuthMethod.PrivateKeyJwt, MinimallyDeclaredClient().AuthMethod);

    /// <summary>
    /// Design 23 section 3: "<c>IssueRefreshToken</c> defaults to true and a machine-to-machine
    /// client should set it false".
    /// </summary>
    [Fact]
    public void GivenAClientDefinitionWithOnlyItsRequiredMembersSet_WhenTheRefreshSettingIsRead_ThenTheRefreshGrantIsPermitted() =>
        Assert.True(MinimallyDeclaredClient().IssueRefreshToken);

    /// <summary>
    /// Design 23 section 3: "<c>AccessTokenType</c> defaults to <c>jwt</c>". The safety of this
    /// one is not about the token: opting a client into reference tokens forces that client's
    /// resource server onto introspection, because an opaque token cannot be validated locally.
    /// </summary>
    [Fact]
    public void GivenAClientDefinitionWithOnlyItsRequiredMembersSet_WhenTheAccessTokenTypeIsRead_ThenItIsJwt() =>
        Assert.Equal("jwt", MinimallyDeclaredClient().AccessTokenType);

    /// <summary>
    /// Design 23 section 3: "<c>RequireConsent</c> defaults to false".
    /// </summary>
    /// <remarks>
    /// The value is also the C# default for <see cref="bool"/>, so the type writes no
    /// initializer for it. That is exactly why the fact is worth asserting: there is no line in
    /// the source to read, and a future initializer flipping it would be a one-word change.
    /// </remarks>
    [Fact]
    public void GivenAClientDefinitionWithOnlyItsRequiredMembersSet_WhenTheConsentSettingIsRead_ThenConsentIsNotRequired() =>
        Assert.False(MinimallyDeclaredClient().RequireConsent);

    /// <summary>
    /// Design 23 section 3: "<c>IsNativeApp</c> defaults to false". The same no-initializer
    /// reasoning as the consent fact above applies.
    /// </summary>
    [Fact]
    public void GivenAClientDefinitionWithOnlyItsRequiredMembersSet_WhenTheNativeApplicationSettingIsRead_ThenItIsFalse() =>
        Assert.False(MinimallyDeclaredClient().IsNativeApp);

    /// <summary>
    /// An undeclared client grants nothing: all four collection members are empty rather than
    /// null.
    /// </summary>
    /// <remarks>
    /// <b>This fact is a recorded choice, not a design statement.</b> Design 23 fixes the four
    /// members as non-nullable arrays and states no default for any of them. The
    /// <c>= []</c> initializers were chosen because empty is the deny-by-default value and
    /// because <c>required</c> would reject a client-credentials client, which section 5.2
    /// gives the token endpoint only. Asserting it here keeps a later change to <c>null</c>
    /// visible, since that would turn every consumer's first read into a dereference of
    /// nothing.
    /// </remarks>
    [Fact]
    public void GivenAClientDefinitionWithOnlyItsRequiredMembersSet_WhenItsCollectionsAreRead_ThenNothingIsGranted()
    {
        ClientDefinition client = MinimallyDeclaredClient();

        // Assert.Multiple, so all four report rather than the first failure
        // hiding the other three. ADR-0060 records that this method is already in
        // the pinned xunit.v3.assert, and that its presence is half of why no
        // assertion package was taken. Four sequential Assert.Empty calls put
        // four members behind one failure slot, which is the "bundle of items
        // behind one reference" shape the root CLAUDE.md names.
        Assert.Multiple(
            () => Assert.Empty(client.RedirectUris),
            () => Assert.Empty(client.PostLogoutRedirectUris),
            () => Assert.Empty(client.AllowedScopes),
            () => Assert.Empty(client.AllowedCorsOrigins));
    }

    /// <summary>
    /// An undeclared client carries no credential, so design 23 section 5.1 invariant 1 derives a
    /// <b>public</b> client from it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the fact with the largest blast radius, and the suite was landed without it.</b>
    /// Invariant 1 at <c>23:191-193</c> is a derivation rather than a check: "A client is
    /// confidential if it has a secret <b>or</b> a JWK set, and public otherwise." Invariant 2
    /// then forces the proof key onto a <em>public</em> code client. So a non-null default on
    /// either credential moves every undeclared client onto the confidential branch, and the
    /// forced proof key never applies to it. That reaches the same control as
    /// <see cref="ClientDefinition.RequirePkce"/> by a second route.
    /// </para>
    /// <para>
    /// Measured 2026-08-08 before this fact existed, with <c>ClientSecret</c> given
    /// <c>= "s3cret"</c>: <c>dotnet build</c> reported 0 warnings and 0 errors,
    /// <c>PublicAPI.Unshipped.txt</c> was byte-identical, and all fourteen facts in the solution
    /// passed. Nothing in the repository saw it.
    /// </para>
    /// <para>
    /// The assertion is on the pair rather than on one member, because invariant 1 reads them
    /// with <b>or</b>. A fact on one alone would pass while the other flipped the derivation.
    /// </para>
    /// </remarks>
    [Fact]
    public void GivenAClientDefinitionWithOnlyItsRequiredMembersSet_WhenItsCredentialIsRead_ThenNeitherIsPresentSoTheClientIsPublic()
    {
        ClientDefinition client = MinimallyDeclaredClient();

        Assert.Multiple(
            () => Assert.Null(client.ClientSecret),
            () => Assert.Null(client.JwksJson));
    }

    /// <summary>
    /// An undeclared client names no tenant, which is the untenanted case the host start-up form
    /// of the seeder handles.
    /// </summary>
    /// <remarks>
    /// Design 23 section 8 states the failure a wrong value here causes, and it is the kind that
    /// does not announce itself: seeding under the wrong ambient context "does not error. It
    /// writes a perfectly valid client into the wrong tenant." No source states a default, so
    /// this pins the C# one so that giving it a value becomes visible.
    /// </remarks>
    [Fact]
    public void GivenAClientDefinitionWithOnlyItsRequiredMembersSet_WhenTheTenantIsRead_ThenTheDefinitionIsUntenanted() =>
        Assert.Null(MinimallyDeclaredClient().TenantId);

    /// <summary>
    /// An undeclared client sets no refresh ceiling of its own.
    /// </summary>
    /// <remarks>
    /// Design 23 section 4 bounds this value by the system ceiling in ADR-0004 and states no
    /// meaning for the null case. The external corpus annotates the same member "null = the
    /// system default" at <c>13-configuration-dx.md:92</c>, which is the corpus and not this
    /// repository's design layer. So this fact pins the C# default and claims nothing about what
    /// null means.
    /// </remarks>
    [Fact]
    public void GivenAClientDefinitionWithOnlyItsRequiredMembersSet_WhenTheRefreshCeilingIsRead_ThenItSetsNoneOfItsOwn() =>
        Assert.Null(MinimallyDeclaredClient().AbsoluteRefreshLifetime);

    /// <summary>
    /// An undeclared client uses the authorization code flow.
    /// </summary>
    /// <remarks>
    /// <b>No source states this default, and this fact does not create one.</b> Design 23
    /// section 3 states a default for six members and states none for
    /// <see cref="ClientDefinition.Flow"/>. The value is what C# produces from the member order
    /// of <see cref="ClientFlow"/>, which the type writes out as an initializer so the line
    /// cannot be mistaken for a decision. The fact exists so that a change to the member order
    /// or to the initializer fails here rather than silently repointing every client that never
    /// declared a flow. Read it as pinning current behaviour, never as evidence that a default
    /// was decided.
    /// </remarks>
    [Fact]
    public void GivenAClientDefinitionWithOnlyItsRequiredMembersSet_WhenTheFlowIsRead_ThenItIsTheAuthorizationCodeFlow() =>
        Assert.Equal(ClientFlow.Code, MinimallyDeclaredClient().Flow);
}
