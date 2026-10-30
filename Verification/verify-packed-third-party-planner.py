#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Verify a third-party planner needs only packed Contracts and published Screenplay."""

import argparse
import base64
import hashlib
import json
import os
import subprocess
import tempfile
from pathlib import Path
from xml.etree import ElementTree
from xml.sax.saxutils import escape
from zipfile import ZipFile


SOURCE = '''module Orders
  feature Ordering
    slice StateChange PlaceOrder
      command PlaceOrder
        id Uuid identifier
        produces OrderPlaced
          for id
          id = id
      event OrderPlaced
        id Uuid
'''

PROGRAM = '''using System;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;

var loaded = await SemanticModelLoader.LoadFromPathAsync(args[0], null, "Canary");
var model = loaded.Model;
var profile = ArtifactRenderProfile.Create("third-party", "1", "ToyPlanner", "1", []);
var request = new ArtifactRenderRequest(
    model,
    loaded.Plan,
    profile,
    new(ArtifactRenderScopeKind.Application, model.Application.Id));
IArtifactRenderPlanner planner = new ToyPlanner();
var first = planner.Plan(request);
var second = planner.Plan(request);
if (!first.Success || !second.Success || first.Artifacts.Length != 1 || second.Artifacts.Length != 1)
{
    throw new Exception("Third-party planning must succeed with exactly one artifact per plan.");
}
if (string.IsNullOrEmpty(first.Digest) || first.Digest != second.Digest)
{
    throw new Exception("Third-party planning produced unequal or empty digests.");
}
Console.WriteLine($"Standalone packed third-party planning succeeded: artifacts={first.Artifacts.Length}; digests equal; digest={first.Digest}");

sealed class ToyPlanner : IArtifactRenderPlanner
{
    public ArtifactRenderPlan Plan(ArtifactRenderRequest request) => ArtifactRenderPlan.Create(
        request,
        [PlannedArtifact.CreateText("Order.cs", "using System;\\n\\npublic record Order(Guid Id);\\n")],
        []);
}
'''


def screenplay_version():
    props = ElementTree.parse(Path(__file__).resolve().parent.parent / "Directory.Packages.props")
    package = props.find(".//PackageVersion[@Include='Cratis.Screenplay']")
    if package is None:
        raise RuntimeError("Directory.Packages.props has no Cratis.Screenplay version")
    version = package.attrib["Version"]
    if version.startswith("$(") and version.endswith(")"):
        version = props.findtext(f".//{version[2:-1]}", default="")
    if not version or "$" in version:
        raise RuntimeError("Cannot resolve the Cratis.Screenplay version in Directory.Packages.props")
    return version


def verify_dependencies(package_file):
    with ZipFile(package_file) as archive:
        nuspec = ElementTree.fromstring(archive.read("Cratis.Stage.Contracts.nuspec"))
    dependencies = {dependency.attrib["id"] for dependency in nuspec.findall(".//{*}dependency")}
    if any(dependency.lower().startswith("cratis.stage.rendering.") for dependency in dependencies):
        raise RuntimeError("Packed Contracts must not depend on Cratis.Stage.Rendering.*")


def write_consumer(root, package_directory, version):
    (root / "Orders.play").write_text(SOURCE, encoding="utf-8")
    (root / "Program.cs").write_text(PROGRAM, encoding="utf-8")
    (root / "Consumer.csproj").write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Cratis.Stage.Contracts" Version="9999.0.0" />
    <PackageReference Include="Cratis.Screenplay" Version="{escape(version)}" />
  </ItemGroup>
</Project>
''', encoding="utf-8")
    (root / "NuGet.Config").write_text(f'''<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear />
<add key="packed" value="{escape(str(package_directory))}" />
<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources>
<packageSourceMapping><clear />
<packageSource key="packed"><package pattern="Cratis.Stage.Contracts" /></packageSource>
<packageSource key="nuget.org"><package pattern="*" /></packageSource>
</packageSourceMapping></configuration>
''', encoding="utf-8")


def verify_restored_package(root, package_file):
    assets = json.loads((root / "obj/project.assets.json").read_text(encoding="utf-8"))
    actual_hash = assets["libraries"]["Cratis.Stage.Contracts/9999.0.0"]["sha512"]
    packed_hash = base64.b64encode(hashlib.sha512(package_file.read_bytes()).digest()).decode("ascii")
    if actual_hash.removeprefix("sha512-") != packed_hash:
        raise RuntimeError("Consumer restored cached Contracts rather than the freshly packed Contracts")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package_directory", type=Path, help="Release NuGet pack output directory")
    args = parser.parse_args()
    package_directory = args.package_directory.resolve()
    package_file = package_directory / "Cratis.Stage.Contracts.9999.0.0.nupkg"
    if not package_file.is_file():
        parser.error(f"Missing packed Contracts: {package_file}")
    verify_dependencies(package_file)
    version = screenplay_version()

    with tempfile.TemporaryDirectory(prefix="stage-packed-third-party-") as folder:
        root = Path(folder)
        write_consumer(root, package_directory, version)
        environment = os.environ.copy()
        environment["NUGET_PACKAGES"] = str(root / "packages")
        subprocess.run(
            ["dotnet", "restore", str(root / "Consumer.csproj"), "--configfile", str(root / "NuGet.Config")],
            check=True, env=environment, timeout=120)
        verify_restored_package(root, package_file)
        subprocess.run(
            ["dotnet", "run", "--no-restore", "--configuration", "Release", "--project", str(root / "Consumer.csproj"), "--", str(root / "Orders.play")],
            check=True, env=environment, timeout=120)


if __name__ == "__main__":
    main()
