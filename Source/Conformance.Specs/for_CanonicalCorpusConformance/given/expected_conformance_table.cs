// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

// A refusal that is correct behavior: the corpus input is intentionally invalid, so there is nothing to track.
public record by_design_refusal(string Code, string Reason, string Artifact)
{
    public renderer_refusal Fingerprint => new(Code, Artifact);
}

// One expected cell. Every code of a refused cell is tracked by the cell's Stage issue, or listed in `ByDesign` with the reason it is correct.
// `Reason` is an optional note.
public record expected_conformance_cell(string Outcome, int? Issue, string? Reason, IReadOnlyList<by_design_refusal> ByDesign)
{
    // Renderer issue coverage is explicit and bounded to exact code/artifact pairs, not every diagnostic sharing a code.
    public IReadOnlyList<renderer_refusal> IssueRefusals { get; init; } = [];
}

// The checked-in conformance matrix (conformance-matrix.json), loaded from the test output directory.
public class expected_conformance_table
{
    public const string FileName = "for_CanonicalCorpusConformance/conformance-matrix.json";
    static readonly JsonSerializerOptions _text = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public const string WriteActualVariable = "CRATIS_STAGE_WRITE_CONFORMANCE_MATRIX";

    public string CorpusVersion { get; private init; } = string.Empty;

    public int MinimumVectorCount { get; private init; }

    public IReadOnlyDictionary<string, string> ExcludedTypes { get; private init; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, IReadOnlyDictionary<conformance_surface, expected_conformance_cell>> Vectors { get; private init; } =
        new Dictionary<string, IReadOnlyDictionary<conformance_surface, expected_conformance_cell>>();

    public static expected_conformance_table Load()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, FileName)));
        var root = document.RootElement;
        return new()
        {
            CorpusVersion = root.GetProperty("corpusVersion").GetString()!,
            MinimumVectorCount = root.GetProperty("minimumVectorCount").GetInt32(),
            ExcludedTypes = root.GetProperty("excludedTypes").EnumerateObject().ToDictionary(property => property.Name, property => property.Value.GetString()!),
            Vectors = root.GetProperty("vectors").EnumerateObject().ToDictionary(
                vector => vector.Name,
                vector => (IReadOnlyDictionary<conformance_surface, expected_conformance_cell>)vector.Value.EnumerateObject().ToDictionary(
                    surface => Enum.Parse<conformance_surface>(surface.Name, true),
                    surface => new expected_conformance_cell(
                        surface.Value.GetProperty("outcome").GetString()!,
                        surface.Value.TryGetProperty("issue", out var issue) && issue.ValueKind == JsonValueKind.Number ? issue.GetInt32() : null,
                        surface.Value.TryGetProperty("reason", out var reason) ? reason.GetString() : null,
                        surface.Value.TryGetProperty("byDesign", out var byDesign)
                            ? [.. byDesign.EnumerateArray().Select(entry => new by_design_refusal(
                                entry.GetProperty("code").GetString()!,
                                entry.TryGetProperty("reason", out var why) ? why.GetString() ?? string.Empty : string.Empty,
                                entry.TryGetProperty("artifact", out var artifact) ? artifact.GetString() ?? string.Empty : string.Empty))]
                            : [])
                    {
                        IssueRefusals = surface.Value.TryGetProperty("issueRefusals", out var coverage)
                            ? [.. coverage.EnumerateArray().Select(entry => new renderer_refusal(
                                entry.GetProperty("code").GetString()!, entry.GetProperty("artifact").GetString()!))]
                            : []
                    }))
        };
    }

    // Renders the table the actual run produced, one line per cell so a changed cell is a one-line diff.
    // A cell whose outcome is unchanged keeps its issue and reason from `previous`; a new refusal gets `"issue": null`, which the spec rejects.
    public static string Render(
        expected_conformance_table previous,
        string corpusVersion,
        IReadOnlyDictionary<string, IReadOnlyDictionary<conformance_surface, string>> actual)
    {
        var builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine($"  \"corpusVersion\": {JsonSerializer.Serialize(corpusVersion, _text)},");
        builder.AppendLine($"  \"minimumVectorCount\": {actual.Count},");
        builder.AppendLine("  \"excludedTypes\": {");
        builder.AppendLine(string.Join($",{Environment.NewLine}", previous.ExcludedTypes.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"    {JsonSerializer.Serialize(pair.Key, _text)}: {JsonSerializer.Serialize(pair.Value, _text)}")));
        builder.AppendLine("  },");
        builder.AppendLine("  \"vectors\": {");
        builder.AppendLine(string.Join($",{Environment.NewLine}", actual.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(vector =>
        {
            var cells = vector.Value.OrderBy(pair => pair.Key).Select(pair =>
            {
                previous.Vectors.TryGetValue(vector.Key, out var known);
                expected_conformance_cell? old = null;
                known?.TryGetValue(pair.Key, out old);
                var kept = old is not null && old.Outcome == pair.Value ? old : null;
                var reason = kept?.Reason is { Length: > 0 } note ? $", \"reason\": {JsonSerializer.Serialize(note, _text)}" : string.Empty;
                var fingerprints = pair.Key == conformance_surface.Renderer ? renderer_refusals.Parse(pair.Value) : new HashSet<renderer_refusal>();
                var designs = old?.ByDesign.Where(entry => fingerprints.Contains(entry.Fingerprint)).ToArray() ?? [];
                var byDesign = designs.Length == 0 ? string.Empty : $", \"byDesign\": [{string.Join(", ", designs.Select(entry => $"{{ \"code\": {JsonSerializer.Serialize(entry.Code, _text)}, \"artifact\": {JsonSerializer.Serialize(entry.Artifact, _text)}, \"reason\": {JsonSerializer.Serialize(entry.Reason, _text)} }}"))}]";
                var coverage = old?.IssueRefusals.Where(fingerprints.Contains).ToArray() ?? [];
                var issueRefusals = coverage.Length == 0 ? string.Empty : $", \"issueRefusals\": [{string.Join(", ", coverage.Select(entry => $"{{ \"code\": {JsonSerializer.Serialize(entry.Code, _text)}, \"artifact\": {JsonSerializer.Serialize(entry.Artifact, _text)} }}"))}]";
                var tracking = pair.Value.StartsWith("refused:", StringComparison.Ordinal)
                    ? $", \"issue\": {(kept?.Issue is { } issue ? issue.ToString() : "null")}{issueRefusals}{byDesign}{reason}"
                    : string.Empty;
                return $"      \"{pair.Key.ToString().ToLowerInvariant()}\": {{ \"outcome\": {JsonSerializer.Serialize(pair.Value, _text)}{tracking} }}";
            });
            return $"    {JsonSerializer.Serialize(vector.Key, _text)}: {{{Environment.NewLine}{string.Join($",{Environment.NewLine}", cells)}{Environment.NewLine}    }}";
        })));
        builder.AppendLine("  }");
        builder.AppendLine("}");
        return builder.ToString();
    }
}
#endif
