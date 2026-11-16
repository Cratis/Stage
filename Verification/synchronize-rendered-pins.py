#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Synchronize emitted npm pins with published releases and Stage's central Arc pin."""

import argparse
from contextlib import contextmanager
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
PROFILE = Path("Source/Rendering.Cratis/Scaffolding/CratisFrontendPackageSet.cs")
MANIFEST = Path("Source/Frontend/package.json")
LOCKFILE = MANIFEST.with_name("package-lock.json")
RUNTIME = Path("Source/Frontend/Runtime/package.json")
CATALOG = Path("Source/Rendering.Cratis/Naming/Imports/FrameworkNames.catalog")
PROJECT = "Source/Rendering.Cratis/Rendering.Cratis.csproj"
STABLE = re.compile(r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\Z")
SCENE = ("blueprint.default", "components", "engine", "model", "primereact", "react")
REQUIRED = {"@cratis/arc", "@cratis/arc.react", "@cratis/components", "@cratis/fundamentals",
            *("@cratis/scene." + name for name in SCENE)}
CURRENT = re.compile(r"(internal static CratisFrontendPackageSet Current \{ get; \} = new\()(.*?)(\);)", re.S)
CONSTRUCTOR = re.compile(r"\n\s*CratisFrontendPackageSet\(((?:\s*string \w+,?)+)\s*\)")
# The Current arguments are positional; each Cratis pin is located by the constructor parameter it binds to.
PARAMETERS = {"arcPackageVersion": "@cratis/arc", "arcReactPackageVersion": "@cratis/arc.react",
              "arcVitePackageVersion": "@cratis/arc.vite", "fundamentalsPackageVersion": "@cratis/fundamentals",
              "componentsPackageVersion": "@cratis/components", "scenePackageVersion": "@cratis/scene.model"}


def stable(version):
    if not isinstance(version, str) or not STABLE.fullmatch(version):
        raise ValueError(f"Require an exact stable version, found {version!r}")
    return version


def arc_version(xml):
    pins = ET.fromstring(xml).findall(".//PackageVersion[@Include='Cratis.Arc']")
    if len(pins) != 1 or set(pins[0].attrib) != {"Include", "Version"}:
        raise ValueError("Cratis.Arc: require one literal central PackageVersion")
    version = stable(pins[0].get("Version"))
    meta = ET.fromstring(xml).findall(".//PackageVersion[@Include='Cratis']")
    if len(meta) != 1 or meta[0].get("Version") != version:
        raise ValueError("Cratis and Cratis.Arc central pins differ")
    return version


def cratis_pins(manifest):
    pins = {}
    for section in ("dependencies", "devDependencies"):
        for name, version in manifest.get(section, {}).items():
            # The Stage runtime is a local npm workspace linked with "*", not a published pin.
            if name.startswith("@cratis/") and name != "@cratis/stage.runtime":
                if name in pins:
                    raise ValueError(f"Duplicate npm pin: {name}")
                pins[name] = stable(version)
    if not REQUIRED <= pins.keys() or len(pins) > 32:
        raise ValueError(f"Expected the shared Cratis packages and at most 32 pins; found {len(pins)}")
    return pins


def validate(manifest, arc):
    pins = cratis_pins(manifest)
    drift = [f"{name}={version}" for name, version in pins.items()
             if (name == "@cratis/arc" or name.startswith("@cratis/arc.")) and version != arc]
    if drift:
        raise ValueError(f"npm Arc != NuGet Arc {arc}: {', '.join(drift)}")
    scenes = {pins["@cratis/scene." + name] for name in SCENE}
    if len(scenes) != 1:
        raise ValueError("Scene npm family versions differ")
    return pins


class CommandFailed(RuntimeError):
    def __init__(self, error):
        super().__init__(f"Command failed: {error}; {error.stdout or ''}; {error.stderr or ''}")
        if error.cmd[0] == "npm":
            # npm uses exit 1 for both dependency defects and an unavailable registry.
            defect = re.search(r"\b(?:ERESOLVE|EJSONPARSE|EINVALIDPACKAGENAME|EINVALIDTAGNAME)\b", error.stderr or "")
            self.code = 1 if defect else 2
        else:
            self.code = error.returncode if error.returncode in (1, 2) else 2


def run(command, **kwargs):
    try:
        return subprocess.run(command, check=True, text=True, timeout=300, **kwargs)
    except subprocess.CalledProcessError as error:
        raise CommandFailed(error) from error
    except (OSError, subprocess.TimeoutExpired) as error:
        raise RuntimeError(f"Could not run {command[0]} ({command[1:]}): {error}") from error


def published(package, version=None):
    subject = package if version is None else f"{package}@{version}"
    result = run(["npm", "view", subject, "version", "--json", "--fetch-retries=0", "--fetch-timeout=15000"], capture_output=True)
    value = json.loads(result.stdout)
    if isinstance(value, list) and len(value) == 1:
        value = value[0]
    resolved = stable(value)
    if version is not None and resolved != version:
        raise ValueError(f"{package}: requested {version}, registry returned {resolved}")
    return resolved


def releases(package, floor):
    # Every stable release from the current pin upward, newest first, with its declared peer ranges.
    result = run(["npm", "view", f"{package}@>={floor}", "version", "peerDependencies", "--json",
                  "--fetch-retries=0", "--fetch-timeout=15000"], capture_output=True)
    value = json.loads(result.stdout or "[]")
    found = dict(filter(None, map(release, value if isinstance(value, list) else [value])))
    return sorted(found.items(), key=lambda item: semver(item[0]), reverse=True)


def release(entry):
    # npm lists bare version strings when no matching release declares peers.
    entry = {"version": entry} if isinstance(entry, str) else entry
    version = entry.get("version") if isinstance(entry, dict) else None
    if not isinstance(version, str) or not STABLE.fullmatch(version):
        return None
    return version, dict(entry.get("peerDependencies") or {})


def semver(version):
    return tuple(int(part) for part in stable(version).split("."))


PARTIAL = re.compile(r"v?((?:0|[1-9][0-9]*|[xX*])(?:\.(?:0|[1-9][0-9]*|[xX*])){0,2})\Z")
OPERATOR = r"(<=|>=|<|>|=|\^|~)"
COMPARATOR = re.compile(OPERATOR + r"?(.+)")
SEARCH_LIMIT = 10000


def partial(text):
    match = PARTIAL.fullmatch(text)
    if not match:
        raise ValueError(f"Unsupported version in peer range: {text!r}")
    parts = []
    for part in match[1].split("."):
        if part in "xX*":
            break
        parts.append(int(part))
    return parts


def bump(parts, index):
    return tuple(parts[:index]) + (parts[index] + 1,) + (0,) * (2 - index)


def comparators(operator, text):
    # Translate one npm comparator into (operator, exact version) bounds; partial versions are x-ranges.
    parts = partial(text)
    if not parts:
        return [] if operator in (None, "=", ">=", "<=", "^", "~") else [("<", (0, 0, 0))]
    low = tuple(parts + [0] * (3 - len(parts)))
    last = len(parts) - 1
    if operator in (None, "="):
        return [("=", low)] if len(parts) == 3 else [(">=", low), ("<", bump(parts, last))]
    if operator == "^":
        return [(">=", low), ("<", bump(parts, next((index for index, part in enumerate(parts) if part), last)))]
    if operator == "~":
        return [(">=", low), ("<", bump(parts, min(1, last)))]
    return comparison(operator, parts, low)


def comparison(operator, parts, low):
    # A partial version widens > and <= to the next release of its last given part.
    if len(parts) == 3 or operator in (">=", "<"):
        return [(operator, low)]
    return [(">=" if operator == ">" else "<", bump(parts, len(parts) - 1))]


CHECKS = {"=": lambda a, b: a == b, ">=": lambda a, b: a >= b, ">": lambda a, b: a > b,
          "<=": lambda a, b: a <= b, "<": lambda a, b: a < b}


def satisfies(version, peer_range):
    """Evaluate an npm peer range for an exact stable version; unsupported syntax fails closed."""
    alternatives = parse_range(peer_range)
    target = semver(version)
    return any(all(CHECKS[operator](target, bound) for operator, bound in bounds) for bounds in alternatives)


def parse_range(peer_range):
    # Parse every OR alternative and every AND comparator before evaluation can short-circuit.
    return [range_bounds(alternative) for alternative in peer_range.split("||")]


def range_bounds(alternative):
    hyphen = re.fullmatch(r"\s*(\S+)\s+-\s+(\S+)\s*", alternative)
    if hyphen:
        return comparators(">=", hyphen[1]) + ceiling(partial(hyphen[2]))
    # Every whitespace-separated token must be one whole comparator; anything else fails closed.
    tokens = re.sub(OPERATOR + r"\s+", r"\1", alternative.strip()).split()
    return [bound for token in tokens for bound in comparators(*comparator(token))]


def comparator(token):
    match = COMPARATOR.fullmatch(token)
    if not match:
        raise ValueError(f"Unsupported comparator in peer range: {token!r}")
    return match[1], match[2]


def ceiling(upper):
    # The upper end of a hyphen range is inclusive when exact and an x-range when partial.
    if not upper:
        return []
    return [("<=", tuple(upper))] if len(upper) == 3 else [("<", bump(upper, len(upper) - 1))]


def family(name):
    # Scene packages ship as one version family; every other non-Arc package is selected on its own.
    return "@cratis/scene.*" if name.startswith("@cratis/scene.") else name


def family_releases(names, pins, lookup, history):
    # Releases every member publishes, between the current pin and the registry's latest, newest first.
    shared = None
    for name in names:
        latest = semver(lookup(name))
        offered = {version: list(peers.items()) for version, peers in history(name, pins[name]) if semver(version) <= latest}
        shared = offered if shared is None else {version: shared[version] + offered[version]
                                                   for version in shared.keys() & offered.keys()}
    if not shared:
        raise ValueError(f"{', '.join(names)}: no shared release between the current pin and the latest")
    return sorted(shared.items(), key=lambda item: semver(item[0]), reverse=True)


def unmet(peers, selection):
    return next(((peer, peer_range) for peer, peer_range in peers
                 if peer in selection and not satisfies(selection[peer], peer_range)), None)


def hold_back(families, selection):
    """Choose the newest releases whose Cratis peer ranges accept one another and the fixed selection.

    The Arc family is fixed by the central NuGet pin and is never held back. Every other family is tried
    newest first, down to its current pin, with backtracking across families so an older release of one
    family can make another family's release fit. If no combination fits, there is no supported update
    and the run stops before npm instead of failing in lockfile generation.
    """
    viable = {key: viable_releases(*families[key], selection) for key in sorted(families)}
    search = ReleaseSearch(families, viable)
    chosen = search.first(list(viable), selection, [])
    if chosen is None:
        family_key, version, (peer, peer_range) = search.conflicts[0]
        raise ValueError(f"No compatible update: no combination of {', '.join(viable)} releases satisfies their peer "
                         f"ranges (for example {family_key} {version} breaks {peer} {peer_range})")
    return chosen


def viable_releases(names, options, selection):
    # Releases whose peers already reject the fixed Arc selection can never be part of a compatible set.
    kept = [(version, peers) for version, peers in options if unmet(peers, selection) is None]
    if not kept:
        raise no_compatible_update(names, options, unmet(options[-1][1], selection), selection)
    return kept


class ReleaseSearch:
    """Depth-first search over families, newest release first, bounded by SEARCH_LIMIT candidates."""

    def __init__(self, families, viable):
        self.families, self.viable = families, viable
        self.budget, self.conflicts = SEARCH_LIMIT, []

    def first(self, keys, chosen, constraints):
        if not keys:
            return chosen
        for version, peers in self.viable[keys[0]]:
            attempt = self.attempt(keys[0], version, chosen)
            problem = unmet([*constraints, *peers], attempt)
            if problem:
                self.conflicts.append((keys[0], version, problem))
                continue
            found = self.first(keys[1:], attempt, [*constraints, *peers])
            if found is not None:
                return found
        return None

    def attempt(self, key, version, chosen):
        self.budget -= 1
        if self.budget < 0:
            raise ValueError(f"No compatible update found within {SEARCH_LIMIT} candidate combinations; "
                             "narrow the update or raise SEARCH_LIMIT")
        return {**chosen, **dict.fromkeys(self.families[key][0], version)}


def no_compatible_update(names, options, problem, chosen):
    peer, peer_range = problem
    return ValueError(f"No compatible update for {', '.join(names)}: every release from {options[-1][0]} to "
                      f"{options[0][0]} rejects the selected {peer} {chosen[peer]} (oldest needs {peer} {peer_range}); "
                      "a compatible upstream release is required before this update can land")


def is_arc(name):
    return name == "@cratis/arc" or name.startswith("@cratis/arc.")


def selectable(pins, fixed, lookup, history):
    members = {}
    for name in sorted(pins.keys() - fixed.keys()):
        members.setdefault(family(name), []).append(name)
    return {key: (names, family_releases(names, pins, lookup, history)) for key, names in members.items()}


def candidate(manifest, arc, lookup=published, history=releases):
    # Work on an independent document; resolve every candidate before writing anything.
    updated = json.loads(json.dumps(manifest))
    pins = cratis_pins(manifest)
    selection = {name: lookup(name, arc) for name in pins if is_arc(name)}
    for name, version in hold_back(selectable(pins, selection, lookup, history), selection).items():
        for section in (section for section in ("dependencies", "devDependencies") if name in updated.get(section, {})):
            updated[section][name] = stable(version)
    validate(updated, arc)
    if "@cratis/arc.vite" not in cratis_pins(updated) and lookup("@cratis/arc.vite", arc) != arc:
        raise ValueError(f"@cratis/arc.vite: emitted version {arc} is not published")
    return updated


def frontend_profile(source, pins):
    matches = list(CURRENT.finditer(source))
    if len(matches) != 1:
        raise ValueError(f"{PROFILE}: require one literal Current package set")
    match = matches[0]
    parameters = profile_parameters(source)
    versions = list(re.finditer(r'"([0-9]+\.[0-9]+\.[0-9]+)"', match[2]))
    if len(versions) != len(parameters):
        raise ValueError(f"{PROFILE}: expected {len(parameters)} exact package arguments, found {len(versions)}")
    replacements = {parameters.index(parameter): pins.get(package, pins["@cratis/arc"]) if package == "@cratis/arc.vite" else pins[package]
                    for parameter, package in PARAMETERS.items()}
    body = match[2]
    for index, version in sorted(replacements.items(), reverse=True):
        token = versions[index]
        body = body[:token.start(1)] + version + body[token.end(1):]
    return source[:match.start(2)] + body + source[match.end(2):]


def profile_parameters(source):
    constructors = CONSTRUCTOR.findall(source)
    if len(constructors) != 1:
        raise ValueError(f"{PROFILE}: require one private constructor of string package versions")
    parameters = re.findall(r"string (\w+)", constructors[0])
    missing = sorted(set(PARAMETERS) - set(parameters))
    if missing or len(set(parameters)) != len(parameters):
        raise ValueError(f"{PROFILE}: constructor must name each of {', '.join(PARAMETERS)} once; missing {', '.join(missing) or 'none'}")
    return parameters


@contextmanager
def pin_transaction(root):
    # npm and the catalog generator write in place. Roll back the complete bounded
    # output set on failure, including partial writes and newly created files.
    originals = {root / path: (root / path).read_bytes() if (root / path).exists() else None
                 for path in (MANIFEST, RUNTIME, LOCKFILE, PROFILE, CATALOG)}
    try:
        yield
    except BaseException:
        for path, content in originals.items():
            if content is None:
                path.unlink(missing_ok=True)
            elif not path.exists() or path.read_bytes() != content:
                path.write_bytes(content)
        raise


def runtime_peers(content, old, new):
    # The Stage runtime workspace declares exact peers on the same Cratis packages; npm refuses a
    # lockfile whose root pins disagree with them, so they move together with the root manifest.
    peers = json.loads(content).get("peerDependencies", {})
    for name in sorted(peers.keys() & new.keys()):
        if peers[name] != old[name]:
            raise ValueError(f"{RUNTIME}: peer {name}={peers[name]} differs from {MANIFEST} {old[name]}")
        token = f'"{name}": "{old[name]}"'
        if content.count(token) != 1:
            raise ValueError(f"{RUNTIME}: expected exactly one token for {name}")
        content = content.replace(token, f'"{name}": "{new[name]}"')
    return content


def regenerate_catalog(root):
    command = ["dotnet", "test", PROJECT, "--configuration", "Debug", "-m:3"]
    # The embedded OLD catalog cannot be checked against the NEW profile yet.
    # Generate only, then embed the result in a new build before validating all specs.
    environment = {key: value for key, value in os.environ.items() if key != "STAGE_WRITE_FRAMEWORK_NAME_CATALOG"}
    run([*command, "--filter", "FullyQualifiedName~for_FrameworkNameCatalog.when_generating_from_the_restored_scaffold"],
        cwd=root, env={**environment, "STAGE_WRITE_FRAMEWORK_NAME_CATALOG": "1"})
    run(["dotnet", "build", PROJECT, "--configuration", "Debug", "-m:3"], cwd=root, env=environment)
    run([*command, "--no-build", "--filter", "FullyQualifiedName~for_FrameworkNameCatalog"], cwd=root, env=environment)


def synchronize(root):
    if not shutil.which("npm"):
        raise RuntimeError("npm is unavailable; install Node/npm before synchronizing rendered pins")
    packages = root / "Directory.Packages.props"
    manifest_path, profile_path, runtime_path = root / MANIFEST, root / PROFILE, root / RUNTIME
    originals = {path: path.read_bytes() for path in (packages, manifest_path, profile_path, runtime_path)}
    arc = arc_version(originals[packages])
    manifest = json.loads(originals[manifest_path])
    updated = candidate(manifest, arc)
    profile = frontend_profile(originals[profile_path].decode(), validate(updated, arc))
    if any(path.read_bytes() != content for path, content in originals.items()):
        raise ValueError("Rendered pin inputs changed during registry lookup; refusing to overwrite")
    # Preserve all unrelated manifest text, rather than reformatting it.
    content = originals[manifest_path].decode()
    for name, version in cratis_pins(updated).items():
        old = cratis_pins(manifest)[name]
        token = f'"{name}": "{old}"'
        if content.count(token) != 1:
            raise ValueError(f"{MANIFEST}: expected exactly one token for {name}")
        content = content.replace(token, f'"{name}": "{version}"')
    runtime = runtime_peers(originals[runtime_path].decode(), cratis_pins(manifest), cratis_pins(updated))
    with pin_transaction(root):
        manifest_path.write_text(content)
        runtime_path.write_text(runtime)
        profile_path.write_text(profile)
        run(["npm", "install", "--package-lock-only", "--prefix", "Source/Frontend"], cwd=root, capture_output=True)
        regenerate_catalog(root)
        validate(json.loads(manifest_path.read_text()), arc_version(packages.read_bytes()))
        if frontend_profile(profile_path.read_text(), cratis_pins(updated)) != profile_path.read_text():
            raise ValueError(f"{PROFILE}: emitted npm pins drifted during synchronization")
    count = len(cratis_pins(updated))
    changed = sum(version != cratis_pins(manifest)[name] for name, version in cratis_pins(updated).items())
    print(f"PASS: checked {count} npm pins, updated {changed}; synchronized emitted profile, lockfile and framework-name catalog")


class RenderedPinSynchronization(unittest.TestCase):
    def setUp(self):
        self.manifest = json.loads((ROOT / MANIFEST).read_text())
        self.arc = arc_version((ROOT / "Directory.Packages.props").read_bytes())
        # The live manifest can disagree with the NuGet Arc right after a NuGet bump; pin the fixture to self.arc.
        for name in self.manifest["dependencies"]:
            if name == "@cratis/arc" or name.startswith("@cratis/arc."):
                self.manifest["dependencies"][name] = self.arc

    def test_detects_planted_arc_drift(self):
        self.manifest["dependencies"]["@cratis/arc"] = "0.0.1"
        with self.assertRaisesRegex(ValueError, "npm Arc != NuGet Arc"):
            validate(self.manifest, self.arc)

    def test_repairs_drift_using_exact_arc_release(self):
        self.manifest["dependencies"]["@cratis/arc"] = "0.0.1"
        lookup = Mock(side_effect=lambda name, version=None: version or cratis_pins(self.manifest)[name])
        result = candidate(self.manifest, self.arc, lookup, self.history())
        self.assertEqual(validate(result, self.arc)["@cratis/arc"], self.arc)
        self.assertEqual(self.manifest["dependencies"]["@cratis/arc"], "0.0.1")
        self.assertIn(unittest.mock.call("@cratis/arc.react", self.arc), lookup.call_args_list)

    def history(self, extra=None):
        # Offline registry: planted releases, plus the current pin without peers unless a planted release replaces it.
        def offered(name, floor):
            planted = (extra or {}).get(name, [])
            return planted if floor in dict(planted) else [*planted, (floor, {})]
        return offered

    def plant_notes(self, releases):
        # A newly published package release that still declares an Arc peer range the selected Arc rejects.
        names = ("@cratis/showrunner.notes", "@cratis/showrunner.notes.react")
        for name in names:
            self.manifest["dependencies"][name] = "1.0.0"
        latest = releases[0][0]
        lookup = lambda name, version=None: version or (latest if name in names else cratis_pins(self.manifest)[name])
        return lookup, self.history(dict.fromkeys(names, releases))

    def test_holds_back_a_release_whose_arc_peer_rejects_the_selected_arc(self):
        major = semver(self.arc)[0]
        lookup, history = self.plant_notes([("2.13.0", {"@cratis/arc": f"^{major - 1}.185.1"}),
                                            ("2.0.0", {"@cratis/arc": f">={major - 1}.0.0 <{major + 1}"})])
        result = validate(candidate(self.manifest, self.arc, lookup, history), self.arc)
        self.assertEqual(result["@cratis/showrunner.notes"], "2.0.0")
        self.assertEqual(result["@cratis/arc"], self.arc)

    def test_reports_no_compatible_update_before_npm(self):
        major = semver(self.arc)[0]
        old = {"@cratis/arc": f"^{major - 1}.155.0"}
        lookup, history = self.plant_notes([("2.13.0", {"@cratis/arc": f"^{major - 1}.185.1"}), ("1.0.0", old)])
        with self.assertRaisesRegex(ValueError, r"No compatible update for @cratis/showrunner\.notes.*@cratis/arc " + re.escape(self.arc)):
            candidate(self.manifest, self.arc, lookup, history)

    def test_peer_ranges_follow_npm_semantics(self):
        accepted = {"22.185.1": ["^22.185.1", ">=20.3.1 <24", "22.x", "~22.185.0", "21 - 22", "^21 || ^22", "*", ""],
                    "23.8.0": [">=20.3.1 <24", ">22", "^23.0", "<=23", "23.8.0", "=23.8.0", "22.0.0 - 23.8.0"],
                    "0.2.5": ["^0.2.1", "~0.2", "^0"]}
        rejected = {"23.8.0": ["^22.185.1", "^22.155.0", "~23.7.0", "<23.8.0", ">23.8.0", "22.x", "21 - 22"],
                    "0.3.0": ["^0.2.1", "~0.2"], "0.0.4": ["^0.0.3"]}
        for expected, cases in ((True, accepted), (False, rejected)):
            for version, ranges in cases.items():
                for peer_range in ranges:
                    with self.subTest(version=version, peer_range=peer_range):
                        self.assertEqual(satisfies(version, peer_range), expected)
        with self.assertRaises(ValueError):
            satisfies("23.8.0", ">=23.0.0-beta")

    def test_unsupported_comparators_fail_closed(self):
        for peer_range in ("~>1.2.3", "<", ">=", "^", "1.2.3 garbage", ">=1.0.0 <", "1 - 2 - 3", "1.2.3>=1.0.0"):
            with self.subTest(peer_range=peer_range), self.assertRaises(ValueError):
                satisfies("1.2.3", peer_range)
        self.assertTrue(satisfies("1.2.3", ">= 1.0.0  < 2"))

    def test_validates_the_complete_range_before_evaluating_matches(self):
        for peer_range in ("* || ~>1.2.3", "1.x || <", ">2.0.0 <", "* || >2.0.0 ~>1.2.3"):
            with self.subTest(peer_range=peer_range), self.assertRaises(ValueError):
                satisfies("1.2.3", peer_range)

    def test_wildcard_upper_bounds_are_unrestricted(self):
        for peer_range in ("<=*", "<=x", ">=*", "*", "x"):
            with self.subTest(peer_range=peer_range):
                self.assertTrue(satisfies("23.8.0", peer_range))
        self.assertFalse(satisfies("23.8.0", "<*"))

    def test_backtracks_to_an_older_release_of_a_later_family(self):
        # Greedy selection exhausts A (its only release needs B ^1) before ever trying B 1.0.0.
        major = semver(self.arc)[0]
        families = {"@cratis/a": (["@cratis/a"], [("1.0.0", [("@cratis/b", "^1.0.0")])]),
                    "@cratis/b": (["@cratis/b"], [("2.0.0", []), ("1.0.0", [("@cratis/arc", f"^{major}.0.0")])])}
        chosen = hold_back(families, {"@cratis/arc": self.arc})
        self.assertEqual((chosen["@cratis/a"], chosen["@cratis/b"]), ("1.0.0", "1.0.0"))
        families["@cratis/b"][1][0] = ("2.0.0", [("@cratis/arc", f"^{major - 1}.0.0")])
        self.assertEqual(hold_back(families, {"@cratis/arc": self.arc})["@cratis/b"], "1.0.0")

    def test_backtracks_across_interacting_families(self):
        # A 2.0.0 needs B ^2, B 2.0.0 needs C ^2, C only offers 1.0.0: the newest compatible set is A1, B1, C1.
        families = {"@cratis/a": (["@cratis/a"], [("2.0.0", [("@cratis/b", "^2")]), ("1.0.0", [("@cratis/b", "^1")])]),
                    "@cratis/b": (["@cratis/b"], [("2.0.0", [("@cratis/c", "^2")]), ("1.0.0", [])]),
                    "@cratis/c": (["@cratis/c"], [("1.0.0", [("@cratis/a", ">=1")])])}
        chosen = hold_back(families, {"@cratis/arc": self.arc})
        self.assertEqual([chosen["@cratis/" + key] for key in "abc"], ["1.0.0", "1.0.0", "1.0.0"])
        families["@cratis/c"][1][0] = ("1.0.0", [("@cratis/a", "^2")])
        with self.assertRaisesRegex(ValueError, "No compatible update: no combination"):
            hold_back(families, {"@cratis/arc": self.arc})

    def test_search_limit_fails_clearly(self):
        releases_of = [(f"{index}.0.0", [("@cratis/b", "^99")]) for index in range(5, 0, -1)]
        families = {"@cratis/a": (["@cratis/a"], releases_of), "@cratis/b": (["@cratis/b"], [("1.0.0", [])])}
        with patch.dict(hold_back.__globals__, SEARCH_LIMIT=3), self.assertRaisesRegex(ValueError, "within 3 candidate"):
            hold_back(families, {"@cratis/arc": self.arc})

    def test_scene_family_moves_together_on_shared_releases(self):
        # 5.5.0: scene.model rejects Arc; 5.4.9: scene.engine never published it; 5.4.8 fits every member.
        major = semver(self.arc)[0]
        scenes = ["@cratis/scene." + name for name in (*SCENE, "extensions")]
        planted = {}
        for name in scenes:
            rejecting = {"@cratis/arc": f"^{major - 1}.0.0"} if name == "@cratis/scene.model" else {}
            planted[name] = [("5.5.0", rejecting), *([] if name == "@cratis/scene.engine" else [("5.4.9", {})]), ("5.4.8", {})]
        lookup = lambda name, version=None: version or ("5.5.0" if name in scenes else cratis_pins(self.manifest)[name])
        result = validate(candidate(self.manifest, self.arc, lookup, self.history(planted)), self.arc)
        self.assertEqual({result[name] for name in scenes if name in result}, {"5.4.8"})

    def test_rejects_empty_population_prereleases_and_scene_drift(self):
        for broken in ({}, {"dependencies": {"@cratis/arc": "1.0.0-beta"}}):
            with self.assertRaises(ValueError):
                validate(broken, self.arc)
        self.manifest["dependencies"]["@cratis/scene.model"] = "0.0.1"
        with self.assertRaisesRegex(ValueError, "Scene npm"):
            validate(self.manifest, self.arc)

    def test_profile_repairs_drift_and_preserves_other_arguments(self):
        source = (ROOT / PROFILE).read_text()
        pins = cratis_pins(self.manifest)
        pins["@cratis/arc"] = "99.0.1"
        updated = frontend_profile(source, pins)
        self.assertIn('"99.0.1"', CURRENT.search(updated)[2])
        self.assertEqual(frontend_profile(updated, pins), updated)
        self.assertEqual(len(re.findall(r'"[0-9]+\.[0-9]+\.[0-9]+"', CURRENT.search(updated)[2])), len(profile_parameters(source)))

    def test_profile_locates_pins_by_parameter_when_a_package_is_added(self):
        source = (ROOT / PROFILE).read_text()
        pins = cratis_pins(self.manifest)
        pins["@cratis/components"] = "98.0.1"
        widened = re.sub(r"(\n(\s*)string arcPackageVersion,)", r"\n\2string addedPackageVersion,\1", source, count=1)
        widened = CURRENT.sub(lambda match: match[1] + '\n        "1.2.3",' + match[2] + match[3], widened, count=1)
        arguments = re.findall(r'"([0-9]+\.[0-9]+\.[0-9]+)"', CURRENT.search(frontend_profile(widened, pins))[2])
        parameters = profile_parameters(widened)
        self.assertEqual(arguments[0], "1.2.3")
        self.assertEqual(arguments[parameters.index("componentsPackageVersion")], "98.0.1")

    def test_profile_rejects_a_missing_pin_parameter(self):
        source = (ROOT / PROFILE).read_text().replace("string scenePackageVersion", "string sceneAlias", 1)
        with self.assertRaisesRegex(ValueError, "scenePackageVersion"):
            frontend_profile(source, cratis_pins(self.manifest))

    def test_published_accepts_scalar_and_singleton_versions(self):
        for response in ('"22.146.0"', '["22.146.0"]'):
            with self.subTest(response=response), patch(__name__ + ".run", return_value=Mock(stdout=response)):
                self.assertEqual(published("@cratis/arc", "22.146.0"), "22.146.0")

    def test_published_rejects_ambiguous_or_invalid_versions(self):
        for response in ('[]', '["1.0.0", "2.0.0"]', '[["1.0.0"]]', '[null]', '[3]', '["1.0.0-beta"]'):
            with self.subTest(response=response), patch(__name__ + ".run", return_value=Mock(stdout=response)):
                with self.assertRaises(ValueError):
                    published("@cratis/arc")

    def test_published_rejects_a_different_exact_version(self):
        with patch(__name__ + ".run", return_value=Mock(stdout='["22.145.0"]')):
            with self.assertRaisesRegex(ValueError, "requested 22.146.0"):
                published("@cratis/arc", "22.146.0")

    def test_runtime_peers_move_with_the_manifest(self):
        # The runtime peers follow the committed root manifest, not the Arc-normalized fixture: right after a
        # NuGet-only Arc bump the fixture already carries the new Arc while both npm manifests carry the old one.
        content = (ROOT / RUNTIME).read_text()
        old = cratis_pins(json.loads((ROOT / MANIFEST).read_text()))
        new = {**old, "@cratis/components": "97.0.1", "@cratis/arc": "98.0.1", "@cratis/arc.react": "98.0.1"}
        updated = json.loads(runtime_peers(content, old, new))["peerDependencies"]
        self.assertEqual(updated["@cratis/components"], "97.0.1")
        self.assertEqual(updated["@cratis/arc"], "98.0.1")
        self.assertEqual(updated["react"], json.loads(content)["peerDependencies"]["react"])

    def test_runtime_peer_drift_is_refused(self):
        content = (ROOT / RUNTIME).read_text()
        old = {**cratis_pins(json.loads((ROOT / MANIFEST).read_text())), "@cratis/components": "0.0.1"}
        with self.assertRaisesRegex(ValueError, "peer @cratis/components"):
            runtime_peers(content, old, old)

    def test_unavailable_registry_does_not_mutate_manifest(self):
        original = json.dumps(self.manifest)
        with self.assertRaises(RuntimeError):
            candidate(self.manifest, self.arc, Mock(side_effect=RuntimeError("npm unavailable")), self.history())
        self.assertEqual(json.dumps(self.manifest), original)

    def test_npm_failure_restores_every_output(self):
        self.assert_failed_transaction("npm")

    def test_catalog_failure_restores_every_output(self):
        self.assert_failed_transaction("dotnet")

    def assert_failed_transaction(self, failing_tool):
        with tempfile.TemporaryDirectory(prefix="pin-self-test-", dir=ROOT / ".ai-work") as directory:
            root = Path(directory)
            paths = (Path("Directory.Packages.props"), MANIFEST, RUNTIME, LOCKFILE, PROFILE, CATALOG)
            for path in paths:
                (root / path).parent.mkdir(parents=True, exist_ok=True)
                (root / path).write_bytes((ROOT / path).read_bytes())
            before = {path: (root / path).read_bytes() for path in paths}
            updated = json.loads(json.dumps(self.manifest))
            updated["dependencies"]["@cratis/components"] = "99.0.1"

            def fail(command, **kwargs):
                (root / LOCKFILE).write_text("partial npm lockfile")
                if command[0] == failing_tool:
                    (root / CATALOG).write_text("partial catalog")
                    raise CommandFailed(subprocess.CalledProcessError(1, command, stderr="ENETUNREACH"))

            with patch.dict(synchronize.__globals__, candidate=Mock(return_value=updated),
                            run=Mock(side_effect=fail)), patch("shutil.which", return_value="npm"):
                with self.assertRaises(CommandFailed):
                    synchronize(root)
            self.assertEqual({path: (root / path).read_bytes() for path in paths}, before)

    def test_rollback_removes_new_outputs(self):
        with tempfile.TemporaryDirectory(prefix="pin-self-test-", dir=ROOT / ".ai-work") as directory:
            root = Path(directory)
            (root / MANIFEST).parent.mkdir(parents=True)
            with self.assertRaises(RuntimeError):
                with pin_transaction(root):
                    (root / LOCKFILE).write_text("new partial lockfile")
                    raise RuntimeError("npm unavailable")
            self.assertFalse((root / LOCKFILE).exists())

    def test_catalog_is_embedded_before_validation(self):
        runner = Mock()
        with patch.dict(regenerate_catalog.__globals__, run=runner):
            regenerate_catalog(ROOT)
        calls = runner.call_args_list
        self.assertEqual(len(calls), 3)
        self.assertIn("FullyQualifiedName~for_FrameworkNameCatalog.when_generating_from_the_restored_scaffold", calls[0].args[0])
        self.assertEqual(calls[0].kwargs["env"]["STAGE_WRITE_FRAMEWORK_NAME_CATALOG"], "1")
        self.assertEqual(calls[1].args[0][:2], ["dotnet", "build"])
        self.assertIn("--no-build", calls[2].args[0])
        self.assertIn("FullyQualifiedName~for_FrameworkNameCatalog", calls[2].args[0])
        for call in calls[1:]:
            self.assertNotIn("STAGE_WRITE_FRAMEWORK_NAME_CATALOG", call.kwargs["env"])

    def test_npm_registry_failure_is_unavailable_not_a_defect(self):
        for command in (["npm", "view", "@cratis/arc"], ["npm", "install"]):
            error = subprocess.CalledProcessError(1, command, stderr="npm error code ENETUNREACH")
            self.assertEqual(CommandFailed(error).code, 2)
        defect = subprocess.CalledProcessError(1, ["npm", "install"], stderr="npm error code ERESOLVE")
        self.assertEqual(CommandFailed(defect).code, 1)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true", help="Plant and detect drift offline; do not synchronize")
    args = parser.parse_args()
    if args.self_test:
        (ROOT / ".ai-work").mkdir(exist_ok=True)
        result = unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(RenderedPinSynchronization))
        print(f"Checked {result.testsRun} synchronization regressions")
        return 0 if result.wasSuccessful() else 1
    try:
        synchronize(ROOT)
    except CommandFailed as error:
        print(f"FAIL: {error}", file=sys.stderr)
        return error.code
    except (OSError, RuntimeError) as error:
        print(f"UNAVAILABLE: {error}", file=sys.stderr)
        return 2
    except (ValueError, ET.ParseError) as error:
        print(f"FAIL: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
