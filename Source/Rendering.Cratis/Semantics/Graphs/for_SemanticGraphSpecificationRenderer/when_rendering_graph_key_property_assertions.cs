// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given.an_organization_graph_model;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.for_SemanticGraphSpecificationRenderer;

public class when_rendering_graph_key_property_assertions : Specification
{
    string _content = string.Empty;
    string _eventSourceEdgeContent = string.Empty;
    string _eventBuiltContent = string.Empty;

    void Because()
    {
        _content = Render();
        _eventSourceEdgeContent = Render(eventSourceEdgeKey: true);
        _eventBuiltContent = Render(specificationName: "AssertingEventBuiltGraphKeys");
    }

    [Fact] void should_assert_the_person_key_as_a_uuid() => _content.ShouldContain($".PersonId.ShouldEqual(Guid.Parse(\"{PersonId}\"));");
    [Fact] void should_assert_an_event_built_node_key_as_a_uuid() => _eventBuiltContent.ShouldContain($".PersonId.ShouldEqual(Guid.Parse(\"{PersonId}\"));");
    [Fact] void should_assert_the_group_key_as_a_uuid() => _content.ShouldContain($".GroupId.ShouldEqual(Guid.Parse(\"{GroupId}\"));");
    [Fact] void should_keep_the_concept_edge_identity() => _content.ShouldContain($".AssignmentId.ShouldEqual(new AssignmentId(Guid.Parse(\"{AssignmentId}\")));");
    [Fact] void should_assert_an_event_source_edge_identity_as_a_uuid() => _eventSourceEdgeContent.ShouldContain($".AssignmentId.ShouldEqual(Guid.Parse(\"{AssignmentId}\"));");
    [Fact] void should_keep_non_key_concept_properties() => _content.ShouldContain(".Name.ShouldEqual(new PersonName(\"Ada\"));");

    static string Render(bool eventSourceEdgeKey = false, string specificationName = "AssertingGraphKeys")
    {
        var context = an_organization_graph_model.Context(a_graph_with_asserted_key_properties.Source(eventSourceEdgeKey));
        var specification = context.Application.GraphSpecifications.Single(specification => specification.Name == specificationName);
        return SemanticGraphSpecificationRenderer.Render(specification, context).Content;
    }
}
#endif
