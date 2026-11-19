// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_a_constraint_target_append_that_conflicts_with_a_given_event : for_SemanticReactionSpecificationRenderer.given.a_constraint_target_with_invocation_expectations
{
    void Establish()
    {
        var append = SemanticReactionSpecification.Input(_specification);
        _specification = _specification with { GivenEvents = [new(append.EventContract, append.Values) { EventSource = append.EventSource! with { Value = SemanticValue.Text("other") } }] };
    }

    void Because() => Validate();

    [Fact] void should_refuse_the_specification() => _diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains("append-time constraint", StringComparison.Ordinal));
}
#endif
