// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_denied_operations;

public class with_an_unrelated_reaction : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish()
    {
        Prepare("""
            module Work
              feature Processing
                slice Automation FollowUp
                  command Complete
                    id String identifier
                    produces Completed
                      for id
                  command Denied
                    id String identifier
                    produces Completed
                      for id
                  event Started
                    id String
                  event OtherStarted
                    id String
                  event Completed
                  reaction Worker
                    when Started
                      invokes Complete
                        id = id
                  reaction OtherWorker
                    when OtherStarted
                      invokes Denied
                        id = id
                  specification Completing
                    when append Started
                      id = "one"
                    then Completed
            """);
        _context.DeniedOperations.Add(_located.Slice.Commands.Single(command => command.Name == "Denied").Id, "Unsupported validation rule.");
    }

    void Because() => Validate();

    [Fact] void should_admit_the_non_denied_reactions_specification() => _diagnostics.ShouldBeEmpty();
}
