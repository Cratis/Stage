// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.for_SemanticGraphSpecificationRenderer;

public class when_rendering_the_organization_graph_specifications : Specification
{
    RenderedFile[] _files = [];

    void Because()
    {
        // The Chronicle graph API is not in the framework name catalog until a Chronicle that ships it is pinned, so the oracle spelling is read.
        using var qualified = QualifiedSpelling.Begin();
        var context = an_organization_graph_model.Context();
        _files = [.. context.Application.GraphSpecifications.Where(specification => !SemanticGraphAdmission.IsSeeded(specification)).Select(specification => SemanticGraphSpecificationRenderer.Render(specification, context))];
    }

    string Content(string name) => _files.Single(file => file.RelativePath.EndsWith($"{name}.cs", StringComparison.Ordinal)).Content;

    [Fact] void should_render_a_specification_per_unseeded_graph_specification() => _files.Length.ShouldEqual(2);
    [Fact] void should_leave_the_seeded_specification_to_the_degradation() => _files.ShouldEachConformTo(file => !file.RelativePath.Contains("detaches", StringComparison.Ordinal));
    [Fact] void should_compile_into_the_application_in_debug_only() => _files.ShouldEachConformTo(file => Path.GetFileName(file.RelativePath).StartsWith("when_", StringComparison.Ordinal));
    [Fact] void should_project_the_events_with_the_graph_scenario() => Content("when_adding_a_member_draws_an_edge_is_graphed").ShouldContain("readonly global::Cratis.Chronicle.Testing.Graphs.GraphScenario _scenario = new();");
    [Fact] void should_append_the_given_events_then_the_appended_one() => Content("when_adding_a_member_draws_an_edge_is_graphed").Split("_scenario.Append(", StringSplitOptions.None).Length.ShouldEqual(4);
    [Fact] void should_read_a_node_by_its_key() => Content("when_adding_a_member_draws_an_edge_is_graphed").ShouldContain("_scenario.Node<global::Organization.Graphs.Organization.Group>(new global::Organization.Common.GroupId(global::System.Guid.Parse(\"0f8fad5b-d9cb-469f-a165-70867728950e\")).Value)");
    [Fact] void should_read_an_edge_by_its_end_nodes() => Content("when_adding_a_member_draws_an_edge_is_graphed").ShouldContain("_scenario.Edge<global::Organization.Graphs.Organization.MemberOf>(");
    [Fact] void should_assert_a_property() => Content("when_adding_a_member_draws_an_edge_is_graphed").ShouldContain("void should_have_group_1_name() => _element0!.Name.ShouldEqual(new global::Organization.Common.GroupName(\"Engineering\"));");
    [Fact] void should_assert_that_an_edge_exists() => Content("when_adding_a_member_draws_an_edge_is_graphed").ShouldContain("void should_have_member_of_2() => _element1.ShouldNotBeNull();");
    [Fact] void should_read_an_edge_by_its_identity() => Content("when_revoking_a_role_deletes_only_that_edge_is_graphed").ShouldContain("new global::Organization.Common.AssignmentId(global::System.Guid.Parse(\"9c858901-8a57-4791-81fe-4c455b099bc9\")).Value);");
    [Fact] void should_assert_that_an_edge_is_gone() => Content("when_revoking_a_role_deletes_only_that_edge_is_graphed").ShouldContain("void should_not_have_has_role_1() => _element0.ShouldBeNull();");
}
#endif
