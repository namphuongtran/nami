using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nami.Identity.Core;
using Xunit;

namespace Nami.Identity.UnitTests;

/// <summary>
/// The fail-closed half of the builder surface: a missing required option stops the host rather
/// than surfacing on the first request that needs it.
/// </summary>
/// <remarks>
/// <para>
/// <b>These facts exist because the alternative is this repository's most expensive failure
/// mode.</b> ADR-0096 parameter B removes the <c>required</c> modifier from
/// <see cref="NamiIdentityOptions.ConnectionString"/> and
/// <see cref="NamiIdentityOptions.Issuer"/>, and parameter C moves the guarantee onto a start-up
/// validator instead. An untested validator would be a control that reads as enforcement while
/// enforcing nothing, which is the shape four of this repository's nine gates were written after.
/// </para>
/// <para>
/// <b>A taxonomy question is left open rather than answered quietly.</b> Design 20 section 3.1
/// describes the unit row as "domain logic and handlers in isolation, no container", and these two
/// facts build a service collection. Nothing external is involved, no process is started and no
/// container image is pulled, so the spirit holds and the letter is arguable. ADR-0060 already owes
/// a confirmation of the taxonomy against the real suites at M1, and this is one more input to it.
/// </para>
/// <para>
/// <b>Validation runs on first read, not on registration.</b> Reading
/// <see cref="IOptions{TOptions}.Value"/> is what makes the options factory build and validate the
/// instance, so the assertion is on the read rather than on
/// <see cref="ServiceCollection.BuildServiceProvider()"/>. <c>ValidateOnStart</c> moves the same
/// check to host start-up, which needs a host and belongs to the increment that has one.
/// </para>
/// </remarks>
public sealed class NamiIdentityOptionsValidationTests
{
    /// <summary>
    /// A service collection carrying an empty configuration, which is what
    /// <c>AddNamiIdentity</c> binds from.
    /// </summary>
    /// <remarks>
    /// The configuration is empty on purpose. It puts the delegate in sole control of the values,
    /// which is what each fact below is about.
    /// </remarks>
    private static ServiceCollection ServicesWithEmptyConfiguration()
    {
        ServiceCollection services = [];
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        return services;
    }

    private static NamiIdentityOptions ResolveOptions(ServiceCollection services) =>
        services.BuildServiceProvider().GetRequiredService<IOptions<NamiIdentityOptions>>().Value;

    /// <summary>
    /// Design 01 section 5.3: "a missing value crashes the host at boot rather than surfacing
    /// lazily on the first request that needs it".
    /// </summary>
    [Fact]
    public void GivenNoOptionsAreConfigured_WhenTheOptionsAreResolved_ThenValidationFails()
    {
        ServiceCollection services = ServicesWithEmptyConfiguration();
        services.AddNamiIdentity(_ => { });

        Assert.Throws<OptionsValidationException>(() => ResolveOptions(services));
    }

    /// <summary>
    /// Supplying only one of the two is still a failure. Asserted separately because a validator
    /// that stopped at the first check would pass the fact above and let this case through.
    /// </summary>
    [Fact]
    public void GivenOnlyTheConnectionStringIsConfigured_WhenTheOptionsAreResolved_ThenValidationFails()
    {
        ServiceCollection services = ServicesWithEmptyConfiguration();
        services.AddNamiIdentity(o => o.ConnectionString = "Host=localhost;Database=nami");

        Assert.Throws<OptionsValidationException>(() => ResolveOptions(services));
    }

    /// <summary>
    /// Whitespace is not a value. Asserted because <c>string.IsNullOrEmpty</c> would accept it and
    /// the difference is invisible in a settings file.
    /// </summary>
    [Fact]
    public void GivenARequiredOptionIsWhitespace_WhenTheOptionsAreResolved_ThenValidationFails()
    {
        ServiceCollection services = ServicesWithEmptyConfiguration();
        services.AddNamiIdentity(o =>
        {
            o.ConnectionString = "Host=localhost;Database=nami";
            o.Issuer = "   ";
        });

        Assert.Throws<OptionsValidationException>(() => ResolveOptions(services));
    }

    /// <summary>
    /// The positive case, which is what stops the three facts above passing for the wrong reason.
    /// A validator that rejected everything would satisfy all three.
    /// </summary>
    [Fact]
    public void GivenBothRequiredOptionsAreConfigured_WhenTheOptionsAreResolved_ThenValidationPasses()
    {
        ServiceCollection services = ServicesWithEmptyConfiguration();
        services.AddNamiIdentity(o =>
        {
            o.ConnectionString = "Host=localhost;Database=nami";
            o.Issuer = "https://id.example.com";
        });

        Assert.Equal("https://id.example.com", ResolveOptions(services).Issuer);
    }

    /// <summary>
    /// ADR-0096 parameter E: the delegate runs after configuration binding and wins over it.
    /// </summary>
    /// <remarks>
    /// This is the fact behind the consequence that parameter E states out loud, that a value
    /// written in the delegate cannot be overridden by an environment variable. It is asserted here
    /// rather than described, because the order is a registration detail that a refactor could
    /// reverse without any other fact noticing.
    /// </remarks>
    [Fact]
    public void GivenConfigurationAndTheDelegateBothSetAnOption_WhenTheOptionsAreResolved_ThenTheDelegateWins()
    {
        ServiceCollection services = [];
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nami:Protocol:AccessTokenLifetime"] = "00:07:00",
            })
            .Build());

        services.AddNamiIdentity(o =>
        {
            o.ConnectionString = "Host=localhost;Database=nami";
            o.Issuer = "https://id.example.com";
            o.AccessTokenLifetime = TimeSpan.FromMinutes(3);
        });

        Assert.Equal(TimeSpan.FromMinutes(3), ResolveOptions(services).AccessTokenLifetime);
    }

    /// <summary>
    /// The other half of parameter E, and the one that proves the binding happens at all: with no
    /// delegate touching the member, the configured value survives.
    /// </summary>
    /// <remarks>
    /// Without this fact the fact above would also pass if <c>BindConfiguration</c> were deleted
    /// outright, since the delegate's value would win by being the only writer.
    /// </remarks>
    [Fact]
    public void GivenOnlyConfigurationSetsAnOption_WhenTheOptionsAreResolved_ThenTheConfiguredValueIsUsed()
    {
        ServiceCollection services = [];
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nami:Protocol:AccessTokenLifetime"] = "00:07:00",
            })
            .Build());

        services.AddNamiIdentity(o =>
        {
            o.ConnectionString = "Host=localhost;Database=nami";
            o.Issuer = "https://id.example.com";
        });

        Assert.Equal(TimeSpan.FromMinutes(7), ResolveOptions(services).AccessTokenLifetime);
    }
}
