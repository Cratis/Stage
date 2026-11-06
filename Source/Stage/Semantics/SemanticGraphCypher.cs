// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Semantics;

/// <summary>
/// The engine query a graph query lowers to, with the parameters it is run with.
/// </summary>
/// <param name="Text">The Cypher text; node labels, edge types and property names come only from the declared graph, and every value is a parameter.</param>
/// <param name="Parameters">The parameter values by name.</param>
/// <param name="Columns">The result columns, in the order the read model's properties are assigned.</param>
public sealed record SemanticGraphCypherQuery(string Text, IReadOnlyDictionary<string, object?> Parameters, IReadOnlyList<SemanticGraphColumn> Columns);

/// <summary>
/// A result column of a graph query and the read-model property it fills.
/// </summary>
/// <param name="Name">The column name in the result.</param>
/// <param name="Property">The read-model property identity it fills.</param>
public sealed record SemanticGraphColumn(string Name, SemanticId Property);

/// <summary>
/// Lowers a graph query to Cypher. The model contains no engine text: the pattern is rebuilt here from the declared nodes and edges,
/// so the only names that reach the engine query are the ones the compiler already checked against the graph.
/// </summary>
public static class SemanticGraphCypher
{
    /// <summary>
    /// Lowers a graph query.
    /// </summary>
    /// <param name="query">The graph query.</param>
    /// <param name="graph">The graph it reads.</param>
    /// <param name="arguments">The argument values by name, already converted to engine values; an argument that is absent leaves its condition out.</param>
    /// <returns>The Cypher query and its parameters.</returns>
    /// <exception cref="UnsupportedSemanticGraph">A construct has no lowering.</exception>
    public static SemanticGraphCypherQuery Lower(SemanticGraphQuery query, SemanticGraph graph, IReadOnlyDictionary<string, object?> arguments)
    {
        var text = new StringBuilder();
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);
        var bound = new HashSet<string>(StringComparer.Ordinal);
        foreach (var match in query.Matches)
        {
            text.Append(match.IsOptional ? "OPTIONAL MATCH " : "MATCH ").Append(Node(match.Start, bound));
            foreach (var hop in match.Hops)
            {
                var edge = hop.Alias is null ? $"[:{Name(hop.Type)}]" : $"[{Alias(hop.Alias)}:{Name(hop.Type)}]";
                text.Append(hop.Outgoing ? $"-{edge}->" : $"<-{edge}-").Append(Node(hop.Node, bound));
                if (hop.Alias is not null) bound.Add(hop.Alias);
            }

            text.Append('\n');
        }

        var conditions = new List<string>();
        foreach (var condition in query.Conditions)
        {
            var name = $"p{parameters.Count}";
            object? value;
            if (condition.Argument is not null)
            {
                if (!arguments.TryGetValue(condition.Argument, out value)) continue;
            }
            else
            {
                value = Literal(condition.Literal);
            }

            parameters[name] = value;
            conditions.Add($"{Alias(condition.Alias)}.{Name(condition.Property)} = ${name}");
        }

        if (conditions.Count > 0) text.Append("WHERE ").AppendJoin(" AND ", conditions).Append('\n');
        var columns = new List<SemanticGraphColumn>();
        var returns = new List<string>();
        foreach (var @return in query.Returns)
        {
            var column = $"c{columns.Count}";
            columns.Add(new(column, @return.Property));
            returns.Add($"{Alias(@return.Alias)}.{Name(@return.Source)} AS {column}");
        }

        text.Append("RETURN ").AppendJoin(", ", returns);
        return new(text.ToString(), parameters, columns);
    }

    static string Node(SemanticGraphPatternNode node, HashSet<string> bound)
    {
        if (node.Alias is null) return $"(:{Name(node.Label)})";

        // A node a previous pattern already bound is referred to by its alias alone: that is how two paths share a start.
        return bound.Add(node.Alias) ? $"({Alias(node.Alias)}:{Name(node.Label)})" : $"({Alias(node.Alias)})";
    }

    // Every name interpolated into the text is an identifier of the declared graph or an alias of the pattern, which the compiler has
    // restricted to letters, digits and underscore. The check here keeps that true for a model that was not compiled by this version.
    static string Name(string name) => IsIdentifier(name) ? name : throw new UnsupportedSemanticGraph($"'{name}' is not an identifier the graph query can lower.");

    static string Alias(string alias) => IsIdentifier(alias) ? $"`{alias}`" : throw new UnsupportedSemanticGraph($"'{alias}' is not an alias the graph query can lower.");

    static bool IsIdentifier(string name) => name.Length > 0 && !char.IsDigit(name[0]) && name.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');

    static object? Literal(SemanticValue? value) => value switch
    {
        SemanticTextValue text => text.Value,
        SemanticNumberValue number => number.Value,
        SemanticBooleanValue boolean => boolean.Value,
        _ => throw new UnsupportedSemanticGraph("A graph query condition literal must be text, a number or a boolean.")
    };
}
