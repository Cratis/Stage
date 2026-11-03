// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_denied_operations;

public class with_a_projection_specification : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish()
    {
        Prepare("""
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
                    produces Completed
                      for id
                  event Started
                    id String
                  event Completed
                  reaction Worker
                    when Started
                      invokes Complete
                        id = id
                  readmodel Progress
                    id String key
                  projection ProgressProjection => Progress
                    from Started key id
                      id = id
                  specification Projecting
                    given Started
                      for "one"
                      id = "one"
                    then readmodel Progress
                      id = "one"
            """);
        _context.DeniedOperations.Add(_located.Slice.Commands.Single(command => command.Name == "Complete").Id, "Unsupported validation rule.");
    }

    void Because() => Validate();

    [Fact] void should_admit_the_projection_specification() => _diagnostics.ShouldBeEmpty();
}
