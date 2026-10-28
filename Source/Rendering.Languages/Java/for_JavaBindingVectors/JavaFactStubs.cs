// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaBindingVectors;

public static class JavaFactStubs
{
    const string Header = "// Copyright (c) Cratis. All rights reserved.\n// Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\n";

    public static IEnumerable<(string Path, string Text)> Generate(string factsJson)
    {
        using var facts = JsonDocument.Parse(factsJson);
        foreach (var type in facts.RootElement.GetProperty("types").EnumerateObject())
        {
            if (type.Name.StartsWith("java.", StringComparison.Ordinal) || type.Name.StartsWith("javax.", StringComparison.Ordinal)) continue;
            var dot = type.Name.LastIndexOf('.');
            if (dot < 1) throw new InvalidOperationException("Fact type has no package: " + type.Name);
            var package = type.Name[..dot];
            var name = type.Name[(dot + 1)..];
            var members = type.Value.EnumerateArray().Select(member => member.GetString()).ToArray();
            if (members.Contains(name)) throw new InvalidOperationException("Fact member equals its type: " + type.Name);
            var body = string.Concat(members.Select(member => "  public static class " + member + " {}\n  public static int " + member + ";\n"));
            yield return (package.Replace('.', '/') + "/" + name + ".java", Header + "package " + package + ";\n\npublic class " + name + " {\n  public " + name + "() {}\n" + body + "}\n");
        }
    }
}
