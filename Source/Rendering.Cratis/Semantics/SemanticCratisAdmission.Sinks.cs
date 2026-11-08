// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    /// <summary>
    /// The code for a query over a read model stored in a named sink: Chronicle writes it there, but the pinned Arc release reads
    /// every rendered query from the application's own MongoDB database, so the query would read the wrong database.
    /// </summary>
    internal const string QueryOverNamedSink = "STAGE-ESM-069";

    static void ValidateNamedSinkQueries(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (context.Sinks.ReadModels.IsEmpty) return;
        foreach (var query in slices.SelectMany(located => located.Slice.Queries))
        {
            if (context.SinkOf(query.ReadModel) is not { } sink) continue;
            diagnostics.Add(Error(
                QueryOverNamedSink,
                $"Query '{query.Name}' reads read model '{context.ReadModels[query.ReadModel].Name}', which is stored in sink '{sink}'. Arc reads rendered queries from the application's MongoDB database and cannot read a named sink yet, so the rendered query would read the wrong database.",
                query.Id));
        }
    }
}
