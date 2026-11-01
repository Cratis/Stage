// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex;

public class when_a_handler_returns_an_unresolved_event : given.a_dependency_index
{
    void Establish()
    {
        var command = _registerProject.Commands.Single();
        _registerProject = _registerProject with
        {
            Commands = [command with
            {
                Produces = [],
                Handler = new("body", 2) { PossibleEvents = [command.Produces.Single() with { EventContract = Identity("missing-handler-event"), Mappings = [] }] }
            }]
        };
    }

    void Because() => BuildIndex();

    [Fact] void should_not_claim_completeness() => _index.IsSemanticGraphComplete.ShouldBeFalse();
    [Fact] void should_locate_the_unresolved_handler_reference() => _index.Incompleteness.Any(reason => reason.Member == "SemanticProducedEvent.EventContract").ShouldBeTrue();
    [Fact] void should_explain_the_unresolved_identity() => _index.IncompleteReasons.Any(reason => reason.Contains("unresolved identity", StringComparison.Ordinal)).ShouldBeTrue();
}
