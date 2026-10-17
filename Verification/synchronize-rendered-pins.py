#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Synchronize emitted npm pins with published releases and Stage's central Arc pin."""

import argparse
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import unittest
from unittest.mock import Mock
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
PROFILE = Path("Source/Rendering.Cratis/Scaffolding/CratisFrontendPackageSet.cs")
MANIFEST = Path("Source/Frontend/package.json")
STABLE = re.compile(r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\Z")
SCENE = ("blueprint.default", "components", "engine", "model", "primereact", "react")
REQUIRED = {"@cratis/arc", "@cratis/arc.react", "@cratis/components", "@cratis/fundamentals",
            *("@cratis/scene." + name for name in SCENE)}
CURRENT = re.compile(r"(internal static CratisFrontendPackageSet Current \{ get; \} = new\()(.*?)(\);)", re.S)


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
            if name.startswith("@cratis/"):
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
        super().__init__(f"Command failed: {error}; {error.stderr or ''}")
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
    return stable(json.loads(result.stdout))


def candidate(manifest, arc, lookup=published):
    # Work on an independent document; resolve every candidate before writing anything.
    updated = json.loads(json.dumps(manifest))
    for name in cratis_pins(manifest):
        version = lookup(name, arc) if name == "@cratis/arc" or name.startswith("@cratis/arc.") else lookup(name)
        for section in ("dependencies", "devDependencies"):
            if name in updated.get(section, {}):
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
    versions = list(re.finditer(r'"([0-9]+\.[0-9]+\.[0-9]+)"', match[2]))
    if len(versions) != 21:
        raise ValueError(f"{PROFILE}: expected 21 exact package arguments, found {len(versions)}")
    replacements = {0: pins["@cratis/arc"], 1: pins["@cratis/arc.react"],
                    2: pins.get("@cratis/arc.vite", pins["@cratis/arc"]),
                    3: pins["@cratis/fundamentals"], 15: pins["@cratis/components"],
                    16: pins["@cratis/scene.model"]}
    body = match[2]
    for index, version in sorted(replacements.items(), reverse=True):
        token = versions[index]
        body = body[:token.start(1)] + version + body[token.end(1):]
    return source[:match.start(2)] + body + source[match.end(2):]


def synchronize(root):
    if not shutil.which("npm"):
        raise RuntimeError("npm is unavailable; install Node/npm before synchronizing rendered pins")
    packages = root / "Directory.Packages.props"
    manifest_path, profile_path = root / MANIFEST, root / PROFILE
    originals = {path: path.read_bytes() for path in (packages, manifest_path, profile_path)}
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
    manifest_path.write_text(content)
    profile_path.write_text(profile)
    run(["npm", "install", "--package-lock-only", "--prefix", "Source/Frontend"], cwd=root)
    run(["dotnet", "test", "Source/Rendering.Cratis/Rendering.Cratis.csproj", "--configuration", "Debug",
         "--filter", "FullyQualifiedName~for_FrameworkNameCatalog"], cwd=root,
        env={**os.environ, "STAGE_WRITE_FRAMEWORK_NAME_CATALOG": "1"})
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

    def test_detects_planted_arc_drift(self):
        self.manifest["dependencies"]["@cratis/arc"] = "0.0.1"
        with self.assertRaisesRegex(ValueError, "npm Arc != NuGet Arc"):
            validate(self.manifest, self.arc)

    def test_repairs_drift_using_exact_arc_release(self):
        self.manifest["dependencies"]["@cratis/arc"] = "0.0.1"
        lookup = Mock(side_effect=lambda name, version=None: version or cratis_pins(self.manifest)[name])
        result = candidate(self.manifest, self.arc, lookup)
        self.assertEqual(validate(result, self.arc)["@cratis/arc"], self.arc)
        self.assertEqual(self.manifest["dependencies"]["@cratis/arc"], "0.0.1")
        self.assertIn(unittest.mock.call("@cratis/arc.react", self.arc), lookup.call_args_list)

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
        self.assertEqual(len(re.findall(r'"[0-9]+\.[0-9]+\.[0-9]+"', CURRENT.search(updated)[2])), 21)

    def test_unavailable_registry_does_not_mutate_manifest(self):
        original = json.dumps(self.manifest)
        with self.assertRaises(RuntimeError):
            candidate(self.manifest, self.arc, Mock(side_effect=RuntimeError("npm unavailable")))
        self.assertEqual(json.dumps(self.manifest), original)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true", help="Plant and detect drift offline; do not synchronize")
    args = parser.parse_args()
    if args.self_test:
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
