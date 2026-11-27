// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Screenplay.Conformance;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

// The reviewed ledger of what the Arc round trip loses, and how a verdict is written for a reader who has to act on it.
internal static class arc_round_trip_ledger
{
    public const string FileName = "for_ArcScreenplayRoundTrip/arc-round-trip-losses.json";

    static readonly JsonSerializerOptions _observationOptions = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    public static SemanticLossLedger Load()
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, FileName));

        return SemanticLossLedger.Load(stream);
    }

    public static string Superseded(string replacement) => $"Superseded by {replacement} (Screenplay decision 0170)";

    // Observations contain measured differences and diagnostics, not approved loss dispositions.
    public static void WriteObservations(string corpusVersion, IReadOnlyList<RoundTripObservation> observations)
    {
        var output = Environment.GetEnvironmentVariable("CRATIS_STAGE_WRITE_ROUND_TRIP_OBSERVATIONS");
        if (string.IsNullOrWhiteSpace(output)) return;
        File.WriteAllText(output, JsonSerializer.Serialize(new { corpusVersion, observations }, _observationOptions));
    }

    // One line per finding, shaped so a reviewer can copy the fixture, change and path into the ledger.
    public static string Describe(IEnumerable<SemanticLossFinding> findings) =>
        string.Join(Environment.NewLine, findings.Select(finding => $"  {finding.Fixture} {finding.Change} {finding.Path}: {finding.Message}"));
}
#endif
