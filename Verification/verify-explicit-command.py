#!/usr/bin/env python3
"""Compare Stage's emitted scene to the C# source and native Debug Arc command proxy.

Usage: python3 Verification/verify-explicit-command.py PATH_TO_EMITTED_DEBUG_APP
The app must have been built with `dotnet build Projects.csproj -c Debug` first.
"""
import json
import re
import sys
from pathlib import Path


def verify(root: Path) -> None:
    scene = json.loads((root / "scene.json").read_text())
    elements = [element for screen in scene["screens"] for slot in screen["slotContent"].values() for element in slot]
    forms = [element for element in elements if element["componentName"] == "Cratis.Components:commandForm"]
    assert len(forms) == 1, "Expected one command form"
    props = forms[0]["properties"]
    assert props["command"] == "RegisterProject" and props["submitLabel"] == "Submit"
    inputs = props["inputs"]
    assert inputs and len(inputs) == len({entry["property"] for entry in inputs}), "Missing or duplicate inputs"
    for entry in inputs:
        assert set(entry) == {"property", "type", "label"}, f"Invalid Scene input shape: {entry}"
        assert entry["type"] in ("string", "guid") and entry["label"]

    folder = root / "Projects/Registration/RegisterProject"
    csharp = (folder / "RegisterProject.cs").read_text()
    match = re.search(r"public record RegisterProject\(([^)]+)\)", csharp)
    assert match, "Missing C# command declaration"
    members = dict(re.fullmatch(r"(\w+) (\w+)", part.strip()).groups()[::-1] for part in match[1].split(","))
    clr = {}
    for member, concept in members.items():
        concept_file = root / "Common" / f"{concept}.cs"
        declaration = concept_file.read_text()
        underlying = re.search(rf"public record {concept}\((Guid|string) Value\)", declaration)
        assert underlying, f"Unrecognized native concept: {concept}"
        clr[member[0].lower() + member[1:]] = "Guid" if underlying[1] == "Guid" else "String"

    proxy = (folder / "RegisterProject.ts").read_text()
    descriptor_block = re.search(r"readonly propertyDescriptors: PropertyDescriptor\[\] = \[(.*?)\];", proxy, re.S)
    assert descriptor_block, "No Arc property descriptors (was the app built in Debug?)"
    native = re.findall(r"new PropertyDescriptor\('([^']+)', (\w+), (true|false)\)", descriptor_block[1])
    assert len(native) == len([line for line in descriptor_block[1].splitlines() if "new PropertyDescriptor" in line]), "Unrecognized Arc descriptor"
    assert len(native) == len(set(name for name, _, _ in native)), "Colliding Arc descriptors"
    assert dict((name, kind) for name, kind, _ in native) == clr, "C# members disagree with native Arc descriptors"
    assert all(optional == "false" for _, _, optional in native), "Canonical C# command has required members"
    actual = {entry["property"]: "Guid" if entry["type"] == "guid" else "String" for entry in inputs}
    assert actual == dict((name, kind) for name, kind, _ in native), "Scene inputs differ from native Arc type/name/required descriptors"
    assert {entry["property"]: entry["label"] for entry in inputs} == {"projectId": "Project ID", "name": "Name"}
    print("PASS: scene inputs match C# command/concepts and native Debug Arc descriptors")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit("Usage: verify-explicit-command.py PATH_TO_EMITTED_DEBUG_APP")
    verify(Path(sys.argv[1]))
