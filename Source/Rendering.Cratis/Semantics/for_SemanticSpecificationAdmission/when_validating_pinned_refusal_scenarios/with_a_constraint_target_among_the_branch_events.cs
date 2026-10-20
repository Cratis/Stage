// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_pinned_refusal_scenarios;

public class with_a_constraint_target_among_the_branch_events : given.a_specification_in_a_slice
{
    void Establish() => Prepare(with_a_pinned_generated_property.Source
        .Replace("      event Failed\n", "      constraint OneFailure\n        unique reason on Failed\n        message \"Already failed\"\n      event Failed\n", StringComparison.Ordinal));

    void Because() => Validate();

    [Fact] void should_refuse_the_specification() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
    [Fact] void should_name_the_branch_outputs() => _diagnostics.Single().Message.ShouldContain("branch outputs");
}
