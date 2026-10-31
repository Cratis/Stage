// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerOmission.when_computing_a_closure;

public class with_an_independent_sibling_in_a_consuming_slice : given.an_omission_context
{
    SemanticId _composite;
    SemanticId _unrelated;
    SemanticId _root;
    SemanticOwnerOmission? _omission;
    IReadOnlyList<SemanticId> _dependents = null!;
    SemanticId[] _siblings = null!;

    void Establish()
    {
        _composite = Identity("composite");
        _unrelated = Identity("unrelated");
        var contract = _registerProject.Events.Single();
        var property = contract.Properties.Single(property => property.Name == "name");
        _application = _application with
        {
            Types = [new(_composite, "CompositeName", [property with { Id = Identity("composite-member") }]), new(_unrelated, "Unrelated", [])]
        };
        _registerProject = _registerProject with
        {
            Events = [contract with { Properties = [.. contract.Properties.Select(member => member.Id == property.Id ? member with { Type = SemanticTypeReference.ForCompositeType(_composite) } : member)] }]
        };
        _projectLookup = _projectLookup with
        {
            Commands = [_registerProject.Commands.Single() with
            {
                Id = Identity("independent-command"),
                Properties = [.. _registerProject.Commands.Single().Properties.Select(property => property with { Id = Identity($"independent-{property.Name}") })],
                Produces = [],
                Reads = [],
                Validations = [],
                Requirements = [],
                CodeValidations = []
            }]
        };
    }

    void Because()
    {
        BuildContext();
        _root = _registerProject.Commands.Single().Id;
        _omission = SemanticOwnerOmission.Compute(_context, _index, Roots(_root));
        _dependents = _index.Dependents(_root);
        _siblings = [_projectLookup.Id, _projectLookup.ReadModels.Single().Id, _projectLookup.Queries.Single().Id, _projectLookup.Projections.Single().Id, _projectLookup.Commands.Single().Id];
    }

    [Fact] void should_be_provable() => _omission.ShouldNotBeNull();
    [Fact] void should_include_every_declaration_of_the_consuming_slice() => _omission!.Closure.ShouldContain(_siblings);
    [Fact] void should_not_reach_the_event_without_sibling_expansion() => _dependents.ShouldNotContain(_registerProject.Events.Single().Id);
    [Fact] void should_not_reach_the_consumer_without_sibling_expansion() => _dependents.ShouldNotContain(_projectLookup.Projections.Single().Id);
    [Fact] void should_include_the_independent_command_after_expanding_the_consuming_slice() => _omission!.Closure.ShouldContain(_projectLookup.Commands.Single().Id);
    [Fact] void should_not_reach_the_independent_command_through_the_event_alone() => _index.Dependents(_registerProject.Events.Single().Id).ShouldNotContain(_projectLookup.Commands.Single().Id);
    [Fact] void should_not_include_the_composite_the_event_depends_on() => _omission!.Closure.ShouldNotContain(_composite);
    [Fact] void should_not_include_an_unrelated_composite() => _omission!.Closure.ShouldNotContain(_unrelated);
}
