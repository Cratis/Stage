// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Graphs;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticGraphViews;

/// <summary>
/// Reads every case of the shared graph view query vectors the way Stage serves a collecting graph query: the query is lowered by
/// <see cref="SemanticGraphViews"/>, run by Chronicle's reference executor over the seed graph and shaped as read-model values. The
/// result must be exactly the vector's, which is also what Screenplay's reference runner returns for the same queries: roots distinct
/// and ordered by key, clauses that select roots without narrowing what is collected, collected rows ordered by the reached key and the
/// edge identity, empty lists, null single rows and bare nodes with absent properties.
/// </summary>
public class when_reading_the_shared_query_vectors : given.the_shared_graph_views
{
    // Each clause shape of the vectors, as the view, the shape and the clause signature, names the query of the fixture model that
    // expresses it and the argument each clause value is passed as.
    static readonly Dictionary<string, (string Query, string[] Arguments)> _queries = new(StringComparer.Ordinal)
    {
        ["PersonWithGroups|list|"] = ("PeopleWithGroups", []),
        ["PersonWithGroups|list|related:groups(property:id)"] = ("PeopleInGroup", ["groupId"]),
        ["PersonWithGroups|list|property:name"] = ("PeopleNamed", ["name"]),
        ["PersonWithGroups|single|property:id"] = ("PersonById", ["personId"]),
        ["PersonWithGroups|single|property:name"] = ("PersonNamed", ["name"]),
        ["GroupWithMembers|list|"] = ("GroupsWithMembers", []),
        ["GroupWithMembers|list|related:members(property:name)"] = ("GroupsWithMemberNamed", ["name"]),
        ["GroupWithParent|list|"] = ("GroupsWithParent", []),
        ["PersonRoles|list|"] = ("PeopleWithRoles", [])
    };

    readonly List<string> _mismatches = [];
    int _cases;

    async Task Because()
    {
        foreach (var vector in Fixture("graph-views-query-vectors.json")["cases"]!.AsArray())
        {
            _cases++;
            var clauses = vector!["clauses"]!.AsArray();
            var key = $"{vector["view"]}|{vector["shape"]}|{string.Join(" and ", clauses.Select(Signature))}";
            if (!_queries.TryGetValue(key, out var mapped))
            {
                _mismatches.Add($"{vector["name"]}: no query expresses '{key}'");
                continue;
            }

            var query = _application.GraphQueries.Single(candidate => candidate.Name == mapped.Query);
            var arguments = mapped.Arguments.Zip(clauses.SelectMany(Values)).ToDictionary(pair => pair.First, pair => (object?)pair.Second, StringComparer.Ordinal);
            var rows = await _scenario.QueryExecutor.Execute(SemanticGraphViews.Lower(_application, query, ReadModelOf(query), arguments));
            var shaped = rows.Select(Json).ToList();
            var actual = vector["shape"]!.GetValue<string>() == "single" ? shaped.FirstOrDefault() : new JsonArray([.. shaped]);
            if (!JsonNode.DeepEquals(actual, vector["expected"]))
            {
                _mismatches.Add($"{vector["name"]}: expected {vector["expected"]?.ToJsonString() ?? "null"}, but Stage read {actual?.ToJsonString() ?? "null"}");
            }
        }
    }

    [Fact] void should_read_every_vector() => _cases.ShouldEqual(9);
    [Fact] void should_read_exactly_the_expected_results() => _mismatches.ShouldBeEmpty();

    static JsonNode Json(GraphRow row) => JsonNode.Parse(JsonSerializer.Serialize(SemanticGraphViews.Values(row)))!;

    static string Signature(JsonNode? clause) => clause!["kind"]!.GetValue<string>() == "related"
        ? $"related:{clause["member"]}({string.Join(" and ", clause["clauses"]!.AsArray().Select(Signature))})"
        : $"property:{clause["member"]}";

    static IEnumerable<string> Values(JsonNode? clause) => clause!["kind"]!.GetValue<string>() == "related"
        ? clause["clauses"]!.AsArray().SelectMany(Values)
        : [clause["value"]!.GetValue<string>()];
}
