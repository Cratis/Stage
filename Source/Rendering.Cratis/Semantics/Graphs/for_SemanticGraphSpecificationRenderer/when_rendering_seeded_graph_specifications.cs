// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.for_SemanticGraphSpecificationRenderer;

public class when_rendering_seeded_graph_specifications : Specification
{
    RenderedFile[] _files = [];
    RenderedFile[] _sample = [];

    void Because()
    {
        var context = a_seeded_graph_model.Context();
        _files = Render(context);
        _sample = Render(a_seeded_graph_model.Context(a_seeded_graph_model.Source));
    }

    string Content(string name) => _sample.Single(file => file.RelativePath.EndsWith($"when_{name}_is_graphed.cs", StringComparison.Ordinal)).Content;

    [Fact] void should_seed_nodes() => _files.Single().Content.ShouldContain("await _scenario.SeedNode(");
    [Fact] void should_seed_in_establish() => _files.Single().Content.ShouldContain("async Task Establish()");
    [Fact] void should_unwrap_concept_keys() => _files.Single().Content.ShouldContain("new PersonId(Guid.Parse(\"6fa459ea-ee8a-4ca4-894e-db77e160355e\")).Value");
    [Fact] void should_unwrap_concept_properties_for_the_seed_api() => _files.Single().Content.ShouldContain("[\"name\"] = \"Ada\"");
    [Fact] void should_seed_edges_without_an_identity() => Content("keeping_a_seeded_graph").ShouldContain(".Value, null, new Dictionary<string, object?>");
    [Fact] void should_seed_edges_with_a_concept_identity() => Content("revoking_a_seeded_role").ShouldContain("new AssignmentId(Guid.Parse(\"9d5d2f42-d1c9-4d54-bd8f-a257e7dc00ba\")).Value");
    [Fact] void should_seed_properties_with_the_graph_naming_policy() => Content("keeping_a_seeded_graph").ShouldContain("[\"tags\"] = new object?[]");
    [Fact] void should_keep_optional_values() => Content("keeping_a_seeded_graph").ShouldContain("[\"visits\"] = 2");
    [Fact] void should_seed_before_appending() => (Content("updating_a_seeded_person").IndexOf("_scenario.SeedNode(", StringComparison.Ordinal) < Content("updating_a_seeded_person").IndexOf("_scenario.Append(", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_append_given_events_before_the_when_event() => (Content("updating_a_seeded_person").IndexOf("new MemberAdded(", StringComparison.Ordinal) < Content("updating_a_seeded_person").IndexOf("new PersonRegistered(", StringComparison.Ordinal)).ShouldBeTrue();

    [Fact]
    void should_preserve_leading_acronyms_in_seed_property_names()
    {
        var context = a_seeded_graph_model.Context(a_seeded_graph_model.Source.Replace("tags", "uRLs", StringComparison.Ordinal));
        Render(context).Single(file => file.RelativePath.Contains("keeping_a_seeded_graph", StringComparison.Ordinal)).Content.ShouldContain("[\"URLs\"] = new object?[]");
    }

    static RenderedFile[] Render(SemanticApplicationContext context) => [.. context.Application.GraphSpecifications.Select(specification => SemanticGraphSpecificationRenderer.Render(specification, context))];
}
#endif
