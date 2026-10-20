// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_append_refusals;

public class with_another_message_than_the_constraints : given.a_specification_in_a_slice
{
    void Establish() => Prepare(with_an_append_a_unique_event_constraint_refuses.Source.Replace("then error \"Dispatched once\"", "then error \"Something else\"", StringComparison.Ordinal));
    void Because() => Validate();

    [Fact] void should_not_pass_the_refusal_off_as_the_constraints() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
}
