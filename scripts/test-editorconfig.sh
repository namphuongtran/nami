#!/usr/bin/env bash
# Self-test for the C# ruleset in .editorconfig and for Directory.Build.props
# (ADR-0065). Run by CI and available locally.
#
# Why this exists. The ruleset landed on 2026-08-02, ahead of any C# in this
# repository, so nothing else in the tree exercises it. scripts/CLAUDE.md holds
# that a check never run against the bug it exists for is not known to work, and
# a ruleset with no code under it is exactly that: it reads as enforced, and
# whether it is stays unknown until the first project lands at M1. This test
# supplies the code, so the rules have to fire on every run rather than once on
# the author's machine.
#
# It is not a formality. Three of the ways this ruleset can be silently inert
# were found by measurement, not by reading, and each is asserted below:
#
#   1. `dotnet_naming_rule.<name>.severity = error` does not reach the build.
#      Only `dotnet_diagnostic.IDE1006.severity` does. Delete that one line and
#      every naming rule goes quiet while still reading as an error.
#   2. Severity of any kind does not fail a build without
#      EnforceCodeStyleInBuild, which is an MSBuild property in
#      Directory.Build.props rather than an editorconfig key.
#   3. The const and static carve-outs are what hold ADR-0065's *instance* field
#      rule to instance fields. Delete the const rule and a private constant is
#      required to carry the `s_` prefix; delete the static rule and a private
#      static is required to carry `_`. Either way the ruleset starts enforcing
#      a convention no decision states, and it does it quietly. The assertions
#      below check that each rule produces its OWN message, which is what makes
#      a lost carve-out visible rather than merely a different error.
#
# The probe project is written INSIDE the repository on purpose. That is the
# only way it inherits the real .editorconfig (root = true at this level) and
# the real Directory.Build.props (MSBuild walks up from the project directory),
# rather than a copy that could drift from what is being edited. scripts/CLAUDE.md
# records the sibling trap: a self-test that built its own isolated environment
# passed while its subject was deliberately broken, because the isolated copy was
# not the one under edit. Here the subject is the working tree by construction.
#
# Proven the same way, by breaking the subject on purpose and counting. Four
# breaks, and the pattern across them is worth more than any single number:
#
#   drop dotnet_diagnostic.IDE1006.severity   -> 5 failures, all on the BUILD path
#   drop EnforceCodeStyleInBuild              -> 8 failures, all on the BUILD path
#   drop the private-instance-field rule      -> 3 failures, spanning BOTH paths
#   reorder the naming rules                  -> 0 failures, correctly
#
# The first two leaving the format path untouched is the asymmetry Part 3 exists
# for: either break silences `dotnet build` completely while `dotnet format
# --verify-no-changes` keeps reporting all four naming violations. A gate built
# only on the format path would therefore stay green through both, and every
# contributor's local build would be silent. That is why neither part stands in
# for the other.
#
# The reorder break is how the "declaration order is load-bearing" claim this
# header used to carry was found to be false before it stayed committed. Its
# green is correct: there is nothing there to catch.
#
# A fifth break was run and is not listed, because it did not run: the edit that
# was supposed to delete a rule threw before writing, and the unmodified file
# then passed. A break experiment that fails to break reports the same green as
# a healthy subject, so check that the subject actually changed before believing
# a green.
#
# Portable to macOS bash 3.2 and the Ubuntu runner, like the scripts beside it:
# no mapfile, no associative arrays, no GNU-only flags. Pure ASCII.
set -uo pipefail

cd "$(git rev-parse --show-toplevel)"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "editorconfig self-test SKIPPED: no dotnet SDK on PATH."
  echo "  This is a skip, not a pass. The ruleset is unverified in this run."
  exit 0
fi

probe=".editorconfig-probe"
cleanup() { rm -rf "$probe"; }
trap cleanup EXIT
rm -rf "$probe"
mkdir -p "$probe" || { echo "editorconfig self-test FAILED: cannot create $probe"; exit 1; }

fails=0
fail() { echo "  FAIL: $1"; fails=$((fails + 1)); }

cat > "$probe/Probe.csproj" <<'PROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
PROJ

# --- Part 1: the compliant fixture must build clean ---
#
# Every construct here is one the ruleset has an opinion about, written the way
# the ruleset wants it, so a rule that fires here is a false positive rather
# than a catch. The last method is the load-bearing one: it is deliberately NOT
# suffixed, because the Async rule matches the `async` modifier and a
# Task-returning method without it is outside coverage. If that line ever starts
# failing, coverage changed and the comment in .editorconfig is stale.
cat > "$probe/Compliant.cs" <<'GOOD'
using System.Threading.Tasks;

namespace NamiEditorConfigProbe;

public interface IProbe
{
    int Value { get; }
}

public sealed class Compliant : IProbe
{
    private const int MaxRetries = 3;
    private static int s_counter = 1;
    private static readonly object s_gate = new();
    private readonly int _value;

    public Compliant(int value) => _value = value;

    public int Value => _value + MaxRetries + s_counter + s_gate.GetHashCode();

    public async Task<int> ReadAsync()
    {
        await Task.Delay(1).ConfigureAwait(false);
        return Value;
    }

    public Task<int> Read() => Task.FromResult(Value);
}
GOOD

good_out=$(dotnet build "$probe/Probe.csproj" -v q --nologo 2>&1)
good_rc=$?
good_ide=$(printf '%s\n' "$good_out" | grep -c "IDE[0-9]")
if [ "$good_rc" -ne 0 ]; then
  fail "compliant fixture did not build (exit $good_rc). The ruleset rejects code it should accept."
  printf '%s\n' "$good_out" | sed 's/^/    /'
fi
if [ "$good_ide" -ne 0 ]; then
  fail "compliant fixture raised $good_ide IDE diagnostic line(s); expected 0."
  printf '%s\n' "$good_out" | grep "IDE[0-9]" | sed 's/^/    /'
fi

dotnet format "$probe/Probe.csproj" --verify-no-changes >/dev/null 2>&1
good_fmt_rc=$?
if [ "$good_fmt_rc" -ne 0 ]; then
  fail "compliant fixture is not format-clean (dotnet format exit $good_fmt_rc)."
  dotnet format "$probe/Probe.csproj" --verify-no-changes 2>&1 | sed 's/^/    /'
fi

# --- Part 2: the violating fixture must fail the build ---
#
# One violation per rule the ruleset sets to error, plus whitespace for IDE0055.
rm -f "$probe/Compliant.cs"
cat > "$probe/Violating.cs" <<'BAD'
using System.Threading.Tasks;

namespace NamiEditorConfigProbe;

public sealed class Violating
{
    private const int maxRetries = 3;
    private static int counter = 1;
    private int value = 2;

    public int Sum() => maxRetries + counter + value;

    public async Task PollForever()
    {
        await Task.Delay(1).ConfigureAwait(false);
    }

    public int  Spaced( )
    {
            return 1;
    }
}
BAD

bad_out=$(dotnet build "$probe/Probe.csproj" -v q --nologo 2>&1)
bad_rc=$?

if [ "$bad_rc" -eq 0 ]; then
  fail "violating fixture BUILT. Every assertion below is meaningless; the ruleset is inert."
fi

# MSBuild prints each diagnostic twice, once inline and once in the summary, so
# every count here is taken over deduplicated file:line:id sites.
sites=$(printf '%s\n' "$bad_out" | grep -oE 'Violating\.cs\([0-9]+,[0-9]+\): error IDE[0-9]+' | sort -u)

expect() {
  if printf '%s\n' "$bad_out" | grep -qF "$2"; then
    :
  else
    fail "$1 not caught (expected a diagnostic containing: $2)"
  fi
}
expect "private const field PascalCase"      "upper case characters: maxRetries"
expect "private static field s_camelCase"    "Missing prefix: 's_'"
expect "private instance field _camelCase"   "Missing prefix: '_'"
expect "async method Async suffix"           "Missing suffix: 'Async'"
expect "formatting"                          "IDE0055"

# Counts rather than per-line greps. scripts/CLAUDE.md records why: a negative
# assertion written as a per-line grep fails open, and every hard-coded line
# number in the first version of the sibling self-test was off by one without a
# single assertion noticing. A count cannot pass by matching nothing.
n1006=$(printf '%s\n' "$sites" | grep -c "IDE1006")
n0055=$(printf '%s\n' "$sites" | grep -c "IDE0055")
if [ "$n1006" -ne 4 ]; then
  fail "expected exactly 4 distinct IDE1006 sites, found $n1006."
  printf '%s\n' "$sites" | sed 's/^/    /'
fi
if [ "$n0055" -lt 1 ]; then
  fail "expected at least 1 IDE0055 site, found $n0055."
fi

# --- Part 3: the same violations must fail the OTHER enforcement path ---
#
# ADR-0065 names `dotnet format --verify-no-changes` as what CI enforces, and
# Part 2 does not exercise it. The two paths are not the same gate wearing two
# names, which is why both are asserted rather than one standing in for the
# other. Three measured differences:
#
#   - `dotnet format` does NOT need EnforceCodeStyleInBuild. It caught a naming
#     violation on a project without the property, on which `dotnet build`
#     reported nothing at all. So a green format gate is not evidence that the
#     property is still set, and deleting it as redundant would leave every
#     local `dotnet build` silent while CI stayed green.
#   - It reports whitespace as `WHITESPACE`, not as `IDE0055`.
#   - It exits 2 rather than 1.
#
# The direction that matters for a contributor is the reverse of the one CI
# cares about: this is the path that also FIXES, so a violation caught here is
# `dotnet format` away from being resolved.
fmt_out=$(dotnet format "$probe/Probe.csproj" --verify-no-changes 2>&1)
fmt_rc=$?

if [ "$fmt_rc" -eq 0 ]; then
  fail "violating fixture is format-clean. The path ADR-0065 names as the CI gate is inert."
fi

fmt_sites=$(printf '%s\n' "$fmt_out" | grep -oE 'Violating\.cs\([0-9]+,[0-9]+\): error [A-Z0-9]+' | sort -u)
fmt_n1006=$(printf '%s\n' "$fmt_sites" | grep -c "IDE1006")
fmt_nws=$(printf '%s\n' "$fmt_sites" | grep -c "WHITESPACE")
if [ "$fmt_n1006" -ne 4 ]; then
  fail "format path: expected exactly 4 distinct IDE1006 sites, found $fmt_n1006."
  printf '%s\n' "$fmt_sites" | sed 's/^/    /'
fi
if [ "$fmt_nws" -lt 1 ]; then
  fail "format path: expected at least 1 WHITESPACE site, found $fmt_nws."
fi

# --- Report ---
if [ "$fails" -gt 0 ]; then
  echo "editorconfig self-test FAILED: $fails assertion(s)."
  exit 1
fi
echo "editorconfig self-test OK: compliant fixture is build-clean and format-clean; violating fixture fails on 4 naming rules and formatting, on both the build and the dotnet-format path."
exit 0
