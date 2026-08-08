using Nami.Identity.Core;
using Xunit;

namespace Nami.Identity.UnitTests;

/// <summary>
/// The defaults of <see cref="NamiIdentityOptions"/>, which design 01 section 3.4 states as "every
/// default is the safe value".
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing else sees a changed default here either</b>, for the same reason recorded on
/// <see cref="ClientDefinitionDefaultsTests"/>: the public-API analyzer records
/// <c>RequireHttps.get -&gt; bool</c> and never an initializer, so removing <c>= true</c> produces
/// no API diff. These facts are the only gate on the values.
/// </para>
/// <para>
/// <b>Twelve members, twelve facts, and that is the whole class.</b> ADR-0096 fixes twelve
/// properties. Ten carry a value worth pinning. The two that read <see langword="null"/> are pinned
/// as well rather than skipped, because the start-up validator's whole job is to reject exactly
/// that state, so a future initializer quietly giving either one a value would disarm the
/// validator while every other fact here still passed.
/// </para>
/// <para>
/// <b>Two defaults have no initializer to read, and that is why they are asserted.</b>
/// <see cref="NamiIdentityOptions.AccessTokenEncryption"/> and
/// <see cref="NamiIdentityOptions.MigrateOnStartup"/> both default to <see langword="false"/>, and
/// writing <c>= false</c> trips <c>CA1805</c>, which ADR-0093 makes an error. So the source carries
/// no line to read for either, and the fact is the only statement of the value.
/// </para>
/// <para>
/// No ASVS requirement identifier is written here, on the same reasoning
/// <see cref="ClientDefinitionDefaultsTests"/> records at length: ADR-0062 gives negative tests as
/// its examples, ASVS 5.0 renumbered its chapters, and guessing an identifier is the defect design
/// 20 names.
/// </para>
/// </remarks>
public sealed class NamiIdentityOptionsDefaultsTests
{
    /// <summary>
    /// An options object as the options system builds it, with nothing configured.
    /// </summary>
    /// <remarks>
    /// A bare <c>new()</c> is the right fixture rather than a shortcut. ADR-0096 parameter B rests
    /// on the options system creating this type through <c>Activator.CreateInstance</c> rather than
    /// through an object initializer, so a fixture that set members would be testing a path the
    /// runtime does not take.
    /// </remarks>
    private static NamiIdentityOptions UnconfiguredOptions() => new();

    /// <summary>
    /// Design 01 section 3.4 marks it required, and ADR-0096 parameter B keeps it nullable so the
    /// validator rather than the compiler enforces it.
    /// </summary>
    [Fact]
    public void GivenUnconfiguredOptions_WhenTheConnectionStringIsRead_ThenItIsAbsent() =>
        Assert.Null(UnconfiguredOptions().ConnectionString);

    /// <summary>As above, for the second of the two required options.</summary>
    [Fact]
    public void GivenUnconfiguredOptions_WhenTheIssuerIsRead_ThenItIsAbsent() =>
        Assert.Null(UnconfiguredOptions().Issuer);

    /// <summary>
    /// ADR-0005: "Signing-algorithm baseline = RS256, with ES256 selectable by configuration".
    /// </summary>
    [Fact]
    public void GivenUnconfiguredOptions_WhenTheSigningAlgorithmIsRead_ThenItIsTheRsaBaseline() =>
        Assert.Equal(SigningAlgorithm.RS256, UnconfiguredOptions().SigningAlgorithm);

    /// <summary>ADR-0004: "access-token lifetime 15 minutes".</summary>
    [Fact]
    public void GivenUnconfiguredOptions_WhenTheAccessTokenLifetimeIsRead_ThenItIsFifteenMinutes() =>
        Assert.Equal(TimeSpan.FromMinutes(15), UnconfiguredOptions().AccessTokenLifetime);

    /// <summary>ADR-0004: "refresh-token absolute lifetime ceiling 8 hours".</summary>
    [Fact]
    public void GivenUnconfiguredOptions_WhenTheRefreshTokenLifetimeIsRead_ThenItIsEightHours() =>
        Assert.Equal(TimeSpan.FromHours(8), UnconfiguredOptions().RefreshTokenLifetime);

    /// <summary>ADR-0003: "inactivity (sliding) 1 hour".</summary>
    [Fact]
    public void GivenUnconfiguredOptions_WhenTheSessionInactivityWindowIsRead_ThenItIsOneHour() =>
        Assert.Equal(TimeSpan.FromHours(1), UnconfiguredOptions().SessionInactivity);

    /// <summary>ADR-0003: "absolute 8 hours; past the absolute limit, re-authentication is required".</summary>
    [Fact]
    public void GivenUnconfiguredOptions_WhenTheAbsoluteSessionLimitIsRead_ThenItIsEightHours() =>
        Assert.Equal(TimeSpan.FromHours(8), UnconfiguredOptions().SessionAbsolute);

    /// <summary>
    /// ADR-0005: "<c>DisableAccessTokenEncryption()</c> is enabled: the access token is a plain
    /// signed JWT".
    /// </summary>
    /// <remarks>
    /// The safe direction here is counter-intuitive and worth stating. Encryption off is the
    /// decided posture, because a resource server validates the token locally with JWKS; what the
    /// decision buys is paid for by the minimal claim set ADR-0005 mandates in the same breath.
    /// </remarks>
    [Fact]
    public void GivenUnconfiguredOptions_WhenTheAccessTokenEncryptionSettingIsRead_ThenItIsOff() =>
        Assert.False(UnconfiguredOptions().AccessTokenEncryption);

    /// <summary>
    /// Design 01 section 3.4: "<c>true</c>, relaxed only in development", which realizes ADR-0076's
    /// rule that the engine's transport-security opt-out is "forbidden in any environment other
    /// than Development".
    /// </summary>
    [Fact]
    public void GivenUnconfiguredOptions_WhenTheTransportSettingIsRead_ThenHttpsIsRequired() =>
        Assert.True(UnconfiguredOptions().RequireHttps);

    /// <summary>
    /// ADR-0012 chooses "first-key seeding by auto-seed". Design 01 section 3.4 names this one of
    /// the two defaults that would be dangerous the other way: a host that did not auto-seed would
    /// come up unable to sign.
    /// </summary>
    [Fact]
    public void GivenUnconfiguredOptions_WhenTheKeySeedingSettingIsRead_ThenTheFirstKeyIsSeeded() =>
        Assert.True(UnconfiguredOptions().AutoSeedFirstKey);

    /// <summary>
    /// ADR-0017 rejects start-up migration, "because a startup migrate is discouraged and unsafe at
    /// fleet scale". Design 01 section 3.4 names this the other dangerous-if-flipped default: a
    /// host that migrated at start-up would race its own replicas.
    /// </summary>
    [Fact]
    public void GivenUnconfiguredOptions_WhenTheMigrationSettingIsRead_ThenMigrationsDoNotRunAtStartup() =>
        Assert.False(UnconfiguredOptions().MigrateOnStartup);

    /// <summary>
    /// ADR-0032 makes the registration key "a nudge, not a gate", so its absence is the default and
    /// blocks nothing.
    /// </summary>
    [Fact]
    public void GivenUnconfiguredOptions_WhenTheRegistrationKeyIsRead_ThenItIsAbsent() =>
        Assert.Null(UnconfiguredOptions().RegistrationKey);
}
