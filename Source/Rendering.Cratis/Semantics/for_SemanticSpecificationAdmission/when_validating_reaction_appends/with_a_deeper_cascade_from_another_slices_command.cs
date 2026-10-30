// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

/// <summary>
/// Screenplay decision 0115: a command another slice owns is decided without its facts being kept, so its event sets off
/// nothing in the specification's run, not even a reaction of the specification's own slice.
/// </summary>
public class with_a_deeper_cascade_from_another_slices_command : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    const string Source = """
        module Work
          feature Processing
            slice StateChange Completion
              command Complete
                id String identifier
                produces Completed
                  for id
              event Completed
            slice Automation FollowUp
              event Started
                id String
              reaction Worker
                when Started
                  invokes Complete
                    id = id
              reaction Next
                when Completed
              specification Completing
                when append Started
                  id = "one"
                then no events
        """;

    void Establish() => Prepare(Source);
    void Because() => Validate();
    [Fact] void should_not_refuse_a_cascade_the_run_never_reaches() => _diagnostics.ShouldBeEmpty();
}
