#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Verify a packed renderer can plan a reducer in a standalone NuGet consumer."""

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
        amount Decimal
        produces OrderPlaced
          for id
          id = id
          amount = amount
      event OrderPlaced
        id Uuid
        amount Decimal
    slice StateView Totals
      readmodel Total
        record Uuid
        amount Decimal
      query ById => Total?
        by record Uuid
      reducer Fold => Total
        on OrderPlaced
          ```csharp
          return new Total(Record: context.Event.Id, Amount: Math.Abs(context.Event.Amount));
          ```
'''

PROGRAM = '''using System;
using System.Linq;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis;

var resources = typeof(CratisArtifactRenderPlanner).Assembly.GetManifestResourceNames();
if (!resources.Contains("PureTransitionReferences.10.0.12.System.IO.dll", StringComparer.Ordinal))
{
    throw new Exception("The packed renderer lost an audited reference assembly.");
}

var loaded = await SemanticModelLoader.LoadFromPathAsync(args[0], null, "Projects");
var model = loaded.Model;
var request = new ArtifactRenderRequest(
    model,
    loaded.Plan,
    CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
    new(ArtifactRenderScopeKind.Application, model.Application.Id))
{
    ImplementationRequirements = loaded.ImplementationRequirements,
    ImplementationContents = loaded.ImplementationContents,
    TypedContextDescriptors = loaded.TypedContextDescriptors
};
var plan = new CratisArtifactRenderPlanner().Plan(request);
if (!plan.Success || !plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal)) ||
    !plan.Artifacts.Any(artifact => System.Text.Encoding.UTF8.GetString(artifact.Bytes.ToArray()).Contains("@record", StringComparison.Ordinal)))
{
    throw new Exception("Standalone reducer planning failed: " + string.Join("; ", plan.Diagnostics));
}
Console.WriteLine("Standalone packed reducer planning succeeded.");
'''


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package_directory", type=Path, help="Release NuGet pack output directory")
    args = parser.parse_args()
    package_directory = args.package_directory.resolve()
    package_file = package_directory / "Cratis.Stage.Rendering.Cratis.9999.0.0.nupkg"
    if not package_file.is_file():
        parser.error(f"Missing packed renderer: {package_file}")
    with ZipFile(package_file) as archive:
        nuspec = ElementTree.fromstring(archive.read("Cratis.Stage.Rendering.Cratis.nuspec"))
        dependencies = {dependency.attrib["id"] for dependency in nuspec.findall(".//{*}dependency")}
        if any("/IO/" in filename for filename in archive.namelist()):
            parser.error("An audited reference assembly became a spurious IO satellite")
    if "Microsoft.CodeAnalysis.CSharp" not in dependencies or "Microsoft.CodeAnalysis.Analyzers" in dependencies:
        parser.error("Packed renderer must expose Roslyn at runtime, but keep analyzers private")

    with tempfile.TemporaryDirectory(prefix="stage-packed-reducer-") as folder:
        root = Path(folder)
        (root / "Orders.play").write_text(SOURCE, encoding="utf-8")
        (root / "Program.cs").write_text(PROGRAM, encoding="utf-8")
        (root / "Consumer.csproj").write_text('''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><PackageReference Include="Cratis.Stage.Rendering.Cratis" Version="9999.0.0" /></ItemGroup>
</Project>
''', encoding="utf-8")
        (root / "NuGet.Config").write_text(f'''<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear />
<add key="packed" value="{escape(str(package_directory))}" />
<add key="cached" value="{escape(str(Path.home() / '.nuget/packages'))}" />
<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources>
<packageSourceMapping>
<packageSource key="packed"><package pattern="Cratis.Stage.Rendering.Cratis" /><package pattern="Cratis.Stage.Contracts" /></packageSource>
<packageSource key="cached"><package pattern="*" /></packageSource>
<packageSource key="nuget.org"><package pattern="*" /></packageSource>
</packageSourceMapping></configuration>
''', encoding="utf-8")
        environment = os.environ.copy()
        environment["NUGET_PACKAGES"] = str(root / "packages")
        subprocess.run(["dotnet", "restore", str(root / "Consumer.csproj"), "--configfile", str(root / "NuGet.Config")], check=True, env=environment)
        assets = json.loads((root / "obj/project.assets.json").read_text(encoding="utf-8"))
        actual_hash = assets["libraries"]["Cratis.Stage.Rendering.Cratis/9999.0.0"]["sha512"]
        packed_hash = base64.b64encode(hashlib.sha512(package_file.read_bytes()).digest()).decode("ascii")
        if actual_hash.removeprefix("sha512-") != packed_hash:
            raise RuntimeError("Consumer restored a cached renderer rather than the freshly packed renderer")
        subprocess.run(["dotnet", "run", "--no-restore", "--configuration", "Release", "--project", str(root / "Consumer.csproj"), "--", str(root)], check=True, env=environment)


if __name__ == "__main__":
    main()
