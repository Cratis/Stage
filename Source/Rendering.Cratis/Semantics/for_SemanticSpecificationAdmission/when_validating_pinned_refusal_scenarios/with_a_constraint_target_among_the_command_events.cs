// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_pinned_refusal_scenarios;

public class with_a_constraint_target_among_the_command_events : given.a_specification_in_a_slice
{
    void Establish() => Prepare(with_a_pinned_generated_property.Source
        .Replace("      event Failed\n", "      constraint OneCompletion\n        unique sku on Completed\n        message \"Already completed\"\n      event Failed\n", StringComparison.Ordinal));

    void Because() => Validate();

    [Fact] void should_leave_the_enforcement_to_the_command_pipeline() => _diagnostics.ShouldBeEmpty();
}
