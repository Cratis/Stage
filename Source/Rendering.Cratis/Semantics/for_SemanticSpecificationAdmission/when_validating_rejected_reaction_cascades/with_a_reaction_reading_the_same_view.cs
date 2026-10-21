// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_rejected_reaction_cascades;

public class with_a_reaction_reading_the_same_view : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    internal const string Source = """
        command reads protected
        module Work
          feature Processing
            slice Automation FollowUp
              command Start
                id String identifier
                reads Item by id
                produces Started
                  for id
                  id = id
              command Complete
                id String identifier
                reads Item by id
                produces Completed
                  for id
              event Started
                id String
              event Completed
              reaction Worker
                when Started
                  invokes Complete
                    id = id
              specification StartingAnUnknownItem
                when Start
                  id = "unknown"
                then error "Item 'unknown' does not exist."
            slice StateView Items
              event Placed
                id String
              event Archived
              readmodel Item
                id String key
              projection BuildItem => Item
                from Placed
                  id = $eventSourceId
                from Started
                  id = $eventSourceId
                remove with Archived
              query ItemById => Item optional
                by id String
        """;

    void Establish() => Prepare(Source);
    bool _rejectedByTrigger;

    void Because()
    {
        _rejectedByTrigger = SemanticReactionCascade.IsRejectedByTrigger(_context, _specification);
        Validate();
    }

    [Fact] void should_treat_the_rejection_as_the_triggering_command_own() => _rejectedByTrigger.ShouldBeTrue();
    [Fact] void should_admit_the_specification() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-011");
}
