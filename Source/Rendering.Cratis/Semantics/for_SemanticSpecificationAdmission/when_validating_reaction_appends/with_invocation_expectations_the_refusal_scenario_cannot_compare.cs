// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_invocation_expectations_the_refusal_scenario_cannot_compare : for_SemanticReactionSpecificationRenderer.given.a_refusal_outcome_with_invocation_expectations
{
    void Establish()
    {
        var expected = _specification.ThenInvocations[0];
        _specification = _specification with { ThenInvocations = [expected with { Values = [expected.Values[0] with { Value = SemanticValue.Array([]) }] }] };
    }

    void Because() => ValidateExpectations();

    [Fact] void should_refuse_the_expectation() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-016"]);
}
#endif
