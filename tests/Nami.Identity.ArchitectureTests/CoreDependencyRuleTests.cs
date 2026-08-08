using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Nami.Identity.Core;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Nami.Identity.ArchitectureTests;

/// <summary>
/// The half of the dependency rule that governs <c>Core</c>: design 01 section 3.1 says it
/// "depends only on <c>Abstractions</c> plus the protocol engine" and "must not reference any
/// adapter, database provider, or cloud SDK". This is rule (b) of ADR-0024's enforcement clause,
/// which that ADR records as unenforced until the project it constrains exists.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three facts, because they fail on different evidence</b>, extending what
/// <c>DependencyRuleTests</c> records for <c>Abstractions</c>. The first reads the type graph and
/// catches a dependency some type actually uses. The second and third read the assembly reference
/// table.
/// </para>
/// <para>
/// <b>The elision limit was measured twice on 2026-08-08 and the second reading changed the
/// picture.</b> With the engine referenced and no code touching it, the reference table held only
/// <c>System.*</c> and <c>Microsoft.Extensions.*</c>: eight new packages had entered the restore graph
/// and the compiled surface had not moved. After seed S-010's wiring, the table holds six
/// <c>OpenIddict.*</c> entries. So the second fact now filters a real table rather than an empty one.
/// </para>
/// <para>
/// <b><c>Nami.Identity.Abstractions</c> is still absent from that table</b>, although the project
/// references it, because no type in <c>Core</c> touches an <c>Abstractions</c> type yet. So the
/// <b>third</b> fact still asserts an empty set that is empty for a reason other than the rule it
/// states, and only the second one changed state.
/// </para>
/// <para>
/// <b>The violation that matters is invisible to the first fact, and no edit to its list can fix
/// that.</b> Read at the 7.6.0 upstream commit, OpenIddict declares every <c>Add*</c> and
/// <c>Use*</c> extension and every builder in <c>Microsoft.Extensions.DependencyInjection</c>. So
/// <c>services.AddOpenIddict().AddCore(o => o.UseEntityFrameworkCore())</c>, which is exactly what
/// design 01 section 3.1 forbids here, names no type outside the allowed namespaces. Planting that
/// call on 2026-08-08 left the first fact green. <b>The second fact was green too</b>, because its
/// prefixes carried <c>Microsoft.EntityFrameworkCore</c> and <c>Quartz</c> and neither matches an
/// <c>OpenIddict.</c> assembly. Three prefixes were added and the plant then failed, which is the
/// only reason either fact covers the persistence boundary at all.
/// </para>
/// <para>
/// <b>Both reflection facts were also failed on purpose against a reference known to be present.</b>
/// Measured 2026-08-08: putting <c>Microsoft.Extensions.Options</c> into the forbidden prefix list
/// failed the second fact, and pointing the third fact's prefix filter at
/// <c>Microsoft.Extensions.</c> failed it too. That proves each mechanism reads the real table, and
/// it is a different claim from whether the lists have anything to catch.
/// </para>
/// <para>
/// <b>The allow-list is narrower than the word "framework" suggests, and that is the point.</b>
/// <c>Microsoft</c> alone would admit <c>Microsoft.EntityFrameworkCore</c>, which is precisely the
/// database provider the rule forbids, so the two framework roots this assembly genuinely uses are
/// named instead. The list widens when the engine reference lands, and widening it is a reviewable
/// edit rather than a silent one.
/// </para>
/// <para>
/// <b>Names here are PascalCase and not Given/When/Then</b>, matching the sibling class. ADR-0060
/// requires scenario names, and this repository has recorded that an architecture rule check is not
/// a scenario; the open item sits with ADR-0060's taxonomy confirmation rather than being settled
/// by renaming two methods.
/// </para>
/// </remarks>
public sealed class CoreDependencyRuleTests
{
    /// <summary>
    /// Everything <c>Core</c> may reach today: the base class library, the two ASP.NET Core
    /// framework roots it composes over, its own namespace, the abstractions it inverts onto, and
    /// the two engine namespaces the wiring names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Widened on 2026-08-08 by exactly two entries, and the narrowness is the point.</b> Seed
    /// S-010 added the engine wiring, and the class remarks had predicted that "the list widens when
    /// the engine reference lands, and widening it is a reviewable edit rather than a silent one".
    /// This is that edit. Only <c>OpenIddict.Abstractions</c> and <c>OpenIddict.Server</c> are added,
    /// because those are the only two <c>OpenIddict.*</c> namespaces the wiring names:
    /// <c>OpenIddictConstants</c> and <c>OpenIddictServerOptions</c>. A bare <c>OpenIddict</c> entry
    /// would have admitted <c>OpenIddict.EntityFrameworkCore</c> and <c>OpenIddict.Quartz</c>, which
    /// design 01 section 3.1 forbids here.
    /// </para>
    /// <para>
    /// <b>This fact is structurally blind to the realistic violation, which is why the second one
    /// carries the weight.</b> Read at the 7.6.0 upstream commit, OpenIddict declares
    /// <c>OpenIddictBuilder</c>, <c>OpenIddictServerBuilder</c>, <c>OpenIddictCoreBuilder</c>,
    /// <c>OpenIddictEntityFrameworkCoreBuilder</c> and every <c>Add*</c>/<c>Use*</c> extension in
    /// <c>Microsoft.Extensions.DependencyInjection</c>, not in its own namespaces. So
    /// <c>services.AddOpenIddict().AddCore(o => o.UseEntityFrameworkCore())</c> names no type outside
    /// this list, and no widening of it could catch that call. Measured 2026-08-08 by planting exactly
    /// that call: this fact passed. The assembly-reference fact below is the one that sees it, and
    /// only after its own list was corrected the same day.
    /// </para>
    /// </remarks>
    private const string AllowedNamespaces =
        @"^(System|Microsoft\.Extensions|Microsoft\.AspNetCore|Nami\.Identity\.Abstractions|Nami\.Identity\.Core|OpenIddict\.Abstractions|OpenIddict\.Server)($|\.).*";

    /// <summary>
    /// Assembly-name prefixes that would mean an adapter, a database provider, or a cloud SDK had
    /// been referenced. The list is stated rather than derived, because a derived one would have to
    /// be derived from something, and nothing here enumerates adapters yet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The three <c>OpenIddict.</c> entries were added on 2026-08-08 and they close a hole this
    /// list had from the day it was written.</b> Seed S-009 established that
    /// <c>OpenIddict.EntityFrameworkCore</c> is persistence, <c>OpenIddict.Quartz</c> is scheduling,
    /// and <c>OpenIddict.Core</c> carries the stores, so none of the three may be referenced here.
    /// None of them was caught: <c>"OpenIddict.EntityFrameworkCore".StartsWith("Microsoft.EntityFrameworkCore")</c>
    /// is false and so is <c>"OpenIddict.Quartz".StartsWith("Quartz")</c>. The two entries that look
    /// like they cover these packages cover different ones.
    /// </para>
    /// <para>
    /// <b>Measured rather than reasoned.</b> On 2026-08-08, planting
    /// <c>services.AddOpenIddict().AddCore(o => o.UseEntityFrameworkCore())</c> in <c>Core</c> put
    /// <c>OpenIddict.Core</c> and <c>OpenIddict.EntityFrameworkCore</c> into the reference table, and
    /// with the old list this fact passed. With these three entries it fails. The namespace fact above
    /// cannot help, because every type in that call sits in
    /// <c>Microsoft.Extensions.DependencyInjection</c>.
    /// </para>
    /// <para>
    /// <b>The prefixes are narrow so the five allowed engine assemblies stay allowed.</b> None of
    /// <c>OpenIddict.Abstractions</c>, <c>.Server</c>, <c>.Server.AspNetCore</c>, <c>.Validation</c>,
    /// <c>.Validation.AspNetCore</c> or <c>.Validation.ServerIntegration</c> starts with any of the
    /// three, and <c>OpenIddict.EntityFrameworkCore</c> also covers its <c>.Models</c> sibling.
    /// </para>
    /// </remarks>
    private static readonly string[] s_forbiddenAssemblyPrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "Azure.",
        "AWSSDK",
        "Amazon.",
        "Google.",
        "VaultSharp",
        "Finbuckle",
        "Quartz",
        "OpenIddict.Core",
        "OpenIddict.EntityFrameworkCore",
        "OpenIddict.Quartz",
    ];

    private static readonly System.Reflection.Assembly s_coreAssembly =
        typeof(NamiIdentityOptions).Assembly;

    private static readonly Architecture s_core =
        new ArchLoader().LoadAssemblies(s_coreAssembly).Build();

    /// <summary>
    /// No type in <c>Core</c> depends on a type outside the allowed namespaces.
    /// </summary>
    /// <remarks>
    /// The negative form is load-bearing. Measured on 2026-08-02 against a planted violation in
    /// <c>Abstractions</c>, the positive formulation passed over real breakage because the loader
    /// holds only the loaded assembly's types and cannot find a foreign type to reject.
    /// </remarks>
    [Fact]
    public void CoreTypesDependOnNothingOutsideTheFrameworkAndAbstractions() =>
        Types()
            .That().ResideInAssembly(s_coreAssembly)
            .Should().NotDependOnAnyTypesThat()
            .DoNotResideInNamespaceMatching(AllowedNamespaces)
            .Check(s_core);

    /// <summary>
    /// The compiled assembly references no adapter, database provider, or cloud SDK.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the half that sees a reference no type uses yet, which is the state
    /// <c>Nami.Identity.Core</c> is deliberately in: <c>Directory.Packages.props</c> pins the engine
    /// and nothing references it.
    /// </para>
    /// <para>
    /// It asserts against the compiled assembly and never against the project file. A test reading
    /// <c>PackageReference</c> items would fail on a correct project, because the public-API
    /// analyzer is referenced with <c>PrivateAssets="all"</c> and is legitimate.
    /// </para>
    /// </remarks>
    [Fact]
    public void CoreReferencesNoAdapterOrDatabaseProviderOrCloudSdk()
    {
        string[] offending = [.. s_coreAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(n => s_forbiddenAssemblyPrefixes.Any(
                p => n.StartsWith(p, StringComparison.Ordinal)))
            .Order()];

        Assert.Empty(offending);
    }

    /// <summary>
    /// The only sibling package <c>Core</c> references is <c>Abstractions</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stated apart from the prefix list above because it fails on a different mistake. The list
    /// catches a third-party adapter; this catches a Nami one, which is the arrow design 01 section
    /// 3.1 draws in the other direction, an adapter depending on <c>Core</c> rather than the
    /// reverse.
    /// </para>
    /// <para>
    /// <b>Today it sees nothing, and the class remarks say why.</b> The carve-out for
    /// <c>Abstractions</c> is written for the day a type in <c>Core</c> uses one, which is also the
    /// day this fact starts having anything to filter.
    /// </para>
    /// </remarks>
    [Fact]
    public void CoreReferencesNoSiblingNamiPackageExceptAbstractions()
    {
        string[] siblings = [.. s_coreAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(n => n.StartsWith("Nami.Identity.", StringComparison.Ordinal))
            .Where(n => n is not "Nami.Identity.Abstractions")
            .Order()];

        Assert.Empty(siblings);
    }
}
