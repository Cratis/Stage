#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Check or synchronize literal Host/client pins, not evaluated external MSBuild inputs."""

import argparse
from contextlib import contextmanager
import json
from pathlib import Path
import re
import subprocess
import sys
import unittest
from unittest.mock import Mock, patch
import xml.etree.ElementTree as ET


PACKAGES = ("Cratis.Chronicle", "Cratis.Chronicle.AspNetCore", "Cratis.Chronicle.Contracts")
ROOT = Path(__file__).resolve().parents[1]
VERSION = r"\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?"
FROM = re.compile(r"^[ \t]*FROM\b(?:[ \t]+(?:--platform=\S+[ \t]+)?(\S+))?", re.I | re.M)
MANIFEST_MEDIA_TYPES = (
    "application/vnd.oci.image.index.v1+json",
    "application/vnd.docker.distribution.manifest.list.v2+json",
    "application/vnd.oci.image.manifest.v1+json",
    "application/vnd.docker.distribution.manifest.v2+json",
)


def client_version(packages_xml):
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
        if not re.fullmatch(VERSION, version):
            raise ValueError(f"{name}: require an exact version, got {version!r}")
        versions[name] = version
    if len(set(versions.values())) != 1:
        raise ValueError(f"Chronicle client package drift: {versions}")
    return versions[PACKAGES[0]]


def expected_image(version):
    # DEVELOPMENT is a compiled kernel flavor, not a client prerelease. Stage also needs
    # the development image's shell/apt/tini tools; the bare-version image is chiseled.
    return f"cratis/chronicle:{version}-development"


def host_base(dockerfile):
    # This literal policy does not parse heredocs or non-default escape directives.
    # Never mistake embedded Dockerfile text or a continued shell line for a stage.
    continued = False
    for line in dockerfile.splitlines():
        stripped = line.strip()
        escape = re.fullmatch(r"#\s*escape\s*=\s*(.+)", stripped, re.I)
        if escape and escape.group(1) != "\\":
            raise ValueError("Host kernel drift: unsupported Dockerfile escape directive")
        if not stripped or stripped.startswith("#"):
            continue
        if "<<" in line or (continued and FROM.match(line)):
            raise ValueError("Host kernel drift: unsupported heredoc or continued FROM text")
        continued = line.rstrip().endswith("\\")
    bases = list(FROM.finditer(dockerfile))
    if (not bases or any(base.group(1) is None for base in bases)
            or not bases[-1].group(1).startswith("cratis/chronicle:")
            or sum(base.group(1).startswith("cratis/chronicle:") for base in bases) != 1):
        raise ValueError("Host kernel drift: require exactly one Chronicle FROM, in the final stage")
    base = bases[-1]
    # Only a literal version token on a complete FROM instruction can be rewritten.
    # Keep platform/alias spelling, whitespace, comments elsewhere and line endings intact.
    line_end = dockerfile.find("\n", base.end(1))
    suffix = dockerfile[base.end(1):line_end if line_end >= 0 else len(dockerfile)]
    if (not re.fullmatch(r"cratis/chronicle:" + VERSION, base.group(1))
            or not re.fullmatch(r"[ \t]*(?:AS[ \t]+[A-Za-z0-9_.-]+[ \t]*)?\r?", suffix, re.I)):
        raise ValueError("Host kernel drift: require a literal version on a complete final FROM")
    return base


def validate(packages_xml, dockerfile):
    version = client_version(packages_xml)
    expected = expected_image(version)
    actual = host_base(dockerfile).group(1)
    if actual != expected:
        raise ValueError(f"Host kernel drift: final FROM must be {expected}; found {actual}")
    return version


def public_image_digest(image):
    """Resolve Docker Hub metadata anonymously; never invoke Docker or read its config."""
    if not re.fullmatch(r"cratis/chronicle:" + VERSION + r"-development", image):
        raise ValueError(f"Unsupported image: {image}")

    def request(url, *options):
        # -q must be first: ignore user curlrc (including credentials and redirects).
        # Two requests, no retries/redirects, each capped at 15s plus a 20s process cap.
        try:
            result = subprocess.run(
                ["curl", "-q", "--silent", "--show-error", "--fail",
                 "--connect-timeout", "5", "--max-time", "15", *options, url],
                capture_output=True, text=True, timeout=20, check=True)
            return result.stdout
        except (OSError, subprocess.SubprocessError) as error:
            # Do not surface a command containing the anonymous bearer token.
            raise ValueError(f"Public image unavailable or registry request timed out: {image}") from error

    try:
        token = json.loads(request(
            "https://auth.docker.io/token?service=registry.docker.io&scope=repository:cratis/chronicle:pull"))["token"]
        if not isinstance(token, str) or not re.fullmatch(r"[A-Za-z0-9_.-]+", token):
            raise ValueError("Invalid anonymous registry token")
        headers = request(
            "https://registry-1.docker.io/v2/cratis/chronicle/manifests/" + image.split(":", 1)[1],
            "--head", "--header", f"Authorization: Bearer {token}",
            "--header", "Accept: " + ", ".join(MANIFEST_MEDIA_TYPES))
        # A proxy CONNECT or interim response must not supply the manifest headers.
        final_headers = re.split(r"\r?\n\r?\n", headers.strip())[-1]
        status = re.match(r"HTTP/\S+ (\d+)\b", final_headers)
        digests = re.findall(r"^docker-content-digest:[ \t]*(sha256:[0-9a-f]{64})[ \t]*\r?$", final_headers, re.I | re.M)
        content_types = re.findall(r"^content-type:[ \t]*([^;\r\n]+)(?:;[^\r\n]*)?\r?$", final_headers, re.I | re.M)
        if (not status or status.group(1) != "200" or len(digests) != 1
                or len(content_types) != 1 or content_types[0].strip().lower() not in MANIFEST_MEDIA_TYPES):
            raise ValueError("Registry did not return one supported manifest type and resolved digest")
        return digests[0]
    except (KeyError, TypeError, ValueError) as error:
        raise ValueError(f"Cannot resolve public image {image}: {error}") from error


def synchronize(root, resolver=public_image_digest):
    """Change only the final Host image token, after resolution and candidate validation."""
    packages_path = root / "Directory.Packages.props"
    host_path = root / "Source/Host/Dockerfile"
    packages = packages_path.read_bytes()
    original = host_path.read_bytes()
    packages_xml = packages.decode("utf-8")
    dockerfile = original.decode("utf-8")
    version = client_version(packages_xml)
    image = expected_image(version)
    base = host_base(dockerfile)
    candidate = dockerfile[:base.start(1)] + image + dockerfile[base.end(1):]
    validate(packages_xml, candidate)
    digest = resolver(image)
    if not digest:
        raise ValueError(f"Public image did not resolve: {image}")
    # Refuse to overwrite input changes made while the registry request was in flight.
    if packages_path.read_bytes() != packages or host_path.read_bytes() != original:
        raise ValueError("Repository pins changed during synchronization; retry")
    updated = candidate.encode("utf-8")
    if updated != original:
        host_path.write_bytes(updated)
    # Read back both inputs and run the same guard; a failed guard must block success.
    validate(packages_path.read_bytes().decode("utf-8"), host_path.read_bytes().decode("utf-8"))
    return version, digest


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

    @contextmanager
    def sync_fixture(self, packages=None, dockerfile=None):
        # In-memory paths: regressions need neither network nor temporary files.
        root = Path("/offline-stage")
        host = root / "Source/Host/Dockerfile"
        files = {
            root / "Directory.Packages.props": (self.packages if packages is None else packages).encode(),
            host: (self.aligned.replace(self.image, "cratis/chronicle:0.0.1-development")
                   if dockerfile is None else dockerfile).encode(),
            root / "Source/Rendering.Cratis/profile": b"18.2.0-development\r\n",
            root / "Source/Other/Dockerfile": b"FROM cratis/chronicle:18.2.0-development\n",
        }
        before = files.copy()

        def write(path, content):
            files[path] = content
            return len(content)

        with patch.object(Path, "read_bytes", autospec=True, side_effect=files.__getitem__), \
                patch.object(Path, "write_bytes", autospec=True, side_effect=write) as writes:
            yield root, host, files, before, writes

    def test_sync_repairs_only_the_intended_host_token(self):
        drift = self.aligned.replace(self.image, "cratis/chronicle:19.1.5-development")
        resolver = Mock(return_value="resolved-digest")
        with self.sync_fixture(dockerfile=drift) as (root, host, files, before, writes):
            self.assertEqual(synchronize(root, resolver), (self.version, "resolved-digest"))
            expected = before.copy()
            expected[host] = self.aligned.encode()
            self.assertEqual(files, expected)
            writes.assert_called_once_with(host, self.aligned.encode())
            resolver.assert_called_once_with(self.image)

    def test_sync_is_idempotent_but_still_resolves(self):
        resolver = Mock(return_value="resolved-digest")
        with self.sync_fixture(dockerfile=self.aligned) as (root, _, files, before, writes):
            synchronize(root, resolver)
            synchronize(root, resolver)
            self.assertEqual(files, before)
            writes.assert_not_called()
            self.assertEqual(resolver.call_count, 2)

    def test_sync_preserves_newlines_alias_platform_and_other_references(self):
        for newline in ("\n", "\r\n"):
            for trailing in ("", newline):
                with self.subTest(newline=newline, trailing=trailing):
                    old_image = "cratis/chronicle:19.1.5-development"
                    lines = ["# Unicode: café; " + old_image,
                             "FROM node:24-alpine AS frontend",
                             "\tfRoM --platform=$TARGETPLATFORM " + old_image + " AS runtime  ",
                             "RUN printf '" + old_image + "'"]
                    dockerfile = newline.join(lines) + trailing
                    expected = newline.join([*lines[:2], lines[2].replace(old_image, self.image), lines[3]]) + trailing
                    with self.sync_fixture(dockerfile=dockerfile) as (root, host, files, _, writes):
                        synchronize(root, lambda _: "resolved-digest")
                        self.assertEqual(files[host], expected.encode())
                        writes.assert_called_once()

    def test_sync_invalid_packages_refuse_without_resolution_or_writes(self):
        pin = f'<PackageVersion Include="{PACKAGES[0]}" Version="{self.version}" />'
        invalid = ["<Project>", self.packages.replace(pin, ""), self.packages.replace(pin, pin + pin)]
        invalid += [self.packages.replace(pin, pin.replace(self.version, version))
                    for version in ("0.0.1", "$(Version)", "19.*", "[19.0.0,20.0.0)")]
        invalid += [self.packages.replace(pin, replacement) for replacement in (
            pin.replace(" />", ' Condition="true" />'),
            '<ItemGroup Condition="true">' + pin + '</ItemGroup>',
            pin.replace('Include=', 'Update='),
            pin.replace(PACKAGES[0], "$(Client)"))]
        for packages in invalid:
            with self.subTest(packages=packages), self.sync_fixture(packages=packages) as (root, _, files, before, writes):
                resolver = Mock()
                with self.assertRaises((ValueError, ET.ParseError)):
                    synchronize(root, resolver)
                resolver.assert_not_called()
                writes.assert_not_called()
                self.assertEqual(files, before)

    def test_sync_malformed_or_indirect_host_refuses_without_writes(self):
        invalid = [self.aligned.replace(self.image, value) for value in (
            "${KERNEL_IMAGE}", "cratis/chronicle:${VERSION}", "cratis/chronicle:latest",
            "cratis/chronicle:19", self.image + "@sha256:abc", self.image + " extra",
            self.image + " \\\nRUN true", "")]
        invalid += [self.aligned + "\nFROM node:24\n", self.aligned + "\nFROM " + self.image + "\n",
                    "FROM\n" + self.image, "# FROM " + self.image,
                    self.aligned + "\nFROM\nnode:24", self.aligned + "\nFROM",
                    "FROM node:24\nCOPY <<EOF /tmp/Dockerfile\nFROM " + self.image + "\nEOF\n",
                    "FROM node:24\nRUN <<'EOF'\nFROM " + self.image + "\nEOF\n",
                    "FROM node:24\nRUN echo \\\nFROM " + self.image + "\n",
                    "# escape=`\nFROM node:24\nRUN echo `\nFROM " + self.image + "\n"]
        for dockerfile in invalid:
            with self.subTest(dockerfile=dockerfile), self.sync_fixture(dockerfile=dockerfile) as (root, _, files, before, writes):
                resolver = Mock()
                with self.assertRaises(ValueError):
                    synchronize(root, resolver)
                resolver.assert_not_called()
                writes.assert_not_called()
                self.assertEqual(files, before)

    def test_sync_unavailable_image_or_timeout_refuses_without_writes(self):
        for resolver in (Mock(return_value=None), Mock(return_value=False),
                         Mock(side_effect=ValueError("unavailable")), Mock(side_effect=TimeoutError("timeout"))):
            with self.subTest(resolver=resolver), self.sync_fixture() as (root, _, files, before, writes):
                with self.assertRaises((ValueError, TimeoutError)):
                    synchronize(root, resolver)
                writes.assert_not_called()
                self.assertEqual(files, before)

    def test_sync_candidate_guard_failure_prevents_resolution_and_writes(self):
        with self.sync_fixture() as (root, _, files, before, writes), \
                patch(__name__ + ".validate", side_effect=ValueError("candidate guard")):
            resolver = Mock()
            with self.assertRaisesRegex(ValueError, "candidate guard"):
                synchronize(root, resolver)
            resolver.assert_not_called()
            writes.assert_not_called()
            self.assertEqual(files, before)

    def test_sync_post_write_guard_failure_blocks_cli_success(self):
        drift = self.aligned.replace(self.image, "cratis/chronicle:0.0.1-development")
        real_synchronize = synchronize
        with self.sync_fixture(dockerfile=drift) as (root, _, _, _, writes), \
                patch(__name__ + ".validate", side_effect=[self.version, ValueError("post-sync guard")]), \
                patch(__name__ + ".synchronize", side_effect=lambda path: real_synchronize(path, lambda _: "resolved-digest")), \
                patch.object(sys, "argv", ["verify-runtime-compatibility.py", "--root", str(root), "--synchronize"]), \
                patch("builtins.print") as output:
            self.assertEqual(main(), 1)
            writes.assert_called_once()
            self.assertFalse(any("PASS:" in str(call) or "RESOLVED:" in str(call) for call in output.call_args_list))

    def test_sync_readback_guard_detects_corrupted_write(self):
        with self.sync_fixture() as (root, _, files, _, writes):
            def corrupt(path, content):
                files[path] = content.replace(self.image.encode(), b"cratis/chronicle:0.0.2-development")
                return len(content)
            writes.side_effect = corrupt
            with self.assertRaisesRegex(ValueError, "Host kernel drift"):
                synchronize(root, lambda _: "resolved-digest")
            writes.assert_called_once()

    def test_sync_concurrent_input_change_refuses_to_overwrite(self):
        with self.sync_fixture() as (root, host, files, _, writes):
            def resolver(_):
                files[host] += b"# concurrent change\n"
                return "resolved-digest"
            with self.assertRaisesRegex(ValueError, "changed during synchronization"):
                synchronize(root, resolver)
            writes.assert_not_called()

    def test_public_resolver_uses_only_anonymous_bounded_metadata_requests(self):
        digest = "sha256:" + "a" * 64
        responses = [Mock(stdout='{"token":"anonymous.token"}'),
                     Mock(stdout="HTTP/1.1 200 Connection established\r\nContent-Type: text/html\r\n\r\n"
                          "HTTP/2 200\r\nContent-Type: " + MANIFEST_MEDIA_TYPES[0] + "\r\nDocker-Content-Digest: " + digest + "\r\n")]
        with patch.object(subprocess, "run", side_effect=responses) as run:
            self.assertEqual(public_image_digest(self.image), digest)
        self.assertEqual(run.call_count, 2)
        for call in run.call_args_list:
            command = call.args[0]
            self.assertEqual(command[:2], ["curl", "-q"])
            self.assertIn("--max-time", command)
            self.assertEqual(command[command.index("--max-time") + 1], "15")
            self.assertEqual(call.kwargs["timeout"], 20)
            self.assertTrue(call.kwargs["check"])
            self.assertNotIn("--location", command)
        self.assertIn("--head", run.call_args.args[0])
        self.assertTrue(run.call_args.args[0][-1].endswith("/manifests/" + self.image.split(":")[1]))

    def test_public_resolver_fails_closed(self):
        errors = [FileNotFoundError("curl"), subprocess.TimeoutExpired("curl", 20),
                  subprocess.CalledProcessError(22, "curl")]
        for error in errors:
            with self.subTest(error=error), patch.object(subprocess, "run", side_effect=error):
                with self.assertRaises(ValueError):
                    public_image_digest(self.image)
        digest_header = "Docker-Content-Digest: sha256:" + "a" * 64 + "\r\n"
        valid = "HTTP/2 200\r\nContent-Type: " + MANIFEST_MEDIA_TYPES[0] + "\r\n" + digest_header
        for headers in ("HTTP/2 404\r\n", "HTTP/2 200\r\n", "HTTP/2 302\r\n" + digest_header,
                        "HTTP/2 200\r\n" + digest_header,
                        "HTTP/2 200\r\nContent-Type: text/html\r\n" + digest_header,
                        valid + "Content-Type: text/html\r\n",
                        valid + "\r\nHTTP/2 200\r\nContent-Type: text/html\r\n",
                        valid + "\r\nHTTP/2 404\r\n"):
            with self.subTest(headers=headers), patch.object(subprocess, "run", side_effect=[
                    Mock(stdout='{"token":"anonymous.token"}'), Mock(stdout=headers)]):
                with self.assertRaises(ValueError):
                    public_image_digest(self.image)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT, help="Repository (or negative-control fixture) root")
    parser.add_argument("--self-test", action="store_true", help="Also exercise positive and drift regressions offline")
    parser.add_argument("--synchronize", action="store_true", help="Resolve the public image, synchronize only the Host pin, then validate (requires curl)")
    arguments = parser.parse_args()
    if arguments.self_test:
        result = unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(RuntimeCompatibility))
        if not result.wasSuccessful():
            return 1
    try:
        if arguments.synchronize:
            version, digest = synchronize(arguments.root)
            print(f"RESOLVED: {expected_image(version)} @ {digest}")
        else:
            version = validate((arguments.root / "Directory.Packages.props").read_text(), (arguments.root / "Source/Host/Dockerfile").read_text())
    except (OSError, ET.ParseError, ValueError) as error:
        print(f"FAIL: {error}", file=sys.stderr)
        return 1
    print(f"PASS: repository Host kernel pin {version}-development matches all three declared Chronicle client pins at {version}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
