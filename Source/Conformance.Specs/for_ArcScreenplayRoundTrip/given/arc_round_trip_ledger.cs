// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Conformance;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

// The reviewed ledger of what the Arc round trip loses, and how a verdict is written for a reader who has to act on it.
internal static class arc_round_trip_ledger
{
    public const string FileName = "for_ArcScreenplayRoundTrip/arc-round-trip-losses.json";

    // Independent, reviewed ratchets: resolving one category must not fund new entries in the other.
    // Lower each cap when its losses are resolved; never derive either cap from the current ledger counts.
    public const int MaximumSemanticEntries = 191;
    public const int MaximumDiagnosticEntries = 42;

    public static SemanticLossLedger Load()
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, FileName));

        return SemanticLossLedger.Load(stream);
    }

    public static ledger_category_counts ValidateCategories(IEnumerable<SemanticLossEntry> entries)
    {
        var semantic = 0;
        var diagnostic = 0;
        var unsupported = 0;
        foreach (var entry in entries)
        {
            switch (entry.Change)
            {
                case SemanticLossChange.Missing:
                case SemanticLossChange.Unexpected:
                case SemanticLossChange.Changed:
                case SemanticLossChange.Uncompilable:
                    semantic++;
                    break;
                case SemanticLossChange.Diagnostic:
                    diagnostic++;
                    break;
                default:
                    unsupported++;
                    break;
            }
        }

        return new(semantic, diagnostic, unsupported);
    }

    // One line per finding, shaped so a reviewer can copy the fixture, change and path into the ledger.
    public static string Describe(IEnumerable<SemanticLossFinding> findings) =>
        string.Join(Environment.NewLine, findings.Select(finding => $"  {finding.Fixture} {finding.Change} {finding.Path}: {finding.Message}"));
}
#endif
