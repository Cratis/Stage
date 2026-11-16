// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Text.Json;
using Cratis.Chronicle.Graphs;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Lowers a graph query that collects nested rows (ESM v54) to a Chronicle graph view query, and shapes the rows it returns as read-model values.
/// </summary>
/// <remarks>
/// <para>
/// A collecting query has the shape of a graph view: one match of its root node alone, scalar returns from the root, and one
/// <see cref="SemanticGraphCollect"/> per relation. Each collect maps one-to-one onto a relation of the view (decision 0163): the relation
/// follows the collect's edge type, <see cref="GraphViewDirection.Outgoing"/> when the hop is outgoing and
/// <see cref="GraphViewDirection.Incoming"/> otherwise, with the collect's cardinality, to a nested view rooted at the reached label whose
/// members are the collect's members. A condition on the root becomes a <see cref="GraphPropertyClause"/>, and the conditions on one collected
/// node become one <see cref="GraphRelatedClause"/>, so they select roots and never narrow what is collected.
/// </para>
/// <para>
/// Member names are the read-model property names and, in a nested view, the row type's property names, so a returned
/// <see cref="GraphRow"/> already carries the names the read model serializes with. The query is backend neutral: Arc's
/// <c language="text">Cratis.Arc.Neo4j.Chronicle</c> translates it to Cypher and runs it through the tenant-aware graph session.
/// </para>
/// </remarks>
public static class SemanticGraphViews
{
    /// <summary>
    /// Gets whether a graph query collects nested rows and is therefore read as a graph view.
    /// </summary>
    /// <param name="query">The graph query.</param>
    /// <returns>True when the query has at least one collect.</returns>
    public static bool Collects(SemanticGraphQuery query) => !query.Collects.IsDefaultOrEmpty;

    /// <summary>
    /// Lowers a collecting graph query to a graph view query.
    /// </summary>
    /// <param name="application">The application the query belongs to, for its graphs and the composite row types.</param>
    /// <param name="query">The collecting graph query.</param>
    /// <param name="readModel">The read model the rows are returned as.</param>
    /// <param name="arguments">The argument values by name; an argument that is absent leaves its condition out, as it does for a flat graph query.</param>
    /// <returns>The <see cref="GraphQuery"/>.</returns>
    /// <exception cref="UnsupportedSemanticGraph">The query is not shaped like a graph view, or names something the graph does not declare.</exception>
    public static GraphQuery Lower(SemanticApplication application, SemanticGraphQuery query, SemanticReadModel readModel, IReadOnlyDictionary<string, object?> arguments)
    {
        var graph = application.Graphs.FirstOrDefault(candidate => string.Equals(candidate.Name, query.Graph, StringComparison.Ordinal))
            ?? throw new UnsupportedSemanticGraph($"Graph query '{query.Name}' reads graph '{query.Graph}', which is not declared.");
        var root = Root(query);
        var members = new List<(int Order, GraphQueryMember Member)>();
        foreach (var @return in query.Returns)
        {
            if (!string.Equals(@return.Alias, root.Alias, StringComparison.Ordinal))
            {
                throw new UnsupportedSemanticGraph($"Graph query '{query.Name}' returns a property of '{@return.Alias}'; a collecting query returns scalars from its root '{root.Alias}' only.");
            }

            members.Add((Order(readModel, @return.Property), new(Name(readModel, @return.Property, query), GraphViewMemberKind.Property, @return.Source)));
        }

        // A condition names a collected node by the alias of the node its hop reaches.
        var collected = new Dictionary<string, GraphQueryMember>(StringComparer.Ordinal);
        foreach (var collect in query.Collects)
        {
            var member = new GraphQueryMember(Name(readModel, collect.Property, query), GraphViewMemberKind.Relation, Relation: Relation(application, graph, query, root, collect));
            members.Add((Order(readModel, collect.Property), member));
            if (collect.Hop.Node.Alias is { } alias) collected[alias] = member;
        }

        var node = new GraphQueryNode(readModel.Name, root.Label, KeyOf(graph, root.Label), [.. members.OrderBy(member => member.Order).Select(member => member.Member)]);
        var shape = query.Cardinality == SemanticQueryCardinality.Many ? GraphQueryShape.List : GraphQueryShape.First;
        return new(graph.Name, shape, node, Clauses(query, root, node, collected, arguments));
    }

    /// <summary>
    /// Shapes a row a graph view query returned as the values of a read model: scalars as they are, a related row as an object
    /// and a <see cref="GraphViewCardinality.Many"/> relation as an array of objects.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <returns>The values by read-model property name.</returns>
    public static IReadOnlyDictionary<string, JsonElement> Values(GraphRow row) =>
        row.Values.ToDictionary(value => value.Key, value => JsonSerializer.SerializeToElement(Plain(value.Value)), StringComparer.Ordinal);

    static SemanticGraphPatternNode Root(SemanticGraphQuery query)
    {
        if (query.Matches.Length != 1 || query.Matches[0] is not { IsOptional: false, Start.Alias: not null } match || !match.Hops.IsDefaultOrEmpty)
        {
            throw new UnsupportedSemanticGraph($"Graph query '{query.Name}' collects, so it must match its root node alone, once, without hops and not optionally.");
        }

        return match.Start;
    }

    static GraphQueryRelation Relation(SemanticApplication application, SemanticGraph graph, SemanticGraphQuery query, SemanticGraphPatternNode root, SemanticGraphCollect collect)
    {
        if (!string.Equals(collect.From, root.Alias, StringComparison.Ordinal))
        {
            throw new UnsupportedSemanticGraph($"Graph query '{query.Name}' collects from '{collect.From}'; a collect follows an edge from the root '{root.Alias}'.");
        }

        var edge = graph.Edges.FirstOrDefault(candidate => string.Equals(candidate.Type, collect.Hop.Type, StringComparison.Ordinal))
            ?? throw new UnsupportedSemanticGraph($"Graph query '{query.Name}' collects along edge '{collect.Hop.Type}', which graph '{graph.Name}' does not declare.");
        var row = application.Types.FirstOrDefault(type => type.Id == collect.Row)
            ?? throw new UnsupportedSemanticGraph($"Graph query '{query.Name}' collects rows of a type the application does not declare.");
        var members = collect.Members.Select(member => new GraphQueryMember(
            row.Properties.FirstOrDefault(property => property.Id == member.Property)?.Name ?? throw new UnsupportedSemanticGraph($"Graph query '{query.Name}' collects a property that row type '{row.Name}' does not declare."),
            member.Kind == SemanticGraphCollectMemberKind.EdgeProperty ? GraphViewMemberKind.EdgeProperty : GraphViewMemberKind.Property,
            member.Source));
        var target = new GraphQueryNode(row.Name, collect.Hop.Node.Label, KeyOf(graph, collect.Hop.Node.Label), [.. members]);
        return new(
            edge.Type,
            collect.Hop.Outgoing ? GraphViewDirection.Outgoing : GraphViewDirection.Incoming,
            collect.Cardinality == SemanticGraphCollectCardinality.One ? GraphViewCardinality.One : GraphViewCardinality.Many,
            edge.IdentifiedBy,
            target);
    }

    static List<GraphQueryClause> Clauses(
        SemanticGraphQuery query,
        SemanticGraphPatternNode root,
        GraphQueryNode node,
        Dictionary<string, GraphQueryMember> collected,
        IReadOnlyDictionary<string, object?> arguments)
    {
        var clauses = new List<GraphQueryClause>();
        var related = new Dictionary<string, List<GraphPropertyClause>>(StringComparer.Ordinal);
        foreach (var condition in query.Conditions)
        {
            object? value;
            if (condition.Argument is not null)
            {
                if (!arguments.TryGetValue(condition.Argument, out value)) continue;
            }
            else
            {
                value = SemanticGraphCypher.Literal(condition.Literal);
            }

            if (string.Equals(condition.Alias, root.Alias, StringComparison.Ordinal))
            {
                clauses.Add(Clause(node, condition.Property, value));
            }
            else if (collected.TryGetValue(condition.Alias, out var member))
            {
                // The conditions on one collected node must all hold on the same reached node, so they share one related clause.
                if (!related.TryGetValue(condition.Alias, out var nested))
                {
                    nested = [];
                    related[condition.Alias] = nested;
                    clauses.Add(new GraphRelatedClause(member.Name, nested));
                }

                nested.Add(Clause(member.Relation!.Target, condition.Property, value));
            }
            else
            {
                throw new UnsupportedSemanticGraph($"Graph query '{query.Name}' has a condition on '{condition.Alias}', which is neither its root nor a collected node.");
            }
        }

        return clauses;
    }

    static GraphPropertyClause Clause(GraphQueryNode node, string property, object? value)
    {
        var member = node.Members.FirstOrDefault(candidate => candidate.Kind == GraphViewMemberKind.Property && string.Equals(candidate.Source, property, StringComparison.Ordinal));
        return new(member?.Name ?? property, property, value);
    }

    static string KeyOf(SemanticGraph graph, string label)
    {
        var node = graph.Nodes.FirstOrDefault(candidate => string.Equals(candidate.Label, label, StringComparison.Ordinal))
            ?? throw new UnsupportedSemanticGraph($"Graph '{graph.Name}' does not declare node '{label}'.");
        return node.Properties.FirstOrDefault(property => property.IsKey)?.Name ?? throw new UnsupportedSemanticGraph($"Node '{label}' of graph '{graph.Name}' declares no key.");
    }

    static string Name(SemanticReadModel readModel, SemanticId property, SemanticGraphQuery query) =>
        readModel.Properties.FirstOrDefault(candidate => candidate.Id == property)?.Name
            ?? throw new UnsupportedSemanticGraph($"Graph query '{query.Name}' assigns a property that read model '{readModel.Name}' does not declare.");

    static int Order(SemanticReadModel readModel, SemanticId property) => readModel.Properties.Select(candidate => candidate.Id).ToList().IndexOf(property);

    static object? Plain(object? value) => value switch
    {
        GraphRow row => row.Values.ToDictionary(entry => entry.Key, entry => Plain(entry.Value), StringComparer.Ordinal),
        string text => text,
        IEnumerable items and not IDictionary => items.Cast<object?>().Select(Plain).ToList(),
        _ => value
    };
}
