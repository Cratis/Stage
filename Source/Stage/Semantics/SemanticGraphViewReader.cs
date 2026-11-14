// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Neo4j;
using Cratis.Arc.Neo4j.Chronicle;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Api;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Reads a graph query that collects nested rows as a Chronicle graph view, through Arc's graph session.
/// </summary>
/// <remarks>
/// The query is lowered by <see cref="SemanticGraphViews"/> and run by Arc's <see cref="Neo4jGraphQueryExecutor"/> on the session the
/// caller opened, so it reads the database of the current tenant and, in single-database mode, only the current namespace. Each row
/// becomes the read model's runtime type with its nested rows as values.
/// </remarks>
/// <param name="application">The application the query belongs to.</param>
/// <param name="query">The collecting graph query.</param>
/// <param name="model">The read model the rows are returned as.</param>
/// <param name="readModelType">The runtime type of the read model.</param>
internal sealed class SemanticGraphViewReader(SemanticApplication application, SemanticGraphQuery query, SemanticReadModel model, Type readModelType)
{
    /// <summary>
    /// Reads the rows.
    /// </summary>
    /// <param name="session">The open graph session.</param>
    /// <param name="arguments">The argument values by name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows, in result order.</returns>
    internal async Task<IReadOnlyList<object>> Read(IGraphSession session, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var view = SemanticGraphViews.Lower(application, query, model, arguments);
        var executor = new Neo4jGraphQueryExecutor(session, NullLogger<Neo4jGraphQueryExecutor>.Instance);
        var rows = await executor.Execute(view, cancellationToken);
        return [.. rows.Select(row => Row(SemanticGraphViews.Values(row)))];
    }

    static string Text(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        _ => value.GetRawText()
    };

    // The identity of a row is its identifier values, joined as a flat graph query joins them.
    DynamicReadModel Row(IReadOnlyDictionary<string, JsonElement> values)
    {
        var key = model.Properties.Where(property => property.IsIdentifier).Select(property => values.TryGetValue(property.Name, out var value) ? Text(value) : string.Empty);
        var row = (DynamicReadModel)JsonSerializer.Deserialize(JsonSerializer.Serialize(new { Id = string.Join('|', key) }), readModelType)!;
        foreach (var value in values) row.Values[value.Key] = value.Value;
        return row;
    }
}
