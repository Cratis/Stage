// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Neo4j;
using Cratis.Arc.Queries;
using Cratis.DependencyInjection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Api;
using Microsoft.AspNetCore.Http;
using Neo4j.Driver;
using Neo4jAccessMode = Neo4j.Driver.AccessMode;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Serves a query that reads a graph: it lowers the declared pattern to Cypher, runs it over Arc's graph session and returns the rows as the declared read model.
/// </summary>
/// <remarks>
/// The role check is the model's own policy evaluation, the same one the other queries use, so a graph query is no more open than the
/// policy it declares. A query that is observable re-runs when the graph's projection writes, which Chronicle reports as a change in the world.
/// </remarks>
[IgnoreConvention]
internal sealed class SemanticGraphQueryPerformer : IQueryPerformer
{
    readonly SemanticGraphQuery _query;
    readonly SemanticGraph _graph;
    readonly SemanticReadModel _model;
    readonly IGraphSessionFactory _sessions;
    readonly IHttpContextAccessor _context;
    readonly ISemanticRuntime _runtime;

    internal SemanticGraphQueryPerformer(
        Type type,
        string name,
        IEnumerable<string> location,
        ISemanticRuntime runtime,
        SemanticGraphQuery query,
        SemanticGraph graph,
        SemanticReadModel model,
        IGraphSessionFactory sessions,
        IHttpContextAccessor context)
    {
        ReadModelType = type;
        Type = type;
        Name = name;
        FullyQualifiedName = $"{type.FullName}.{name}";
        Location = location;
        _runtime = runtime;
        _query = query;
        _graph = graph;
        _model = model;
        _sessions = sessions;
        _context = context;
        Parameters = [];
        foreach (var argument in query.Arguments)
        {
            Parameters.Add(argument.Name, typeof(string));
        }
    }

    /// <inheritdoc/>
    public QueryName Name { get; }

    /// <inheritdoc/>
    public FullyQualifiedQueryName FullyQualifiedName { get; }

    /// <inheritdoc/>
    public Type Type { get; }

    /// <inheritdoc/>
    public Type ReadModelType { get; }

    /// <inheritdoc/>
    public IEnumerable<string> Location { get; }

    /// <inheritdoc/>
    public string? CustomRoute => null;

    /// <inheritdoc/>
    public IEnumerable<Type> Dependencies => [];

    /// <inheritdoc/>
    public QueryParameters Parameters { get; }

    /// <inheritdoc/>
    public bool AllowsAnonymousAccess => true;

    /// <inheritdoc/>
    public bool SupportsPaging => false;

    /// <inheritdoc/>
    public bool IsAuthorized(QueryContext context) => Authorize() is null;

    /// <inheritdoc/>
    public async ValueTask<object?> Perform(QueryContext context)
    {
        if (Authorize() is { } refusal)
        {
            throw new SemanticQueryRejected(refusal);
        }

        var many = _query.Cardinality == SemanticQueryCardinality.Many;
        var answer = await Evaluate(context);
        if (_query.Delivery != SemanticQueryDelivery.Live || _runtime is not ISemanticWorldChanges changes)
        {
            if (many) return answer;
            return answer.Count > 0 ? answer[0] : null;
        }

        // A later answer is produced long after this request, so it reads with the arguments captured now.
        var arguments = context;
        var type = many ? typeof(IEnumerable<>).MakeGenericType(ReadModelType) : ReadModelType;
        return typeof(SemanticGraphQueryPerformer)
            .GetMethod(nameof(Live), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(type)
            .Invoke(this, [changes, arguments, ToAnswer(answer, many), many]);
    }

    SemanticLiveQuery<T> Live<T>(ISemanticWorldChanges changes, QueryContext context, T initial, bool many) =>
        new(changes, async () => (T)ToAnswer(await Evaluate(context), many), initial);

    object ToAnswer(IReadOnlyList<object> rows, bool many)
    {
        if (!many) return rows.Count > 0 ? rows[0] : null!;
        var typed = Array.CreateInstance(ReadModelType, rows.Count);
        for (var index = 0; index < rows.Count; index++) typed.SetValue(rows[index], index);
        return typed;
    }

    string? Authorize()
    {
        if (_query.Authorization is null) return null;
        var principal = _context.HttpContext?.User ?? new ClaimsPrincipal();
        return SemanticGraphQueryAuthorization.Refusal(_query, _runtime.Plan, SemanticCallers.From(principal));
    }

    async Task<IReadOnlyList<object>> Evaluate(QueryContext context)
    {
        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var argument in _query.Arguments)
        {
            if (context.Arguments?.TryGetValue(argument.Name, out var value) == true && value is not null) arguments[argument.Name] = value.ToString();
        }

        var cypher = SemanticGraphCypher.Lower(_query, _graph, arguments);
        await using var session = _sessions.Open(Neo4jAccessMode.Read);
        var rows = new List<object>();
        var cursor = await session.Run(cypher.Text, cypher.Parameters);
        await foreach (var record in cursor.WithCancellation(context.CancellationToken))
        {
            rows.Add(Row(record, cypher.Columns));
        }

        return rows;
    }

    DynamicReadModel Row(IRecord record, IReadOnlyList<SemanticGraphColumn> columns)
    {
        var row = (DynamicReadModel)JsonSerializer.Deserialize("{}", ReadModelType)!;
        var identifiers = _model.Properties.Where(property => property.IsIdentifier).ToArray();
        var key = new List<string>();
        foreach (var column in columns)
        {
            var property = _model.Properties.Single(candidate => candidate.Id == column.Property);
            var value = record[column.Name];
            row.Values[property.Name] = JsonSerializer.SerializeToElement(value);
            if (property.IsIdentifier) key.Add(value?.ToString() ?? string.Empty);
        }

        return JsonSerializer.Deserialize(JsonSerializer.Serialize(new { Id = string.Join('|', key) }), ReadModelType) is DynamicReadModel withId
            ? SemanticGraphRows.Merge(withId, row)
            : row;
    }
}
