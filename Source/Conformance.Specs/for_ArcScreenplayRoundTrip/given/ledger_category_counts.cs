// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

internal sealed record ledger_category_counts(int Semantic, int Diagnostic, int Unsupported)
{
    public bool IsWithinBudget => Semantic + Diagnostic > 0 && Unsupported == 0 &&
        Semantic <= arc_round_trip_ledger.MaximumSemanticEntries &&
        Diagnostic <= arc_round_trip_ledger.MaximumDiagnosticEntries;
}
#endif
