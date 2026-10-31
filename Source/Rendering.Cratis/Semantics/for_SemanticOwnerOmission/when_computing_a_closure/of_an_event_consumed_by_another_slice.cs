// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerOmission.when_computing_a_closure;

public class of_an_event_consumed_by_another_slice : given.an_omission_context
{
    SemanticOwnerOmission? _omission;

    void Because()
    {
        BuildContext();
        _omission = SemanticOwnerOmission.Compute(_context, _index, Roots(_registerProject.Events.Single().Id));
    }

    [Fact] void should_be_provable() => _omission.ShouldNotBeNull();
    [Fact] void should_keep_the_root() => _omission!.Roots.ContainsKey(_registerProject.Events.Single().Id).ShouldBeTrue();
    [Fact] void should_include_the_event_slice_and_its_command() => _omission!.Closure.ShouldContain(_registerProject.Id, _registerProject.Commands.Single().Id);
    [Fact] void should_include_the_consuming_projection() => _omission!.Closure.ShouldContain(_projectLookup.Projections.Single().Id);
    [Fact] void should_include_every_declaration_of_the_consuming_slice() => _omission!.Closure.ShouldContain(_projectLookup.Id, _projectLookup.ReadModels.Single().Id, _projectLookup.Queries.Single().Id);
    [Fact] void should_not_include_the_concepts() => _omission!.Closure.Intersect([_projectId.Id, _projectName.Id]).ShouldBeEmpty();
    [Fact] void should_not_be_empty() => _omission!.IsEmpty.ShouldBeFalse();
}
