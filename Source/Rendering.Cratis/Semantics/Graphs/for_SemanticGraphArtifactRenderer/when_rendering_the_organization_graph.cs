// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.for_SemanticGraphArtifactRenderer;

public class when_rendering_the_organization_graph : Specification
{
    RenderedFile[] _files = [];

    void Because()
    {
        _files = [.. SemanticGraphArtifactRenderer.Render(an_organization_graph_model.Context())];
    }

    string Content(string name) => _files.Single(file => file.RelativePath.EndsWith($"/{name}.cs", StringComparison.Ordinal)).Content;

    [Fact] void should_render_a_record_per_node_and_edge() => _files.Select(file => Path.GetFileName(file.RelativePath)).Order(StringComparer.Ordinal).ShouldEqual(["Group.cs", "HasRole.cs", "MemberOf.cs", "Person.cs"]);
    [Fact] void should_place_them_under_the_graph_folder() => _files.ShouldEachConformTo(file => file.RelativePath.StartsWith("Graphs/Organization/", StringComparison.Ordinal));
    [Fact] void should_name_the_graph() => Content("Person").ShouldContain("[Graph(\"Organization\")]");
    [Fact] void should_mark_a_node() => Content("Person").ShouldContain("[Node]");
    [Fact] void should_not_let_chronicle_map_what_the_model_did_not_say() => Content("Person").ShouldContain("[NoAutoMap]");
    [Fact] void should_key_a_node_on_the_event_source_by_default() => Content("Person").ShouldContain("[FromEvent<PersonRegistered>]");
    [Fact] void should_remove_a_node_with_its_edges() => Content("Person").ShouldContain("[RemovedWith<PersonRemoved>(Detach = true)]");
    [Fact] void should_declare_the_key_of_a_node_that_is_an_event_source_identity_as_the_value_it_holds() => Content("Person").ShouldContain("([Key] Guid PersonId");
    [Fact] void should_map_a_property_from_the_event() => Content("Person").ShouldContain("SetFrom<PersonRegistered>(nameof(PersonRegistered.Name))");
    [Fact] void should_not_map_the_key_again() => Content("Person").ShouldNotContain("SetFromContext");
    [Fact] void should_name_an_edge_by_its_type() => Content("MemberOf").ShouldContain("[Edge(\"MEMBER_OF\")]");
    [Fact] void should_read_the_end_nodes_from_the_event() => Content("MemberOf").ShouldContain("EdgeFrom<MemberAdded>(sourceKey: WellKnownExpressions.EventSourceId, targetKey: nameof(MemberAdded.GroupId))");
    [Fact] void should_remove_an_edge_by_its_end_nodes() => Content("MemberOf").ShouldContain("EdgeRemovedWith<MemberRemoved>(sourceKey: WellKnownExpressions.EventSourceId, targetKey: nameof(MemberRemoved.GroupId))");
    [Fact] void should_declare_the_endpoint_members_as_source_and_target() => Content("MemberOf").ShouldContain("Source<Person>");
    [Fact] void should_declare_the_target() => Content("MemberOf").ShouldContain("Target<Group>");
    [Fact] void should_identify_an_edge_by_the_event_property() => Content("HasRole").ShouldContain("identity: nameof(RoleAssigned.AssignmentId)");
    [Fact] void should_mark_the_identity_of_an_edge_as_its_key() => Content("HasRole").ShouldContain("[Key] AssignmentId AssignmentId");
    [Fact] void should_not_map_the_identity_again() => Content("HasRole").ShouldNotContain("SetFrom<RoleAssigned>(nameof(RoleAssigned.AssignmentId))");
    [Fact] void should_map_the_event_context_into_a_property() => Content("MemberOf").ShouldContain("SetFromContext<MemberAdded>(\"Occurred\")");
    [Fact] void should_not_render_a_graph_record_without_the_application_as_its_source() => _files.ShouldEachConformTo(file => file.Sources.Length == 1);
}
#endif
