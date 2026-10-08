// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;

namespace Cratis.Stage.Host;

internal static class SemanticSpecificationRuns
{
    internal const string Route = "/stage/semantic/specifications/run";

    internal static void Map(WebApplication app, SemanticExecutionPlan plan) =>
        app.MapPost(Route, (SemanticSpecificationRunRequest request, ISemanticSpecificationExecutor executor, CancellationToken cancellationToken) => Run(plan, request, executor, cancellationToken));

    static async Task<IResult> Run(SemanticExecutionPlan plan, SemanticSpecificationRunRequest request, ISemanticSpecificationExecutor executor, CancellationToken cancellationToken)
    {
        var scopes = new List<SemanticId>();
        foreach (var scope in request.Scopes ?? [])
        {
            if (!SemanticId.TryParse(scope, out var id))
            {
                return Results.BadRequest(new { details = $"Invalid semantic identity '{scope}'." });
            }

            scopes.Add(id);
        }

        // Only the immutable plan reaches the executor. It creates fresh state for each specification,
        // independent of the live runtime's world and Chronicle event store.
        var report = await executor.Run(plan, new([.. scopes]), new(), cancellationToken);

        return Results.Json(report, SemanticSpecificationRunReportFile.SerializerOptions);
    }
}

internal sealed record SemanticSpecificationRunRequest(string[]? Scopes = null);
