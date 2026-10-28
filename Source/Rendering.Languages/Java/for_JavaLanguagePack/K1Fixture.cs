// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

/// <summary>Loads the frozen K1 plan as declarative language files plus the test facts for its external types.</summary>
public static class K1Fixture
{
    const string ResourcePrefix = "Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.Vectors.k1.";

    /// <summary>Gets the 289 frozen files, in the frozen order.</summary>
    /// <returns>The declarative files.</returns>
    public static ImmutableArray<DeclarativeLanguageFile> Files()
    {
        using var document = JsonDocument.Parse(Read("plan.json"));
        var root = document.RootElement;
        var types = root.GetProperty("types").EnumerateArray().Select(Type).ToArray();

        return [.. root.GetProperty("files").EnumerateArray().Select(file => File(file, types))];
    }

    /// <summary>Gets the test facts: every frozen type the plan does not declare, as an empty member set.</summary>
    /// <remarks>This covers the external libraries and the sibling same-package classes the frozen 289-file selection omits.</remarks>
    /// <returns>The facts.</returns>
    public static JavaTypeFacts Facts()
    {
        using var document = JsonDocument.Parse(Read("plan.json"));
        var root = document.RootElement;
        var types = root.GetProperty("types").EnumerateArray().Select(Type).ToArray();
        var declared = root.GetProperty("files").EnumerateArray().SelectMany(file => file.GetProperty("segments").EnumerateArray())
            .Where(segment => segment[0].GetString() == "d").Select(segment => types[segment[1].GetInt32()]).ToHashSet();
        var undeclared = types.Where(type => !declared.Contains(type)).Select(type => type.Namespace + "." + type.Name.Split('.')[0])
            .Distinct(StringComparer.Ordinal).ToDictionary(name => name, _ => Array.Empty<string>());

        return JavaTypeFacts.Parse(JsonSerializer.Serialize(new { types = undeclared }));
    }

    public static string Read(string name)
    {
        using var stream = typeof(K1Fixture).Assembly.GetManifestResourceStream(ResourcePrefix + name) ?? throw new InvalidOperationException("Missing K1 resource " + name);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    static TemplateSymbol Type(JsonElement element) => new(
        element[0].GetString()!,
        string.Join('.', new[] { element[1].GetString()! }.Concat(element[2].EnumerateArray().Select(item => item.GetString()!))));

    static DeclarativeLanguageFile File(JsonElement file, TemplateSymbol[] types)
    {
        var package = file.GetProperty("package").GetString()!;
        var segments = file.GetProperty("segments").EnumerateArray().ToArray();
        var result = new List<DeclarativeSegment>();
        for (var index = 0; index < segments.Length; index++)
        {
            var kind = segments[index][0].GetString();
            var text = kind == "t" ? segments[index][1].GetString()! : string.Empty;
            if (kind == "t" && index + 1 < segments.Length && segments[index + 1][0].GetString() == "i")
            {
                var packageLine = "package " + package + ";\n";
                if (!text.EndsWith(packageLine, StringComparison.Ordinal)) throw new InvalidOperationException("Missing package line in " + file.GetProperty("id").GetString());
                text = text[..^packageLine.Length];
            }
            else if (kind == "t" && index > 0 && segments[index - 1][0].GetString() == "i")
            {
                if (!text.StartsWith('\n')) throw new InvalidOperationException("Missing blank line after imports in " + file.GetProperty("id").GetString());
                text = text[1..];
            }

            result.Add(kind switch
            {
                "t" => new DeclarativeTextSegment(text),
                "i" => new DeclarativeSymbolSegment(TemplateSymbolKind.Imports, null),

                // Every "n" repeats the declaration of the file's own top-level type (constructors); the frozen plan has no nested types.
                "d" or "n" => new DeclarativeSymbolSegment(TemplateSymbolKind.Declaration, types[segments[index][1].GetInt32()]),
                "r" => new DeclarativeSymbolSegment(TemplateSymbolKind.Reference, types[segments[index][1].GetInt32()]),
                _ => throw new InvalidOperationException("Unknown K1 segment " + kind),
            });
        }

        return new(file.GetProperty("id").GetString()!, [.. result.Where(segment => segment is not DeclarativeTextSegment { Value.Length: 0 })]);
    }
}
