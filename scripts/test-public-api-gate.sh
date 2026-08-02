#!/usr/bin/env bash
# Self-test for the public-API lock (ADR-0044 parameter A) and for Central
# Package Management (ADR-0026 section C). Run by CI and available locally.
#
# Why this exists, concretely rather than on principle. The gate landed on
# 2026-08-02 and one third of it was inert on arrival: RS0017 sat at its default
# severity of warning because a `dotnet_diagnostic` line cannot reach it from
# .editorconfig at all, and the case RS0017 uniquely covers is a public member
# deleted from the code with its lines left in the API file. That built green at
# exit 0, which is ADR-0044 parameter B's MAJOR-breaking direction passing a gate
# that read as configured. Nothing in the tree would have noticed, and nothing
# would notice it coming back. Part 3 below is that exact case.
#
# The gate is spread across three files and each can silence it alone:
#
#   .editorconfig             RS0016 and RS0037 severities        -> Parts 2, 4
#   Directory.Build.props     <WarningsAsErrors> carrying RS0017  -> Part 3
#   Directory.Packages.props  the version, and CPM being on       -> Parts 1, 5
#
# so the assertions are grouped by the failure, not by the file.
#
# The probe project is written INSIDE the repository on purpose, for the reason
# the sibling scripts/test-editorconfig.sh states at more length: MSBuild walks
# UP from a project directory, so a probe here inherits the real .editorconfig,
# the real Directory.Build.props and the real Directory.Packages.props rather
# than copies that could drift from what is being edited. scripts/CLAUDE.md
# records the trap that argument exists to avoid, where a self-test built its own
# isolated environment and passed while its subject was deliberately broken.
#
# It also means Part 1 proves something it does not assert directly: the probe's
# PackageReference carries no Version, so if CPM were off or the PackageVersion
# row were gone, Part 1 could not build at all.
#
# Proven the same way the sibling was, by breaking the subject on purpose. Five
# breaks, each reverted, and the useful part is which assertions each one moved:
#
#   drop dotnet_diagnostic.RS0016.severity  -> Part 2 only, 2 assertions
#   drop RS0017 from WarningsAsErrors       -> Part 3 only, 2 assertions
#   drop dotnet_diagnostic.RS0037.severity  -> Part 4 only, 2 assertions
#   ManagePackageVersionsCentrally = false  -> 8 assertions across every part
#   delete the PackageVersion row           -> 6 assertions across every part
#
# The first three isolating cleanly is the property that makes a red here
# readable: the failing part names the file to open. The last two cascading is
# correct rather than noisy, because nothing can restore, and Part 1's control
# fires first and says so in those words.
#
# Part 4 needed a second attempt to earn its line in that table. The obvious
# fixture, the compliant API file with its header deleted, also fires RS0016, so
# dropping the RS0037 severity left the build failing anyway and only the count
# assertion noticed. Its entries are now written unannotated so RS0037 is alone.
# Part 3 was built with that hazard in mind and Part 4 was not, which is worth
# knowing: an exit code is a weak assertion when several rules watch one fixture.
#
# Portable to macOS bash 3.2 and the Ubuntu runner, like the scripts beside it:
# no mapfile, no associative arrays, no GNU-only flags. Pure ASCII.
set -uo pipefail

cd "$(git rev-parse --show-toplevel)"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "public-API gate self-test SKIPPED: no dotnet SDK on PATH."
  echo "  This is a skip, not a pass. The gate is unverified in this run."
  exit 0
fi

probe=".publicapi-probe"
cleanup() { rm -rf "$probe"; }
trap cleanup EXIT
rm -rf "$probe"
mkdir -p "$probe" || { echo "public-API gate self-test FAILED: cannot create $probe"; exit 1; }

fails=0
fail() { echo "  FAIL: $1"; fails=$((fails + 1)); }

# TargetFramework is written literally rather than through the ADR-0030 knob,
# matching the sibling self-test. The knob is a different gate's subject, and
# reading it here would let a knob edit produce a red in a script about the API
# lock. Both scripts move together when the framework moves.
write_project() {
  # $1: extra attributes for the PackageReference (may be empty)
  cat > "$probe/Probe.csproj" <<PROJ
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.PublicApiAnalyzers"$1 />
  </ItemGroup>
</Project>
PROJ
}

write_compliant_source() {
  cat > "$probe/Probe.cs" <<'SRC'
namespace NamiPublicApiProbe;

public sealed class Probe
{
    public string Name { get; set; } = string.Empty;
}
SRC
}

write_api_files() {
  printf '#nullable enable\n' > "$probe/PublicAPI.Shipped.txt"
  cat > "$probe/PublicAPI.Unshipped.txt" <<'API'
#nullable enable
NamiPublicApiProbe.Probe
NamiPublicApiProbe.Probe.Name.get -> string!
NamiPublicApiProbe.Probe.Name.set -> void
NamiPublicApiProbe.Probe.Probe() -> void
API
}

# Both counts below are taken over deduplicated file:line:id sites, because
# MSBuild prints each diagnostic twice, once inline and once in the summary.
# scripts/CLAUDE.md records why these are counts and not per-line greps: a
# negative assertion written as a grep fails open, and every hard-coded line
# number in the first version of a sibling self-test was off by one with no
# assertion noticing. A count cannot pass by matching nothing.
count_sites() {
  # $1: build output, $2: diagnostic id
  printf '%s\n' "$1" | grep -oE "\(?[0-9]*,?[0-9]*\)?: error $2" | sort -u | grep -c "error $2"
}

build() { dotnet build "$probe/Probe.csproj" -v q --nologo 2>&1; }

# --- Part 1: the compliant fixture must build clean ---
#
# The control. Without it a script that fails on everything would report every
# break below as caught, which is the shape of a self-test that proves nothing.
write_project ' PrivateAssets="all"'
write_compliant_source
write_api_files

good_out=$(build)
good_rc=$?
good_rs=$(printf '%s\n' "$good_out" | grep -c "RS[0-9][0-9][0-9][0-9]")
if [ "$good_rc" -ne 0 ]; then
  fail "compliant fixture did not build (exit $good_rc). The gate rejects code it should accept, or CPM is not supplying the analyzer version."
  printf '%s\n' "$good_out" | sed 's/^/    /'
fi
if [ "$good_rs" -ne 0 ]; then
  fail "compliant fixture raised $good_rs RS diagnostic line(s); expected 0."
  printf '%s\n' "$good_out" | grep "RS[0-9][0-9][0-9][0-9]" | sed 's/^/    /'
fi

# --- Part 2: a public member absent from the API file must fail the build ---
#
# The direction everyone expects the gate to cover, and the only one that also
# fails `dotnet format`. It is guarded by the RS0016 line in .editorconfig;
# delete that line and this part is what goes red.
cat >> "$probe/Probe.cs" <<'SRC'

public sealed class Undeclared
{
    public int Value { get; set; }
}
SRC

add_out=$(build)
add_rc=$?
add_n=$(count_sites "$add_out" "RS0016")
if [ "$add_rc" -eq 0 ]; then
  fail "an undeclared public member BUILT. RS0016 is not at error severity; check .editorconfig."
fi
if [ "$add_n" -lt 1 ]; then
  fail "expected at least 1 RS0016 error site for the undeclared member, found $add_n."
  printf '%s\n' "$add_out" | grep "RS[0-9][0-9][0-9][0-9]" | sed 's/^/    /'
fi

# The same break on the other enforcement path. ADR-0065 keeps `dotnet build`
# and `dotnet format --verify-no-changes` as two gates rather than one, and this
# is the half of the API lock that both of them see. Part 3 is the half only one
# of them sees, which is asserted there rather than assumed here.
fmt_out=$(dotnet format "$probe/Probe.csproj" --verify-no-changes 2>&1)
fmt_rc=$?
if [ "$fmt_rc" -eq 0 ]; then
  fail "an undeclared public member is format-clean. The dotnet-format path is inert for RS0016."
fi

# --- Part 3: a STALE ENTRY must fail the build. This is the regression. ---
#
# A public member deleted from the code with its lines left in the API file.
# RS0017 is the only one of the three diagnostics that fires here, and while it
# was a warning this produced `2 Warning(s)`, `Build succeeded`, exit 0.
#
# It is guarded by <WarningsAsErrors> in Directory.Build.props and NOT by
# .editorconfig, because a severity is matched against the file a diagnostic is
# reported in and RS0017 is reported inside PublicAPI.Unshipped.txt. Four
# editorconfig placements were tried on 2026-08-02 and four left it at warning.
# So if someone "tidies" that property into .editorconfig for symmetry, every
# other part of this script stays green and this one goes red. That is the whole
# reason the file exists.
write_compliant_source
python3 - "$probe/Probe.cs" <<'PY'
import sys
p = sys.argv[1]
s = open(p).read().replace("    public string Name { get; set; } = string.Empty;\n", "")
open(p, "w").write(s)
PY
# A break experiment that fails to break reports the same green as a healthy
# subject. scripts/CLAUDE.md records that happening, so the edit is verified.
if grep -q "public string Name" "$probe/Probe.cs"; then
  fail "Part 3 setup did not modify the fixture; its result below means nothing."
fi

stale_out=$(build)
stale_rc=$?
stale_n=$(count_sites "$stale_out" "RS0017")
stale_16=$(count_sites "$stale_out" "RS0016")
if [ "$stale_rc" -eq 0 ]; then
  fail "a STALE API ENTRY built green. RS0017 is back at warning: a removed public member, which ADR-0044 parameter B calls MAJOR, now passes the gate silently."
fi
if [ "$stale_n" -lt 1 ]; then
  fail "expected at least 1 RS0017 error site for the stale entry, found $stale_n."
  printf '%s\n' "$stale_out" | grep "RS[0-9][0-9][0-9][0-9]" | sed 's/^/    /'
fi
# RS0016 must NOT fire here, or Part 3 would pass on the strength of Part 2's
# diagnostic and the RS0017 regression would be invisible again. This is the
# assertion that keeps the two parts independent.
if [ "$stale_16" -ne 0 ]; then
  fail "Part 3 also raised $stale_16 RS0016 site(s); the fixture is no longer a PURE removal and cannot isolate RS0017."
fi

# --- Part 4: a missing nullable header must fail the build ---
#
# Without `#nullable enable` in the API files, every recorded signature loses
# its `!` and `?` and parameter A stops versioning nullability. The header is
# project-wide rather than per-file: present in Shipped alone it satisfies the
# analyzer, so both files are stripped here.
#
# The ENTRIES ARE WRITTEN UNANNOTATED on purpose, and this is not cosmetic. The
# obvious fixture, which is the compliant API file with its header deleted, also
# fires RS0016, because entries carrying `!` stop matching what the analyzer
# expects once tracking is off. Part 4 would then pass on RS0016's back: dropping
# the RS0037 severity alone left the build failing and only the count assertion
# below noticed. Measured, and it is the same independence Part 3 needs. With the
# entries unannotated, RS0037 is the only diagnostic in the build.
write_compliant_source
: > "$probe/PublicAPI.Shipped.txt"
cat > "$probe/PublicAPI.Unshipped.txt" <<'API'
NamiPublicApiProbe.Probe
NamiPublicApiProbe.Probe.Name.get -> string
NamiPublicApiProbe.Probe.Name.set -> void
NamiPublicApiProbe.Probe.Probe() -> void
API

null_out=$(build)
null_rc=$?
null_n=$(count_sites "$null_out" "RS0037")
null_16=$(count_sites "$null_out" "RS0016")
if [ "$null_rc" -eq 0 ]; then
  fail "API files with no '#nullable enable' built green. RS0037 is not at error severity; check .editorconfig."
fi
if [ "$null_n" -lt 1 ]; then
  fail "expected at least 1 RS0037 error site, found $null_n."
  printf '%s\n' "$null_out" | grep "RS[0-9][0-9][0-9][0-9]" | sed 's/^/    /'
fi
if [ "$null_16" -ne 0 ]; then
  fail "Part 4 also raised $null_16 RS0016 site(s); the fixture cannot isolate RS0037 and the exit-code assertion above is passing on the wrong diagnostic."
fi

# --- Part 5: Central Package Management must still be on ---
#
# Two different mistakes, two different errors, and both are only errors while
# ManagePackageVersionsCentrally is true. Turn CPM off and NU1008 stops being a
# rule at all, which is the silent direction.
write_compliant_source
write_api_files

write_project ' Version="5.6.0" PrivateAssets="all"'
cpm_out=$(build)
cpm_rc=$?
if [ "$cpm_rc" -eq 0 ] || ! printf '%s\n' "$cpm_out" | grep -q "NU1008"; then
  fail "a Version on a PackageReference did not produce NU1008 (exit $cpm_rc). Central Package Management is off."
fi

# A package with no row in Directory.Packages.props. Deliberately a name that
# will never be adopted, so this cannot start resolving against a real feed.
write_project ' PrivateAssets="all" />
    <PackageReference Include="Nami.Identity.NoSuchPackage.SelfTestOnly"'
miss_out=$(build)
miss_rc=$?
if [ "$miss_rc" -eq 0 ] || ! printf '%s\n' "$miss_out" | grep -q "NU1010"; then
  fail "a package with no PackageVersion row did not produce NU1010 (exit $miss_rc)."
fi

# --- Part 6: PrivateAssets must keep the analyzer out of the packed surface ---
#
# design 01 section 3.1 line 97 says `Abstractions` depends on nothing, and an
# analyzer reference is compatible with that ONLY because of PrivateAssets. The
# analyzer's own nuspec declares developmentDependency=true, which reads as
# settling the question and does not: without PrivateAssets the produced package
# carries a real dependency on it and every consumer restores it.
#
# Asserted on obj/Release/<id>.<version>.nuspec, the nuspec pack generates and
# then packs. Compared against the copy inside the .nupkg on 2026-08-02: the
# <metadata> element, which is the part read here, is identical, and only the
# <files> element differs.
write_project ' PrivateAssets="all"'
nuspec="$probe/obj/Release/Probe.1.0.0.nuspec"

rm -f "$nuspec"
pack_out=$(dotnet pack "$probe/Probe.csproj" -c Release -v q --nologo 2>&1)
pack_rc=$?
if [ "$pack_rc" -ne 0 ] || [ ! -f "$nuspec" ]; then
  fail "pack with PrivateAssets did not produce $nuspec (exit $pack_rc). Part 6 asserts nothing."
  printf '%s\n' "$pack_out" | sed 's/^/    /'
elif grep -q "<dependency id=" "$nuspec"; then
  fail "packed with PrivateAssets=all and the package STILL declares a dependency. Abstractions would no longer depend on nothing."
  grep "<dependency id=" "$nuspec" | sed 's/^/    /'
fi

write_project ''
rm -f "$nuspec"
pack2_out=$(dotnet pack "$probe/Probe.csproj" -c Release -v q --nologo 2>&1)
pack2_rc=$?
if [ "$pack2_rc" -ne 0 ] || [ ! -f "$nuspec" ]; then
  fail "pack without PrivateAssets did not produce $nuspec (exit $pack2_rc); the control for Part 6 is missing."
  printf '%s\n' "$pack2_out" | sed 's/^/    /'
elif ! grep -q "<dependency id=\"Microsoft.CodeAnalysis.PublicApiAnalyzers\"" "$nuspec"; then
  # Not a failure of this repository: it means NuGet changed how it treats a
  # developmentDependency package, so PrivateAssets may no longer be what keeps
  # the analyzer out. The comments in the csproj and in src/CLAUDE.md assert the
  # old behaviour and would be stale.
  fail "pack WITHOUT PrivateAssets no longer declares the analyzer as a dependency. NuGet's behaviour changed; re-read the PrivateAssets rationale in src/Nami.Identity.Abstractions/Nami.Identity.Abstractions.csproj before trusting it."
fi

# --- Report ---
if [ "$fails" -gt 0 ]; then
  echo "public-API gate self-test FAILED: $fails assertion(s)."
  exit 1
fi
echo "public-API gate self-test OK: compliant fixture builds clean; an undeclared member fails build and format on RS0016; a stale entry fails the build on RS0017 alone; a missing nullable header fails on RS0037; CPM answers NU1008 and NU1010; and PrivateAssets keeps the analyzer out of the packed surface."
exit 0
