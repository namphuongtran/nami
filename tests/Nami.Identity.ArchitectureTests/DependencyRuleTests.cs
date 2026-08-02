using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Nami.Identity.Abstractions;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Nami.Identity.ArchitectureTests;

/// <summary>
/// The dependency rule of design 01 section 3.1 line 97, "<c>Abstractions</c> depends on
/// nothing", asserted from outside the project that has to obey it (ADR-0024 enforcement,
/// ADR-0060's architecture-test row).
/// </summary>
/// <remarks>
/// <para>
/// Two facts, deliberately, because neither alone covers the rule and they fail on
/// different evidence. The first reads the type graph and catches a dependency that is
/// <em>used</em>. The second reads the assembly reference table and catches a reference the
/// compiler kept, used or not by any single type. Measured on 2026-08-02 against a planted
/// <c>Newtonsoft.Json</c> reference: with a type actually calling into it, both fail; with
/// the package referenced and no code touching it, <b>both pass</b>, because an unused
/// reference is elided from metadata. That third case is the stated limit of this file, and
/// the thing that closes it is a packed-surface check, which needs a pack and does not exist
/// yet.
/// </para>
/// <para>
/// Neither fact may be rewritten to read the <c>.csproj</c>. The analyzer reference in
/// <c>Nami.Identity.Abstractions.csproj</c> is legitimate and carries
/// <c>PrivateAssets="all"</c>, so it reaches neither the type graph nor the reference table,
/// and both facts are green with it in place. A test asserting on <c>PackageReference</c>
/// items would fail on that correct project and would be "fixed" by deleting the analyzer.
/// </para>
/// </remarks>
public sealed class DependencyRuleTests
{
    /// <summary>
    /// Everything <c>Abstractions</c> is allowed to reach: the base class library, and its
    /// own namespace tree. Sibling Nami packages are deliberately excluded, since depending
    /// on one would break the rule exactly as a third-party package would.
    /// </summary>
    private const string AllowedNamespaces = @"^(System|Nami\.Identity\.Abstractions)($|\.).*";

    private static readonly System.Reflection.Assembly s_abstractionsAssembly =
        typeof(ScopeDefinition).Assembly;

    private static readonly Architecture s_abstractions =
        new ArchLoader().LoadAssemblies(s_abstractionsAssembly).Build();

    /// <summary>
    /// No type in <c>Abstractions</c> depends on a type outside the allowed namespaces.
    /// </summary>
    /// <remarks>
    /// <b>The negative form is load-bearing and the positive one is inert.</b> Measured on
    /// 2026-08-02 against the planted reference, <c>OnlyDependOnTypesThat()
    /// .ResideInNamespaceMatching(...)</c> passed while this rule failed, on the same
    /// architecture and the same violation. The cause is that the loader was given one
    /// assembly, so <c>Architecture.Types</c> holds only the types in it, and a formulation
    /// that resolves its allowed set out of that collection can never see a foreign type to
    /// reject. The dependency itself is recorded either way: dumping
    /// <c>ProbeSerializer.Dependencies</c> showed <c>Newtonsoft.Json.JsonConvert</c> present.
    /// So do not "simplify" this into the sentence it reads as in English; the two are not
    /// the same rule, and the readable one is the one that passes over a real violation.
    /// </remarks>
    [Fact]
    public void AbstractionsTypesDependOnNothingOutsideTheFramework() =>
        Types()
            .That().ResideInAssembly(s_abstractionsAssembly)
            .Should().NotDependOnAnyTypesThat()
            .DoNotResideInNamespaceMatching(AllowedNamespaces)
            .Check(s_abstractions);

    /// <summary>
    /// The compiled assembly references nothing but the base class library.
    /// </summary>
    /// <remarks>
    /// This is the compile-time-reference half that <c>Nami.Identity.Abstractions.csproj</c>
    /// asks the architecture test to assert. It is plain reflection rather than ArchUnitNET
    /// because the question is about the assembly, not about its type graph, and the two are
    /// answered by different tables.
    /// </remarks>
    [Fact]
    public void AbstractionsReferencesNoAssemblyOutsideTheFramework()
    {
        string[] referenced = [.. s_abstractionsAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(n => !n.StartsWith("System.", StringComparison.Ordinal)
                        && n is not ("System" or "netstandard" or "mscorlib"))
            .Order()];

        Assert.Empty(referenced);
    }
}
