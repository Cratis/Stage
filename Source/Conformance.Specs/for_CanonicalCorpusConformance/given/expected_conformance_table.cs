// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

public class expected_conformance_table
{
    public const string FileName = "for_CanonicalCorpusConformance/conformance-matrix.json";
    public const string WriteActualVariable = "CRATIS_STAGE_WRITE_CONFORMANCE_MATRIX";
    static readonly JsonSerializerOptions _text = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    static readonly JsonSerializerOptions _details = new(_text) { WriteIndented = true };

    public string CorpusVersion { get; init; } = string.Empty;
    public int MinimumVectorCount { get; init; }
    public IReadOnlyList<string> SchemaProblems { get; init; } = [];
    public IReadOnlyDictionary<string, string> ExcludedTypes { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, IReadOnlyDictionary<conformance_surface, expected_conformance_cell>> Vectors { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<conformance_surface, expected_conformance_cell>>();

    public static expected_conformance_table Load() => Load(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, FileName)));

    public static expected_conformance_table Load(string text) => conformance_table_schema.Load(text);

    // Dispositions belong to an exact vector/surface/fingerprint, never to a cell outcome or code.
    public static string Render(
        expected_conformance_table previous,
        string corpusVersion,
        IReadOnlyDictionary<string, IReadOnlyDictionary<conformance_surface, conformance_observation>> actual)
    {
        var builder = new StringBuilder()
            .AppendLine("{")
            .AppendLine($"  \"corpusVersion\": {Text(corpusVersion)},")
            .AppendLine($"  \"minimumVectorCount\": {actual.Count},")
            .AppendLine("  \"excludedTypes\": {")
            .AppendJoin($",{Environment.NewLine}", previous.ExcludedTypes.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"    {Text(pair.Key)}: {Text(pair.Value)}"))
            .AppendLine()
            .AppendLine("  },")
            .AppendLine("  \"vectors\": {");
        builder.AppendJoin($",{Environment.NewLine}", actual.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(vector =>
        {
            var cells = vector.Value.OrderBy(pair => pair.Key).Select(pair =>
            {
                var old = previous.Vectors.TryGetValue(vector.Key, out var row) && row.TryGetValue(pair.Key, out var cell) ? cell : null;
                var records = pair.Value.Outcome == "refused" ? pair.Value.Refusals.Select(fingerprint =>
                {
                    // Ambiguous prior records cannot silently donate a disposition.
                    var matches = old?.Refusals.Where(record => record.Fingerprint == fingerprint).ToArray() ?? [];
                    var kept = matches.Length == 1 ? matches[0] : null;
                    return Record(new(fingerprint, kept?.Issue, kept?.ByDesign));
                }).ToArray() : [];
                var refusals = records.Length == 0 ? string.Empty : $", \"refusals\": [{Environment.NewLine}        {string.Join($",{Environment.NewLine}        ", records)} ]";
                return $"      \"{pair.Key.ToString().ToLowerInvariant()}\": {{ \"outcome\": {Text(pair.Value.Outcome)}{refusals} }}";
            });
            return $"    {Text(vector.Key)}: {{{Environment.NewLine}{string.Join($",{Environment.NewLine}", cells)}{Environment.NewLine}    }}";
        })).AppendLine()
            .AppendLine("  }")
            .AppendLine("}");
        return builder.ToString();
    }

    public static string RenderDetails(IReadOnlyDictionary<string, IReadOnlyDictionary<conformance_surface, conformance_observation>> actual) =>
        JsonSerializer.Serialize(
        actual.SelectMany(vector => vector.Value.SelectMany(surface => surface.Value.Refusals.Select(fingerprint => new
        {
            vector = vector.Key,
            surface = surface.Key.ToString().ToLowerInvariant(),
            code = fingerprint.Code,
            kind = fingerprint.Kind,
            artifact = fingerprint.Artifact,
            details = surface.Value.Details.GetValueOrDefault(fingerprint, string.Empty)
        }))),
        _details);

    static string Text(string text) => JsonSerializer.Serialize(text, _text);

    static string Record(refusal_record record)
    {
        var fingerprint = record.Fingerprint;
        var kind = fingerprint.Kind is null ? string.Empty : $", \"kind\": {Text(fingerprint.Kind)}";
        var issue = record.ByDesign is null ? ", \"issue\": null" : string.Empty;
        if (record.Issue is { } number)
        {
            issue = $", \"issue\": {number}";
        }
        var design = record.ByDesign is null ? string.Empty : $", \"byDesign\": {Text(record.ByDesign)}";
        return $"{{ \"code\": {Text(fingerprint.Code)}{kind}, \"artifact\": {Text(fingerprint.Artifact)}{issue}{design} }}";
    }
}
#endif
