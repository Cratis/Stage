#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Check literal repository Host/client pins, not evaluated external MSBuild inputs. No dependencies."""

import argparse
from pathlib import Path
import re
import sys
import unittest
import xml.etree.ElementTree as ET


PACKAGES = ("Cratis.Chronicle", "Cratis.Chronicle.AspNetCore", "Cratis.Chronicle.Contracts")
ROOT = Path(__file__).resolve().parents[1]


def validate(packages_xml, dockerfile):
    project = ET.fromstring(packages_xml)
    versions = {}
    protected = {name.casefold(): [] for name in PACKAGES}
    for node in project.iter():
        if node.tag.rsplit("}", 1)[-1].casefold() != "packageversion":
            continue
        # Deliberately not an MSBuild evaluator: ambiguous targets anywhere in this
        # file could affect a protected client, so accept only single literal IDs.
        targets = [(key, value) for key, value in node.attrib.items()
                   if key.casefold() in ("include", "update", "remove")]
        if (node.tag != "PackageVersion" or len(targets) != 1
                or targets[0][0] not in ("Include", "Update", "Remove")
                or not re.fullmatch(r"[A-Za-z0-9_.-]+", targets[0][1])):
            raise ValueError("PackageVersion: unsupported targeting form; require one literal package ID")
        operation, target = targets[0]
        if target.casefold() in protected:
            if operation != "Include":
                raise ValueError(f"{target}: protected PackageVersion {operation} is not supported")
            protected[target.casefold()].append(node)
    for name in PACKAGES:
        pins = protected[name.casefold()]
        if (len(pins) != 1 or set(pins[0].attrib) != {"Include", "Version"}
                or pins[0].get("Include") != name or list(pins[0])):
            raise ValueError(f"{name}: require exactly one unconditional literal PackageVersion")
        pin = pins[0]
        # Conditions, Choose/When, targets, and metadata are outside this literal policy.
        for parent in project.iter():
            if parent is pin or pin not in parent.iter():
                continue
            if parent.tag not in ("Project", "ItemGroup") or any(key.casefold() == "condition" for key in parent.attrib):
                raise ValueError(f"{name}: conditional or unsupported parent forms are not supported")
        version = pin.get("Version")
        if not re.fullmatch(r"\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?", version):
            raise ValueError(f"{name}: require an exact version, got {version!r}")
        versions[name] = version
    if len(set(versions.values())) != 1:
        raise ValueError(f"Chronicle client package drift: {versions}")
    version = versions[PACKAGES[0]]
    # DEVELOPMENT is a compiled kernel flavor, not a client prerelease. Stage also needs
    # the development image's shell/apt/tini tools; the bare-version image is chiseled.
    expected = f"cratis/chronicle:{version}-development"
    bases = re.findall(r"^\s*FROM\s+(?:--platform=\S+\s+)?(\S+)", dockerfile, re.I | re.M)
    if not bases or bases[-1] != expected or sum(base.startswith("cratis/chronicle:") for base in bases) != 1:
        raise ValueError(f"Host kernel drift: final FROM must be {expected}; found {bases}")
    return version


class RuntimeCompatibility(unittest.TestCase):
    def setUp(self):
        self.packages = (ROOT / "Directory.Packages.props").read_text()
        self.dockerfile = (ROOT / "Source/Host/Dockerfile").read_text()
        self.version = ET.fromstring(self.packages).find(".//PackageVersion[@Include='Cratis.Chronicle']").get("Version")
        self.image = f"cratis/chronicle:{self.version}-development"
        self.aligned = re.sub(r"cratis/chronicle:\S+", self.image, self.dockerfile)

    def test_matching_exact_pins_pass(self):
        self.assertEqual(validate(self.packages, self.aligned), self.version)

    def test_original_kernel_drift_fails(self):
        with self.assertRaisesRegex(ValueError, "Host kernel drift"):
            validate(self.packages, self.aligned.replace(self.image, "cratis/chronicle:18.1.3-development"))

    def test_same_version_wrong_flavor_fails(self):
        with self.assertRaisesRegex(ValueError, "Host kernel drift"):
            validate(self.packages, self.aligned.replace(self.image, f"cratis/chronicle:{self.version}"))

    def test_each_client_package_drift_fails(self):
        for name in PACKAGES:
            with self.subTest(package=name), self.assertRaisesRegex(ValueError, "client package drift"):
                validate(self.packages.replace(f'Include="{name}" Version="{self.version}"', f'Include="{name}" Version="0.0.1"'), self.aligned)

    def test_floating_or_indirect_image_fails(self):
        for tag in ("latest", "18", "18.3", "${CHRONICLE_VERSION}"):
            with self.subTest(tag=tag), self.assertRaisesRegex(ValueError, "Host kernel drift"):
                validate(self.packages, self.aligned.replace(self.image, f"cratis/chronicle:{tag}"))

    def test_nonliteral_package_versions_fail(self):
        for version in ("18.*", "[18.3.0,19.0.0)", "$(ChronicleVersion)"):
            with self.subTest(version=version), self.assertRaisesRegex(ValueError, "exact version"):
                validate(self.packages.replace(f'Version="{self.version}"', f'Version="{version}"'), self.aligned)

    def test_missing_duplicate_or_conditional_pin_fails(self):
        pin = f'<PackageVersion Include="{PACKAGES[0]}" Version="{self.version}" />'
        for replacement in ("", pin + pin, pin.replace(" />", ' Condition="true" />'), f'<ItemGroup Condition="true">{pin}</ItemGroup>'):
            with self.subTest(replacement=replacement), self.assertRaises(ValueError):
                validate(self.packages.replace(pin, replacement), self.aligned)

    def test_protected_mutations_and_case_duplicates_fail(self):
        for name in PACKAGES:
            for operation in ("Update", "Remove", "Include"):
                for target in (name, name.lower(), name.upper()):
                    extra = f'<PackageVersion {operation}="{target}" Version="18.1.3" />'
                    with self.subTest(extra=extra), self.assertRaises(ValueError):
                        validate(self.packages.replace('</Project>', f'<ItemGroup>{extra}</ItemGroup></Project>'), self.aligned)

    def test_unsupported_package_targets_fail_closed(self):
        for operation in ("Include", "Update", "Remove"):
            for target in ("Cratis.Chronicle;Other", "Other;cratis.chronicle", "Cratis.*", "*", "Cratis.Chronicle?", "$(Client)", "@(Clients)", "%43ratis.Chronicle", " Cratis.Chronicle "):
                extra = f'<PackageVersion {operation}="{target}" Version="18.1.3" />'
                with self.subTest(extra=extra), self.assertRaises(ValueError):
                    validate(self.packages.replace('</Project>', f'<ItemGroup>{extra}</ItemGroup></Project>'), self.aligned)

    def test_noncanonical_or_nested_package_forms_fail(self):
        for extra in ('<packageversion Update="Cratis.Chronicle" Version="18.1.3" />',
                      '<PackageVersion update="Cratis.Chronicle" Version="18.1.3" />',
                      '<PackageVersion><Update>Cratis.Chronicle</Update><Version>18.1.3</Version></PackageVersion>'):
            with self.subTest(extra=extra), self.assertRaises(ValueError):
                validate(self.packages.replace('</Project>', f'<ItemGroup>{extra}</ItemGroup></Project>'), self.aligned)
        pin = f'<PackageVersion Include="{PACKAGES[0]}" Version="{self.version}" />'
        replacement = pin.replace(' />', '><Version>18.1.3</Version></PackageVersion>')
        with self.assertRaises(ValueError):
            validate(self.packages.replace(pin, replacement), self.aligned)

    def test_unrelated_literal_mutations_are_not_protected(self):
        extra = '<ItemGroup><PackageVersion Update="Other.Package" Version="1.0.0" /><PackageVersion Remove="Another.Package" /></ItemGroup>'
        self.assertEqual(validate(self.packages.replace('</Project>', extra + '</Project>'), self.aligned), self.version)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT, help="Repository (or negative-control fixture) root")
    parser.add_argument("--self-test", action="store_true", help="Also exercise positive and drift regressions")
    arguments = parser.parse_args()
    if arguments.self_test:
        result = unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(RuntimeCompatibility))
        if not result.wasSuccessful():
            return 1
    try:
        version = validate((arguments.root / "Directory.Packages.props").read_text(), (arguments.root / "Source/Host/Dockerfile").read_text())
    except (OSError, ET.ParseError, ValueError) as error:
        print(f"FAIL: {error}", file=sys.stderr)
        return 1
    print(f"PASS: repository Host kernel pin {version}-development matches all three declared Chronicle client pins at {version}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
