// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex;

public class when_encountering_an_unresolved_reference : given.a_dependency_index
{
    void Establish()
    {
        var command = _registerProject.Commands.Single();
        _registerProject = _registerProject with
        {
            Commands = [command with { Produces = [command.Produces.Single() with { EventContract = Identity("missing-event") }] }]
        };
    }

    void Because() => BuildIndex();

    [Fact] void should_not_claim_completeness() => _index.IsSemanticGraphComplete.ShouldBeFalse();
    [Fact] void should_explain_the_unresolved_reference() => _index.IncompleteReasons.Any(reason => reason.Contains("unresolved identity", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_retain_other_known_dependencies() => _index.Dependents(_registerProject.Events.Single().Id).ShouldContain(_projectLookup.Projections.Single().Id);
}
