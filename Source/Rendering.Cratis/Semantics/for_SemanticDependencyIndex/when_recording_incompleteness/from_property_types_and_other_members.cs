// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_recording_incompleteness;

public class from_property_types_and_other_members : given.a_dependency_index
{
    readonly Dictionary<string, SemanticDependencyIndex> _indices = [];
    SemanticDependencyIndex _baseline = null!;
    SemanticId _event;
    SemanticId _command;

    void Establish()
    {
        BuildIndex();
        _baseline = _index;
        var original = _registerProject;
        var owner = original.Events.Single();
        _event = owner.Id;
        _command = original.Commands.Single().Id;
        Record("event-type", original with { Events = [owner with { Properties = [.. owner.Properties.Select(property => property with { Type = property.Type with { Kind = (SemanticTypeReferenceKind)999 } })] }] });
        Record("unresolved-target", original with { Events = [owner with { Properties = [.. owner.Properties.Select(property => property with { Type = SemanticTypeReference.ForConcept(SemanticId.Create(SemanticKind.Concept, "missing")) })] }] });
        Record("command-type", original with { Commands = [.. original.Commands.Select(command => command with { Properties = [.. command.Properties.Select(property => property with { Type = property.Type with { Kind = (SemanticTypeReferenceKind)999 } })] })] });
        Record("visibility", original with { Events = [owner with { Visibility = (SemanticEventVisibility)999 }] });
        Record("unresolved-command", original with { Commands = [.. original.Commands.Select(command => command with { Produces = [.. command.Produces.Select(produced => produced with { EventContract = SemanticId.Create(SemanticKind.EventContract, "missing") })] })] });
    }

    [Fact] void should_keep_every_index_globally_incomplete() => _indices.Values.All(index => !index.IsSemanticGraphComplete).ShouldBeTrue();
    [Fact] void should_identify_the_event_type_gap() => _indices["event-type"].Incompleteness.All(gap => gap.Owner == _event && gap.PropertyTypeOf == _event && gap.Kind == SemanticDependencyIncompletenessKind.UnknownVariant).ShouldBeTrue();
    [Fact] void should_locate_an_actual_unresolved_target() => _indices["unresolved-target"].Incompleteness.All(gap => gap.Owner == _event && gap.PropertyTypeOf == _event && gap.Member == "SemanticTypeReference.Target" && gap.Kind == SemanticDependencyIncompletenessKind.UnresolvedIdentity).ShouldBeTrue();
    [Fact] void should_identify_the_command_type_gap() => _indices["command-type"].Incompleteness.All(gap => gap.Owner == _command && gap.PropertyTypeOf == _command).ShouldBeTrue();
    [Fact] void should_not_label_visibility_as_a_property_type() => _indices["visibility"].Incompleteness.All(gap => gap.Owner == _event && gap.PropertyTypeOf is null).ShouldBeTrue();
    [Fact] void should_attribute_a_missing_produced_event_to_the_command() => _indices["unresolved-command"].Incompleteness.All(gap => gap.Owner == _command && gap.PropertyTypeOf is null).ShouldBeTrue();
    [Fact] void should_preserve_incoming_event_edges() => Assert.Equal(_baseline.Edges.Where(edge => edge.Dependency == _event), _indices["event-type"].Edges.Where(edge => edge.Dependency == _event));
    [Fact] void should_preserve_event_dependents() => Assert.Equal(_baseline.Dependents(_event), _indices["event-type"].Dependents(_event));

    void Record(string name, SemanticSlice slice)
    {
        _registerProject = slice;
        BuildIndex();
        _indices.Add(name, _index);
    }
}
