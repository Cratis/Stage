// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_rejected_reaction_cascades;

public class with_a_triggering_command_without_rules : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    internal const string Source = """
        module Work
          feature Processing
            slice Automation FollowUp
              command Start
                id String identifier
                produces Started
                  for id
                  id = id
              command Complete
                id String identifier
                validate
                  id not empty message "required"
                produces Completed
                  for id
              event Started
                id String
              event Completed
              reaction Worker
                when Started
                  invokes Complete
                    id = id
              specification Rejecting
                when Start
                  id = ""
                then error "required"
        """;

    void Establish() => Prepare(Source);
    void Because() => Validate();
    [Fact] void should_lower_the_cascade_without_refusing_it() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-011");
}
