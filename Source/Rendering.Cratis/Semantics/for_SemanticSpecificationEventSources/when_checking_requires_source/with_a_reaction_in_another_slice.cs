// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationEventSources.when_checking_requires_source;

public class with_a_reaction_in_another_slice : a_source_observability_check
{
    void Because() => _requiresSource = Check(_slice, _slice with
    {
        Id = SemanticId.Create(SemanticKind.Slice, "notifying-slice"),
        Name = "Notify",
        Kind = SemanticSliceKind.Automation,
        Events = [],
        ReadModels = [],
        Projections = [],
        Specifications = [],
        Queries = [],
        Reactions = [new(SemanticId.Create(SemanticKind.Reaction, "notifying-reaction"), "Notify", [new(SemanticReactionTriggerKind.Event) { Source = _slice.Events.Single().Id }])]
    });

    [Fact] void should_require_a_source_for_the_registered_reactor() => _requiresSource.ShouldBeTrue();
}
