// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

// Replays named fixtures through the same render, compile and recover path as the full gate and keeps what an Arc author needs to reproduce
// a loss: the emitted C# closure, the recovered Screenplay source and the observation with its comparison and diagnostics. It never reconciles
// the ledger, so a filtered replay cannot satisfy the full gate's coverage.
internal static class round_trip_replay
{
    internal const string FixturesVariable = "CRATIS_STAGE_ARC_REPLAY_FIXTURES";
    internal const string DirectoryVariable = "CRATIS_STAGE_ARC_REPLAY_DIRECTORY";
    static readonly JsonSerializerOptions _indented = new() { WriteIndented = true };

    internal static IReadOnlyList<string> Replay(IReadOnlyList<string> names, string directory)
    {
        var table = expected_conformance_table.Load();
        var eligible = round_trip_fixtures.EligibleNames(table);
        var unknown = names.Except(eligible, StringComparer.Ordinal).ToArray();
        Assert.True(unknown.Length == 0, $"Not eligible for the round trip: {string.Join(", ", unknown)}.");

        var replayed = new List<string>();
        foreach (var fixture in round_trip_fixtures.Fixtures(table, eligible.Except(names, StringComparer.Ordinal)))
        {
            var path = Path.Combine(directory, fixture.Name);
            Directory.CreateDirectory(path);
            Retain(fixture, path);
            replayed.Add(fixture.Name);
        }

        return replayed;
    }

    static void Retain(round_trip_fixture fixture, string directory)
    {
        foreach (var artifact in fixture.Plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)))
        {
            var file = Path.Combine(directory, artifact.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllBytes(file, [.. artifact.Bytes]);
        }

        try
        {
            var result = arc_round_trip.Run(fixture, source => File.WriteAllText(Path.Combine(directory, "Recovered.play"), source));
            File.WriteAllText(Path.Combine(directory, "observation.json"), JsonSerializer.Serialize(result, _indented));
        }
        catch (Exception exception)
        {
            File.WriteAllText(Path.Combine(directory, "error.txt"), exception.Message);
        }
    }
}
