#!/usr/bin/env bash
# Self-test for the warning-escalation gate. Three decisions meet in one
# PropertyGroup and each can be silenced alone, so the assertions are grouped
# by the failure rather than by the decision:
#
#   TreatWarningsAsErrors   ADR-0093 parameter A     -> Part 2
#   AnalysisLevelSecurity   ADR-0092 section 1       -> Part 3
#   AnalysisMode            ADR-0094                 -> Part 4
#   WarningsNotAsErrors     ADR-0093 parameter C     -> Part 5
#
# Parts 2 to 5 all assert against a throwaway probe, which is what lets them be
# behavioural. Part 6 asserts the same four properties as EVALUATED values on
# the projects the repository actually ships, because a per-project override is
# invisible to the probe by construction. Its own comment carries the
# measurement and the reason the probe cannot stand in for it.
#
# Until this script existed nothing in the tree would have noticed any of them
# being deleted: measured 2026-08-03 on SDK 10.0.301, the solution builds
# 0 Warning(s) with the properties and 0 Warning(s) without them, so the
# ordinary build is green either way and says nothing about whether the gate
# is armed.
#
# The probe project is written INSIDE the repository on purpose, for the reason
# the two sibling self-tests state at more length: MSBuild walks UP from a
# project directory, so a probe here inherits the real Directory.Build.props
# rather than a copy that could drift from what is being edited.
#
# Each violating fixture was measured to raise its diagnostic ALONE, which is
# the lesson test-public-api-gate.sh Part 4 paid for: an exit code is a weak
# assertion when several rules watch one fixture. Part 3 needed a second
# attempt for that reason and its comment records what the first one missed.
#
# TWO WAYS TO GET A FALSE GREEN WHEN CHECKING THIS GATE BY HAND. Both were
# measured on 2026-08-03 on SDK 10.0.301, and both report exit 0 with no
# diagnostic at all, which is indistinguishable from a gate that is genuinely
# off. Neither can reach this script, and the reasons are worth stating because
# they are why it builds the way it does.
#
#   1. Two -p: flags inside ONE shell argument. zsh does not word-split an
#      unquoted expansion, so `dotnet build $flags` with both in one variable
#      arrives as a single argument. MSBuild then reads
#      AnalysisMode = "Recommended -p:AnalysisLevelSecurity=latest-all" and
#      AnalysisLevelSecurity = "", verified with -getProperty. A garbage
#      AnalysisMode names a globalconfig that does not exist, so BOTH axes
#      configure nothing and every analyzer diagnostic disappears, including
#      ones either property reports on its own. Exit 0. Pass each flag as its
#      own argument. Delivered correctly, the -p: route and a project file
#      agree on every row that was measured.
#   2. An incremental build after a property-only change. Nothing in the
#      compilation inputs moved, so MSBuild skips the compiler and reports the
#      previous run's silence. Add -t:Rebuild when comparing property values by
#      hand. This script does not need it: it rm -rf's the probe on entry, so
#      obj/ is fresh, and it rewrites Probe.cs before every part.
#
# A third shape is NOT a trap and is recorded so nobody "fixes" it, measured
# the same day and on the same SDK as the two above: MSBuild splits a -p:
# argument on ';' into name=value pairs, so
# `-p:AnalysisMode=Recommended;AnalysisLevelSecurity=latest-all` is two valid
# pairs and works. `-p:WarningsNotAsErrors=NU1901;NU1902` fails MSB1006 by the
# same rule, because the bare NU1902 has no '='. That is why a multi-valued
# property has to be set in a project file rather than on the command line.
#
# What Part 5 does NOT cover, stated here because its green is easy to
# over-read: it asserts the VALUE of WarningsNotAsErrors, not that NuGet
# honours it at restore. That was measured once, on 2026-08-03 on SDK 10.0.301,
# against System.Text.Encodings.Web 4.5.0, where NU1904 went from a warning at
# exit 0 to `error NU1904: Warning As Error` at exit 1. Directory.Build.props
# carries that measurement in full, including the row showing the carve-out is
# narrow enough that NU1510 still fails the same restore; it is not repeated
# here. Asserting it on every run would need a network restore and a live
# advisory that can change under the test, so the property is what this file
# checks.
#
# Portable to macOS bash 3.2 and the Ubuntu runner, like the scripts beside it:
# no mapfile, no associative arrays, no GNU-only flags. Pure ASCII.
set -uo pipefail

cd "$(git rev-parse --show-toplevel)"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "warnings-as-errors self-test SKIPPED: no dotnet SDK on PATH."
  echo "  This is a skip, not a pass. The gate is unverified in this run."
  exit 0
fi

probe=".warnaserror-probe"
cleanup() { rm -rf "$probe"; }
trap cleanup EXIT
rm -rf "$probe"
mkdir -p "$probe" || { echo "warnings-as-errors self-test FAILED: cannot create $probe"; exit 1; }

fails=0
fail() { echo "  FAIL: $1"; fails=$((fails + 1)); }

# TargetFramework is written literally rather than through the ADR-0030 knob,
# matching both sibling self-tests. The knob is a different gate's subject, and
# reading it here would let a knob edit produce a red in a script about
# warnings. All three scripts move together when the framework moves.
cat > "$probe/Probe.csproj" <<'PROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
PROJ

build() { dotnet build "$probe/Probe.csproj" -v q --nologo 2>&1; }

# Counts are taken over deduplicated site:id pairs, because MSBuild prints each
# diagnostic twice, once inline and once in the summary. scripts/CLAUDE.md
# records why these are counts and not per-line greps: a negative assertion
# written as a grep fails open, and every hard-coded line number in the first
# version of a sibling self-test was off by one with nothing noticing.
count_sites() {
  # $1: build output, $2: diagnostic id
  printf '%s\n' "$1" | grep -oE "\(?[0-9]*,?[0-9]*\)?: error $2" | sort -u | grep -c "error $2"
}

# The isolation check. A fixture that raises a second diagnostic makes the
# exit-code assertion pass on the wrong rule, which is how a part goes quiet
# while reading as armed.
other_ids() {
  # $1: build output, $2: the id this fixture is meant to raise alone
  printf '%s\n' "$1" | grep -oE "error [A-Z]+[0-9]+" | sed 's/^error //' | sort -u | grep -v "^$2\$" | tr '\n' ' '
}

get_prop() {
  # $1: property name, evaluated through the real import chain
  dotnet msbuild "$probe/Probe.csproj" -getProperty:"$1" 2>/dev/null
}

# --- Part 1: the compliant fixture must build clean ---
#
# The control. Without it a repository whose build is broken for an unrelated
# reason would report every break below as caught, which is the shape of a
# self-test that proves nothing.
cat > "$probe/Probe.cs" <<'SRC'
namespace NamiWarningProbe;

public sealed class Probe
{
    public int Value { get; set; }
}
SRC

good_out=$(build)
good_rc=$?
good_n=$(printf '%s\n' "$good_out" | grep -cE "(warning|error) [A-Z]+[0-9]+")
if [ "$good_rc" -ne 0 ]; then
  fail "compliant fixture did not build (exit $good_rc). Every part below asserts a fixture in isolation and cannot do that from a broken baseline."
  printf '%s\n' "$good_out" | sed 's/^/    /'
fi
if [ "$good_n" -ne 0 ]; then
  fail "compliant fixture raised $good_n diagnostic line(s); expected 0. A noisy control makes the isolation assertions below meaningless."
  printf '%s\n' "$good_out" | grep -E "(warning|error) [A-Z]+[0-9]+" | sed 's/^/    /'
fi

# --- Part 2: a plain compiler warning must fail the build (ADR-0093 A) ---
#
# The property itself. CS0219 on an unused local: measured 2026-08-03 on SDK
# 10.0.301 as a warning at exit 0 without TreatWarningsAsErrors and an error
# at exit 1 with it. Delete that property and this part is what goes red.
cat > "$probe/Probe.cs" <<'SRC'
namespace NamiWarningProbe;

public static class Unused
{
    public static int Value()
    {
        int unusedLocal = 1;
        return 2;
    }
}
SRC

cs_out=$(build)
cs_rc=$?
cs_n=$(count_sites "$cs_out" "CS0219")
cs_other=$(other_ids "$cs_out" "CS0219")
if [ "$cs_rc" -eq 0 ]; then
  fail "a plain compiler warning BUILT GREEN. TreatWarningsAsErrors is not set; check Directory.Build.props (ADR-0093 parameter A)."
fi
if [ "$cs_n" -lt 1 ]; then
  fail "expected at least 1 CS0219 error site, found $cs_n."
  printf '%s\n' "$cs_out" | grep -E "(warning|error) [A-Z]+[0-9]+" | sed 's/^/    /'
fi
if [ -n "$cs_other" ]; then
  fail "Part 2's fixture also raised: $cs_other. It cannot isolate CS0219 and the exit-code assertion above may be passing on the wrong diagnostic."
fi

# --- Part 3: a security rule must fail the build (ADR-0092 section 1) ---
#
# The whole difficulty here is finding a diagnostic that ONLY the security axis
# can produce. AnalysisMode=Recommended is set three lines away in
# Directory.Build.props because ADR-0094 requires it, and the two tiers
# overlap, so most security-looking rules fire either way and prove nothing
# about this property.
#
# CA5392 is outside the overlap. Counted from the shipped globalconfigs in
# Sdks/Microsoft.NET.Sdk/analyzers/build/config on 2026-08-03, SDK 10.0.301:
# analysislevel_10_recommended.globalconfig enables 145 CA rules, and
# analysislevelsecurity_10_all.globalconfig enables 94, of which 70 are absent
# from the Recommended set. CA5392 is one of those 70; it appears at line 252
# of the security file and nowhere in the Recommended or default ones.
#
# Measured against this exact fixture on 2026-08-03 on SDK 10.0.301,
# properties set in a project file rather than on the command line, which the
# header explains:
#
#   AnalysisMode=Recommended alone                     no diagnostic, exit 0
#   AnalysisLevelSecurity=latest-all alone             CA5392
#   Recommended + latest-all                           CA5392 alone
#   Recommended + all                                  no diagnostic, exit 0
#   Recommended + latest-all + TreatWarningsAsErrors   error CA5392, exit 1
#   Recommended + all        + TreatWarningsAsErrors   no diagnostic, exit 0
#
# The last two rows are what this part asserts, and the difference between them
# is the whole point: the bare `all` is INERT and this part goes red on it. It
# is inert because it parses as a LEVEL rather than as a level-mode pair, so the
# SDK looks for a globalconfig that was never shipped and the Exists() guard on
# the include silently skips it. Directory.Build.props carries that trace,
# property by property, and it is not repeated here.
#
# CA5351 WAS the fixture here and was rejected on measurement. MD5 is a
# security-sounding rule that lives in BOTH tiers:
# analysislevel_10_recommended.globalconfig sets
# `dotnet_diagnostic.CA5351.severity = warning` at line 396, read 2026-08-03 in
# SDK 10.0.301 like every other line number here. So the part passed with
# AnalysisLevelSecurity set to the inert `all`, and passed again with the
# property deleted outright, while reading as armed. It was caught by the break
# experiment and not by the part itself, which is the argument for doing them.
#
# BOTH the class and the method are `internal` on purpose. The public form also
# raises CA1401, "P/Invoke method should not be visible", which is an
# Interoperability rule inside Recommended (line 75 of
# analysislevel_10_recommended.globalconfig, and absent from the security
# file). Measured 2026-08-03 on SDK 10.0.301: the public form raises CA1401
# under Recommended alone, so this part would pass on CA1401's back under the
# inert `all` and the regression would be invisible again. The other_ids
# assertion below is what keeps that honest.
cat > "$probe/Probe.cs" <<'SRC'
using System.Runtime.InteropServices;

namespace NamiWarningProbe;

internal static class Native
{
    [DllImport("kernel32.dll")]
    internal static extern int GetCurrentProcessId();
}
SRC

sec_out=$(build)
sec_rc=$?
sec_n=$(count_sites "$sec_out" "CA5392")
sec_other=$(other_ids "$sec_out" "CA5392")
if [ "$sec_rc" -eq 0 ]; then
  fail "an unsafe DllImport BUILT GREEN. AnalysisLevelSecurity is unset or carries the inert bare 'all' rather than 'latest-all'; check Directory.Build.props (ADR-0092 section 1)."
fi
if [ "$sec_n" -lt 1 ]; then
  fail "expected at least 1 CA5392 error site, found $sec_n."
  printf '%s\n' "$sec_out" | grep -E "(warning|error) [A-Z]+[0-9]+" | sed 's/^/    /'
fi
if [ -n "$sec_other" ]; then
  fail "Part 3's fixture also raised: $sec_other. It cannot isolate CA5392 and may be passing on a Recommended-tier rule rather than on the security axis."
fi

# --- Part 4: a Recommended-tier rule must fail the build (ADR-0094) ---
#
# CA1050 is silent at the SDK default and a warning at Recommended, measured
# 2026-08-03 on SDK 10.0.301. A rule that already fires at the default would
# give a green here indistinguishable from AnalysisMode being absent, which is
# why this fixture is CA1050 and not something noisier.
cat > "$probe/Probe.cs" <<'SRC'
public sealed class NoNamespace
{
    public int Value { get; set; }
}
SRC

am_out=$(build)
am_rc=$?
am_n=$(count_sites "$am_out" "CA1050")
am_other=$(other_ids "$am_out" "CA1050")
if [ "$am_rc" -eq 0 ]; then
  fail "a Recommended-tier quality violation BUILT GREEN. AnalysisMode is not set to Recommended; check Directory.Build.props (ADR-0094)."
fi
if [ "$am_n" -lt 1 ]; then
  fail "expected at least 1 CA1050 error site, found $am_n."
  printf '%s\n' "$am_out" | grep -E "(warning|error) [A-Z]+[0-9]+" | sed 's/^/    /'
fi
if [ -n "$am_other" ]; then
  fail "Part 4's fixture also raised: $am_other. It cannot isolate CA1050."
fi

# --- Part 5: the carve-out must be present and deliberate (ADR-0093 C) ---
#
# Asserted on the EVALUATED property rather than behaviourally, and the header
# says why. The break this catches is the realistic one: the line deleted, or
# the $(WarningsNotAsErrors) append replaced by a bare assignment that drops
# whatever another file contributed.
cat > "$probe/Probe.cs" <<'SRC'
namespace NamiWarningProbe;

public sealed class Probe
{
    public int Value { get; set; }
}
SRC

twae=$(get_prop TreatWarningsAsErrors)
if [ "$twae" != "true" ]; then
  fail "TreatWarningsAsErrors evaluates to '$twae', expected 'true' (ADR-0093 parameter A)."
fi

wnae=$(get_prop WarningsNotAsErrors)
for code in NU1901 NU1902 NU1903 NU1904; do
  case ";$wnae;" in
    *";$code;"*) ;;
    *) fail "WarningsNotAsErrors does not carry $code (ADR-0093 parameter C). Evaluated value: '$wnae'." ;;
  esac
done

# --- Part 6: the REAL projects, not the probe (all four properties) ---
#
# THE BREAK THIS CATCHES AND NOTHING ELSE DOES. Every part above asserts
# against .warnaserror-probe, a project this script writes and deletes, so
# until this part existed nothing in the tree said anything about the projects
# the repository ships. A <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
# in a real .csproj, or a src/Directory.Build.props that does not <Import> the
# root one, disarms the gate for the only code there is. That is the exact
# shape the root CLAUDE.md calls the worst outcome available here, and it is
# what this script exists to close.
#
# WHY THE PROBE CANNOT CATCH IT, and it is not an oversight in how the probe is
# written. MSBuild walks UP from a project directory. The probe sits at the
# repository root, so it inherits the root Directory.Build.props and there is
# no edit under src/ or tests/ that can reach it. The blind spot is a property
# of where the probe lives, and the only way to see a per-project override is
# to evaluate the project it overrides.
#
# Proven rather than argued, 2026-08-03 on SDK 10.0.301, then reverted: with
# <TreatWarningsAsErrors>false</TreatWarningsAsErrors> planted in
# src/Nami.Identity.Abstractions/Nami.Identity.Abstractions.csproj, this part
# reported the override and NOTHING else moved. Parts 1 to 5 stayed green, and
# so did every other gate measured the same day:
#
#   dotnet build Nami.Identity.slnx -t:Rebuild   0 Warning(s) 0 Error(s), exit 0
#   dotnet test Nami.Identity.slnx                                       exit 0
#   dotnet format Nami.Identity.slnx --verify-no-changes                 exit 0
#   bash scripts/test-warnings-as-errors.sh      1 FAIL, this part,      exit 1
#
# -getProperty is the same mechanism Part 5 uses on the probe, pointed at a
# different project: it reads the real import chain. It evaluates rather than
# builds, so it costs no compile time.
#
# The project list is DISCOVERED rather than written down, so a project added
# later is covered without anyone remembering this file. A discovery that finds
# nothing would be the quiet failure, so the count is asserted first. Two
# projects exist on 2026-08-03:
# src/Nami.Identity.Abstractions/Nami.Identity.Abstractions.csproj and
# tests/Nami.Identity.ArchitectureTests/Nami.Identity.ArchitectureTests.csproj.
real_projects=$(find src tests -type f -name '*.csproj' 2>/dev/null | sort)
real_n=$(printf '%s\n' "$real_projects" | grep -c '\.csproj$')
if [ "$real_n" -lt 2 ]; then
  fail "found $real_n project file(s) under src/ and tests/, expected at least 2. Every assertion below is per-project, so an empty discovery would report a pass having checked nothing."
fi

get_proj_prop() {
  # $1: project path, $2: property name, through that project's import chain.
  #
  # A FAILED EVALUATION AND AN UNSET PROPERTY BOTH PRINT NOTHING, so this
  # distinguishes them rather than letting a caller read one as the other. Every
  # assertion in Part 6 is positive and fails loudly on an empty string, but Part
  # 7's src/ ban is negative and an empty string SATISFIES it. Measured
  # 2026-08-08 against a project with malformed XML: exit 1, empty stdout.
  # Returning a sentinel makes that path fail closed everywhere.
  #
  # The value is returned RAW. Part 6 compares it against exact spellings such
  # as `Recommended`, so normalising here would quietly change what those
  # assertions accept. Part 7 normalises at its own call site instead.
  gpp_out=$(dotnet msbuild "$1" -getProperty:"$2" 2>/dev/null) || {
    printf '%s' "__EVAL_FAILED__"
    return
  }
  printf '%s' "$gpp_out"
}

for proj in $real_projects; do
  p_twae=$(get_proj_prop "$proj" TreatWarningsAsErrors)
  if [ "$p_twae" != "true" ]; then
    fail "TreatWarningsAsErrors evaluates to '$p_twae' in $proj, expected 'true' (ADR-0093 parameter A). The probe cannot see this: a per-project override or a Directory.Build.props shadowing the root one leaves every part above green."
  fi

  p_wnae=$(get_proj_prop "$proj" WarningsNotAsErrors)
  for code in NU1901 NU1902 NU1903 NU1904; do
    case ";$p_wnae;" in
      *";$code;"*) ;;
      *) fail "WarningsNotAsErrors does not carry $code in $proj (ADR-0093 parameter C). Evaluated value: '$p_wnae'." ;;
    esac
  done

  # CONTAINMENT IS NOT THE WHOLE ASSERTION, and the missing half was found by
  # review on 2026-08-08 rather than by this script. The loop above says the four
  # carve-out codes are present and says nothing about a fifth. Measured that
  # day: adding CA1707 to this property in the src/ project demoted it from a
  # build error to a warning, `dotnet build` exited 0 on `1 Warning(s)`, and
  # every part of this script stayed green. It is a second door to the same room
  # as NoWarn, for any diagnostic rather than only CA1707, and CI exits 0 on a
  # warning.
  #
  # ADR-0093 parameter C is the whole permitted set, so anything else is a
  # decision that has to be written down. The evaluated value carries a leading
  # empty field from the unset parent, which is why the filter keeps the empty
  # string.
  #
  # THE ORDER OF THE TWO `tr` CALLS IS LOAD-BEARING. Whitespace is stripped
  # BEFORE the split, because `tr -d '[:space:]'` after `tr ';' '\n'` deletes the
  # newlines the split just created and collapses the list into one token that
  # matches nothing. Written that way round first, and caught by running the
  # clean case: it reported all four permitted codes as extras. It fails closed,
  # which is why it was loud rather than silent.
  p_extra=$(printf '%s' "$p_wnae" | tr -d '[:space:]' | tr ';' '\n' \
    | grep -vE '^(NU1901|NU1902|NU1903|NU1904)?$' | tr '\n' ' ')
  if [ -n "$p_extra" ]; then
    fail "WarningsNotAsErrors in $proj carries codes beyond the ADR-0093 parameter C carve-out: $p_extra. Each one demotes a build error to a warning, and CI's \`dotnet build\` exits 0 on warnings."
  fi

  p_als=$(get_proj_prop "$proj" AnalysisLevelSecurity)
  if [ "$p_als" != "latest-all" ]; then
    fail "AnalysisLevelSecurity evaluates to '$p_als' in $proj, expected 'latest-all' (ADR-0092 section 1). The bare 'all' is inert and reads as armed."
  fi

  p_am=$(get_proj_prop "$proj" AnalysisMode)
  if [ "$p_am" != "Recommended" ]; then
    fail "AnalysisMode evaluates to '$p_am' in $proj, expected 'Recommended' (ADR-0094)."
  fi

  # A test project that omits TestingPlatformDotnetTestSupport is not run by
  # `dotnet test` and is not reported as skipped either: it vanishes. Measured
  # 2026-08-08 by deleting the line from Nami.Identity.UnitTests: the run printed
  # only the architecture suite's "Passed: 2" and exited 0, with every fact in
  # that project gone and no line naming them. Three documents warned about this property and
  # nothing read it, which is this repository's own definition of a control that
  # reads as enforced while enforcing nothing.
  #
  # A grep would not do. Measured the same day, `grep -c
  # TestingPlatformDotnetTestSupport` on that csproj still returned a match after
  # the property was deleted, because the word survives five times in the
  # comment above it. This reads the evaluated property.
  case "$proj" in
    tests/*)
      p_tp=$(get_proj_prop "$proj" TestingPlatformDotnetTestSupport)
      if [ "$p_tp" != "true" ]; then
        fail "TestingPlatformDotnetTestSupport evaluates to '$p_tp' in $proj, expected 'true'. Without it \`dotnet test\` runs this project's tests nowhere and reports nothing, so the suite disappears at exit 0."
      fi
      ;;
  esac
done

# --- Part 7: the CA1707 exemption stays where ADR-0093 parameter D put it ---
#
# Added 2026-08-08, with the exemption itself. ADR-0093 parameter B forbids a
# carve-out by directory and parameter D requires "a per-project <NoWarn> with a
# comment". Both are prose, and nothing read them. The failure mode this part
# exists for is a widening: the same suppression moved into
# Directory.Build.props, or added to a src/ project, disables a naming rule
# repository-wide while every part above stays green.
#
# IT HAS TWO HALVES BECAUSE A PROPERTY READ CANNOT SEE THE FORBIDDEN ARTIFACT.
# The first version of this part read the evaluated NoWarn and nothing else, and
# review broke it four ways on 2026-08-08, each time leaving the script green
# while CA1707 was in fact off for src/: an `.editorconfig` severity line, a
# `WarningsNotAsErrors` entry, a `NoWarn` written with one space after the
# semicolon, and a `tests/Directory.Build.props`. Three of the four never touch
# NoWarn at all. So 7a asks the compiler instead, and 7b keeps the property read
# for the one case a probe cannot reach.

# --- 7a: behavioural. CA1707 still bites under src/. ---
#
# The root probe cannot answer this. It sits at the repository root, so MSBuild
# and EditorConfig both walk up past anything scoped to src/, which is the reason
# Part 6 exists at all. This probe sits INSIDE src/, so it inherits a
# src/Directory.Build.props, matches a `[src/**]` editorconfig glob, and honours
# a src-scoped WarningsNotAsErrors. One assertion closes all three routes plus
# the whitespace one.
#
# It is created here rather than at the top of the file on purpose: the project
# discovery above globs src/ and tests/, and a probe present at that moment would
# join the list Part 6 asserts over.
srcprobe="src/.ca1707-probe"
cleanup_srcprobe() { rm -rf "$srcprobe"; }
trap 'cleanup; cleanup_srcprobe' EXIT
rm -rf "$srcprobe"
mkdir -p "$srcprobe" || fail "cannot create $srcprobe"

if [ -d "$srcprobe" ]; then
  cat > "$srcprobe/Probe.csproj" <<'PROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
PROJ
  cat > "$srcprobe/Probe.cs" <<'PROBE'
namespace Ca1707Probe;

/// <summary>Probe.</summary>
public static class Probe
{
    /// <summary>Probe. The underscore is the point.</summary>
    public static int Get_Value() => 1;
}
PROBE
  srcprobe_out=$(dotnet build "$srcprobe/Probe.csproj" -v q --nologo 2>&1)
  if ! printf '%s\n' "$srcprobe_out" | grep -q "error CA1707"; then
    fail "a public member named Get_Value under src/ did not produce 'error CA1707'. The exemption written for tests/ has reached src/, or the rule was turned off another way. A NoWarn is only one of the routes: an .editorconfig severity line and a WarningsNotAsErrors entry both do this without changing NoWarn, and 7b below reads NoWarn only. Build output follows."
    printf '%s\n' "$srcprobe_out" | sed 's/^/    /'
  fi
  cleanup_srcprobe
fi

# --- 7b: the property read, for the one case 7a cannot reach ---
#
# A per-project <NoWarn> in some OTHER src project does not reach the probe, so
# 7a is blind to exactly the mechanism parameter D sanctions. This half covers
# it, and it also pins the number of exemptions.
#
# THE COUNT IS EXACT, NOT A FLOOR. scripts/CLAUDE.md: "A negative assertion fails
# open ... Where a property must hold negatively, assert a count." A floor of one
# passed a planted tests/Directory.Build.props on 2026-08-08, which is the
# directory carve-out ADR-0093 parameter B forbids by name: both test projects
# inherited the exemption, the count rose from 1 to 2, and nothing asserted the
# number. An exact count turns every new exemption into a deliberate edit here.
#
# get_proj_prop returns a sentinel rather than an empty string when evaluation
# fails, so a project that cannot be evaluated cannot satisfy the src/ ban by
# reading as blank.
ca1707_exempt=0
ca1707_expected=1
for proj in $real_projects; do
  p_nowarn=$(get_proj_prop "$proj" NoWarn)
  if [ "$p_nowarn" = "__EVAL_FAILED__" ]; then
    fail "could not evaluate NoWarn on $proj. The CA1707 ban below is a negative assertion, and an unreadable project would satisfy it silently."
    continue
  fi
  # Normalised, because MSBuild keeps the spaces and newlines an author writes
  # inside the element and the compiler ignores them. All three variants were
  # measured to suppress the diagnostic while evading an unnormalised pattern.
  p_norm=$(printf '%s' "$p_nowarn" | tr -d '[:space:]' | tr 'a-z' 'A-Z')
  case ";$p_norm;" in
    *";CA1707;"*)
      ca1707_exempt=$((ca1707_exempt + 1))
      case "$proj" in
        src/*)
          fail "NoWarn carries CA1707 in $proj, which is under src/. ADR-0093 parameter B allows the exemption only where a test genuinely needs it, and CA1707's own page limits it to test code. Evaluated value: '$p_nowarn'."
          ;;
      esac
      ;;
  esac
done

if [ "$ca1707_exempt" -ne "$ca1707_expected" ]; then
  fail "expected exactly $ca1707_expected project(s) to evaluate NoWarn with CA1707, found $ca1707_exempt. ADR-0093 parameter D makes each exemption a per-project opt-in, so a second one is a deliberate change that updates this number in the same commit. A count above the expected one is also what an inherited Directory.Build.props looks like from here, and that is the directory carve-out parameter B forbids. A count below it means the exemption was removed, in which case delete this part in the same change."
fi

# --- Report ---
if [ "$fails" -gt 0 ]; then
  echo "warnings-as-errors self-test FAILED: $fails assertion(s)."
  exit 1
fi
echo "warnings-as-errors self-test OK: the compliant fixture builds clean; a plain compiler warning, an unsafe DllImport that only the security axis reports, and a Recommended-tier violation each fail the build alone; the NuGet audit carve-out is present in the evaluated WarningsNotAsErrors; all four properties evaluate as decided on the $real_n real project(s) under src/ and tests/, not only on the probe; and CA1707 still fires on a probe inside src/ while exactly $ca1707_exempt project(s) carry the NoWarn exemption, none of them under src/."
exit 0
