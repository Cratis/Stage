// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.for_SemanticGraphAdmission;

public class when_checking_a_collecting_graph_query : Specification
{
    const string Source = """
        concept PersonId : Uuid
        concept GroupId  : Uuid

        type GroupMembership
          id    GroupId
          since DateTime optional

        module Organization
          feature Chart
            slice StateView GraphViews
              graph Organization
                node Person
                  personId PersonId key
                  name     String
                node Group
                  groupId GroupId key
                edge MEMBER_OF source Person target Group
                  since DateTime

              readmodel PersonWithGroups
                id     PersonId key
                groups GroupMembership[]

              query PeopleWithGroups => PersonWithGroups[] graph Organization
                match person:Person
                return
                  id     = person.personId
                  groups = collect person -membership:MEMBER_OF-> group:Group
                    id    = group.groupId
                    since = membership.since
        """;

    ImmutableArray<ArtifactRenderDiagnostic> _diagnostics;

    void Because() => _diagnostics = SemanticGraphAdmission.Check(an_organization_graph_model.Context(Source), chronicleAdmitsGraphs: true);

    [Fact] void should_refuse_it_as_a_graph_query_the_renderer_does_not_render() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual([SemanticGraphAdmission.QueryNotRendered]);
    [Fact] void should_refuse_it_as_an_error() => _diagnostics.Single().Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_say_that_it_collects_nested_rows() => _diagnostics.Single().Message.ShouldContain("collects nested rows (ESM v54)");
    [Fact] void should_say_where_it_runs() => _diagnostics.Single().Message.ShouldContain("Chronicle graph view through Arc's graph session");
}
#endif
