// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_invocation_expectations_and_a_refusal_outcome : for_SemanticReactionSpecificationRenderer.given.a_refusal_outcome_with_invocation_expectations
{
    void Because()
    {
        ValidateExpectations();
        Validate();
    }

    [Fact] void should_admit_the_specification() => _diagnostics.ShouldBeEmpty();
}
#endif
