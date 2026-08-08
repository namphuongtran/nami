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
/// <b>The elision limit is not theoretical here, it is this assembly's current state, and it was
/// measured rather than assumed.</b> Read on 2026-08-08 out of the built
/// <c>Nami.Identity.Core.dll</c>, the reference table holds only <c>System.*</c> and
/// <c>Microsoft.Extensions.*</c> entries. <b><c>Nami.Identity.Abstractions</c> is absent from it</b>,
/// although the project references that project, because no type in <c>Core</c> touches an
/// <c>Abstractions</c> type yet and an unused reference is elided from metadata. So the third fact
/// below currently asserts an empty set that is empty for a reason other than the rule it states.
/// </para>
/// <para>
/// <b>Both reflection facts were still failed on purpose, by pointing them at a reference that is
/// present.</b> Measured 2026-08-08: putting <c>Microsoft.Extensions.Options</c> into the forbidden
/// prefix list failed the second fact, and pointing the third fact's prefix filter at
/// <c>Microsoft.Extensions.</c> failed it too. That proves the mechanism reads the real table. It
/// does not prove the lists have anything to catch today, and the two claims are different.
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
    /// framework roots it composes over, its own namespace, and the abstractions it inverts onto.
    /// </summary>
    private const string AllowedNamespaces =
        @"^(System|Microsoft\.Extensions|Microsoft\.AspNetCore|Nami\.Identity\.Abstractions|Nami\.Identity\.Core)($|\.).*";

    /// <summary>
    /// Assembly-name prefixes that would mean an adapter, a database provider, or a cloud SDK had
    /// been referenced. The list is stated rather than derived, because a derived one would have to
    /// be derived from something, and nothing here enumerates adapters yet.
    /// </summary>
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
