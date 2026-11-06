// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticGraphCypher;

public class when_lowering_a_graph_query : Specification
{
    const string Source = """
        concept PersonId : Uuid
        concept GroupId : Uuid
        concept Name : String

        module Organization
          feature Chart
            slice StateView OrganizationChart
              graph Organization
                node Person
                  personId PersonId key
                  name     Name
                node Group
                  groupId GroupId key
                  title   Name
                edge MEMBER_OF source Person target Group
                  since String optional
                edge LEADS source Person target Group

              readmodel Membership
                personId PersonId key
                person   Name
                since    String optional

              readmodel Leadership
                groupId GroupId key
                leader  Name optional

              query MembersOf => Membership[] graph Organization
                filter groupId GroupId
                match person:Person -link:MEMBER_OF-> group:Group
                where group.groupId = $groupId and group.title = "Ops"
                return
                  personId = person.personId
                  person   = person.name
                  since    = link.since

              query Leaders => Leadership[] graph Organization
                match group:Group
                optional match group:Group <-LEADS- leader:Person
                return
                  groupId = group.groupId
                  leader  = leader.name
        """;

    SemanticApplication _application;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Model"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Model", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success);
        _application = compilation.Value.Model.Application;
    }

    SemanticGraphCypherQuery Lower(string name, params (string Name, object? Value)[] arguments) =>
        SemanticGraphCypher.Lower(
            _application.GraphQueries.Single(query => query.Name == name),
            _application.Graphs.Single(),
            arguments.ToDictionary(argument => argument.Name, argument => argument.Value, StringComparer.Ordinal));

    [Fact] void should_match_the_pattern() => Lower("MembersOf", ("groupId", "g")).Text.ShouldContain("MATCH (`person`:Person)-[`link`:MEMBER_OF]->(`group`:Group)");
    [Fact] void should_compare_with_parameters_and_never_with_values() => Lower("MembersOf", ("groupId", "g")).Text.ShouldContain("WHERE `group`.groupId = $p0 AND `group`.title = $p1");
    [Fact] void should_pass_the_argument_and_the_literal() => Lower("MembersOf", ("groupId", "g")).Parameters.ShouldEqual(new Dictionary<string, object?> { ["p0"] = "g", ["p1"] = "Ops" });
    [Fact] void should_leave_out_a_condition_whose_argument_is_absent() => Lower("MembersOf").Text.ShouldNotContain("groupId");
    [Fact] void should_return_every_property_as_a_column() => Lower("MembersOf", ("groupId", "g")).Text.ShouldContain("RETURN `person`.personId AS c0, `person`.name AS c1, `link`.since AS c2");
    [Fact] void should_name_a_column_per_returned_property() => Lower("MembersOf", ("groupId", "g")).Columns.Count.ShouldEqual(3);
    [Fact] void should_follow_an_incoming_edge_in_an_optional_match() => Lower("Leaders").Text.ShouldContain("OPTIONAL MATCH (`group`)<-[:LEADS]-(`leader`:Person)");
    [Fact] void should_bind_a_shared_start_by_its_alias_only() => Lower("Leaders").Text.ShouldContain("MATCH (`group`:Group)\nOPTIONAL MATCH (`group`)");
}
