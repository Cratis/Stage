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
CATALOG = Path("Source/Rendering.Cratis/Naming/Imports/FrameworkNames.catalog")
PROJECT = "Source/Rendering.Cratis/Rendering.Cratis.csproj"
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


@contextmanager
def pin_transaction(root):
    # npm and the catalog generator write in place. Roll back the complete bounded
    # output set on failure, including partial writes and newly created files.
    originals = {root / path: (root / path).read_bytes() if (root / path).exists() else None
                 for path in (MANIFEST, LOCKFILE, PROFILE, CATALOG)}
    try:
        yield
    except BaseException:
        for path, content in originals.items():
            if content is None:
                path.unlink(missing_ok=True)
            elif not path.exists() or path.read_bytes() != content:
                path.write_bytes(content)
        raise


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
    with pin_transaction(root):
        manifest_path.write_text(content)
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

    def test_npm_failure_restores_every_output(self):
        self.assert_failed_transaction("npm")

    def test_catalog_failure_restores_every_output(self):
        self.assert_failed_transaction("dotnet")

    def assert_failed_transaction(self, failing_tool):
        with tempfile.TemporaryDirectory(prefix="pin-self-test-", dir=ROOT / ".ai-work") as directory:
            root = Path(directory)
            paths = (Path("Directory.Packages.props"), MANIFEST, LOCKFILE, PROFILE, CATALOG)
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
