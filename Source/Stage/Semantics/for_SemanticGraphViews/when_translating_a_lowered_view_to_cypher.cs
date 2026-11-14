// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j;
using Cratis.Arc.Neo4j.Chronicle;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticGraphViews;

/// <summary>
/// Holds a lowered collecting query to the Cypher Arc's graph session runs for it. The text is the one Arc documents for the
/// <c language="csharp">GroupWithMembers</c> view, so a collect lowers to exactly the view a hand-written <c language="csharp">[ViewOf&lt;Group&gt;]</c> record declares.
/// </summary>
public class when_translating_a_lowered_view_to_cypher : given.an_organization_with_views
{
    const string Expected = """
        MATCH (n0:`Group`)
        RETURN {`id`: n0.`groupId`, `name`: n0.`name`, `members`: COLLECT { MATCH (n0)<-[e1:`MEMBER_OF`]-(n1:`Person`) RETURN {`id`: n1.`personId`, `name`: n1.`name`, `since`: e1.`since`} AS item ORDER BY n1.`personId` }} AS row
        ORDER BY n0.`groupId`
        """;

    GraphCypherQuery _cypher = null!;
    GraphCypherQuery _related = null!;

    void Because()
    {
        _cypher = GraphQueryTranslator.Translate(Lower("GroupsWithMembers"), GraphDatabaseMode.PerNamespace);
        _related = GraphQueryTranslator.Translate(Lower("PeopleInGroup", ("groupId", "g1")), GraphDatabaseMode.PerNamespace);
    }

    [Fact] void should_translate_to_the_documented_view_query() => _cypher.Text.ShouldEqual(Expected.ReplaceLineEndings("\n"));
    [Fact] void should_select_roots_by_a_related_node_with_an_exists_subquery() => _related.Text.ShouldContain("WHERE EXISTS { MATCH (n0)-[e1:`MEMBER_OF`]->(n1:`Group`) WHERE n1.`groupId` = $p0 AND n1.`name` = $p1 }");
    [Fact] void should_pass_every_value_as_a_parameter() => _related.Parameters.Values.ShouldEqual(["g1", "Engineering"]);
}
