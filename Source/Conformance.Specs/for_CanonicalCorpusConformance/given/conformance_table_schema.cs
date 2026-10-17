// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

public static class conformance_table_schema
{
    public static expected_conformance_table Load(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var problems = new List<string>();
            var root = document.RootElement;
            Fields(root, "matrix", ["corpusVersion", "minimumVectorCount", "excludedTypes", "vectors"], problems);
            var vectors = new Dictionary<string, IReadOnlyDictionary<conformance_surface, expected_conformance_cell>>(StringComparer.Ordinal);
            foreach (var vector in root.GetProperty("vectors").EnumerateObject())
            {
                var cells = new Dictionary<conformance_surface, expected_conformance_cell>();
                foreach (var surface in vector.Value.EnumerateObject())
                {
                    var path = $"{vector.Name} / {surface.Name}";
                    if (!Enum.TryParse<conformance_surface>(surface.Name, true, out var key) || !Enum.IsDefined(key))
                    {
                        problems.Add($"{path}: unknown surface");
                        continue;
                    }

                    Fields(surface.Value, path, ["outcome", "refusals"], problems);
                    var records = new List<refusal_record>();
                    if (surface.Value.TryGetProperty("refusals", out var refusals))
                    {
                        foreach (var entry in refusals.EnumerateArray())
                        {
                            Fields(entry, path, ["code", "kind", "artifact", "issue", "byDesign"], problems);
                            var code = entry.GetProperty("code").GetString()!;
                            var kind = OptionalText(entry, "kind");
                            var artifact = OptionalText(entry, "artifact") ?? string.Empty;
                            int? issue = null;
                            if (entry.TryGetProperty("issue", out var number) && number.ValueKind != JsonValueKind.Null)
                            {
                                if (number.ValueKind == JsonValueKind.Number && number.TryGetInt32(out var integer))
                                {
                                    issue = integer;
                                }
                                else
                                {
                                    problems.Add($"{path} / {code}@{artifact}: issue must be an integer or null");
                                }
                            }

                            records.Add(new(new(code, kind, artifact), issue, OptionalText(entry, "byDesign")));
                        }
                    }

                    if (!cells.TryAdd(key, new(surface.Value.GetProperty("outcome").GetString()!, records)))
                    {
                        problems.Add($"{path}: duplicate surface");
                    }
                }

                vectors.Add(vector.Name, cells);
            }

            return new()
            {
                CorpusVersion = root.GetProperty("corpusVersion").GetString()!,
                MinimumVectorCount = root.GetProperty("minimumVectorCount").GetInt32(),
                ExcludedTypes = root.GetProperty("excludedTypes").EnumerateObject().ToDictionary(property => property.Name, property => property.Value.GetString()!, StringComparer.Ordinal),
                Vectors = vectors,
                SchemaProblems = problems
            };
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or ArgumentException)
        {
            return new() { SchemaProblems = [$"invalid conformance matrix schema: {error.Message}"] };
        }
    }

    static string? OptionalText(JsonElement entry, string name) => entry.TryGetProperty(name, out var value) ? value.GetString() : null;

    static void Fields(JsonElement element, string path, IReadOnlyList<string> allowed, List<string> problems)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                problems.Add($"{path}: duplicate field '{property.Name}'");
            }

            if (!allowed.Contains(property.Name, StringComparer.Ordinal))
            {
                var category = property.Name == "issue" || property.Name == "issueRefusals" || property.Name == "byDesign" ? "legacy cell-level or unknown" : "unknown";
                problems.Add($"{path}: {category} field '{property.Name}'");
            }
        }
    }
}
#endif
