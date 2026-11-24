// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Screenplay.Conformance;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

// One render/compile/recover per corpus fixture, shared by the declaration assertions and the optional artifact probe.
internal static class generation_round_trips
{
    internal const string ProbeDirectoryVariable = "CRATIS_STAGE_GENERATION_PROBE_DIRECTORY";
    static readonly JsonSerializerOptions _indented = new() { WriteIndented = true };
    static readonly string[] _names = ["GeneratedValuesCorpus.V15", "GenerationSupportCorpus.V21", "EsmGoldenVectors.V38"];
    static readonly Lazy<IReadOnlyDictionary<string, observation>> _fixtures = new(Observe);

    internal static observation Get(string name) => _fixtures.Value[name];
    internal static IEnumerable<observation> All => _fixtures.Value.Values;

    static IReadOnlyDictionary<string, observation> Observe()
    {
        var table = expected_conformance_table.Load();
        var excluded = round_trip_fixtures.EligibleNames(table).Except(_names, StringComparer.Ordinal);
        var fixtures = round_trip_fixtures.Fixtures(table, excluded).ToArray();
        Assert.Equal(_names.Order(StringComparer.Ordinal), fixtures.Select(fixture => fixture.Name).Order(StringComparer.Ordinal));
        return fixtures.ToDictionary(fixture => fixture.Name, Observe, StringComparer.Ordinal);
    }

    static observation Observe(round_trip_fixture fixture)
    {
        var directory = RetainRenderedArtifacts(fixture);
        var recoveredSource = string.Empty;
        var result = arc_round_trip.Run(fixture, source =>
        {
            recoveredSource = source;
            if (directory is not null) File.WriteAllText(Path.Combine(directory, "Recovered.play"), source);
        });
        if (directory is not null) File.WriteAllText(Path.Combine(directory, "observation.json"), JsonSerializer.Serialize(result, _indented));
        return new(fixture, result, recoveredSource);
    }

    static string? RetainRenderedArtifacts(round_trip_fixture fixture)
    {
        var root = Environment.GetEnvironmentVariable(ProbeDirectoryVariable);
        if (string.IsNullOrWhiteSpace(root)) return null;
        var directory = Path.Combine(root, fixture.Name);
        Directory.CreateDirectory(directory);
        foreach (var artifact in fixture.Plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)))
        {
            var path = Path.Combine(directory, artifact.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, [.. artifact.Bytes]);
        }
        var declarations = fixture.Model.Application.Concepts.Select(concept => new { concept.Name, concept.Primitive, concept.Generation });
        File.WriteAllText(Path.Combine(directory, "expected-generation.json"), JsonSerializer.Serialize(declarations, _indented));
        Console.WriteLine($"Generation artifact probe: {fixture.Name}: {directory}");
        return directory;
    }

    internal sealed record observation(round_trip_fixture Fixture, RoundTripObservation Result, string RecoveredSource);
}
#endif
