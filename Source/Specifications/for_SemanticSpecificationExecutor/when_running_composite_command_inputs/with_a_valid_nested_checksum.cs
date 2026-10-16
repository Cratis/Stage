// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_command_inputs;

public class with_a_valid_nested_checksum : given.a_composite_input
{
    void Establish() => Prepare("CheckedDetails", "{\"count\":6,\"code\":\"79927398713\"}");
    Task Because() => Execute();

    [Fact] void should_match_reference_success() => (_reference.Passed && _result.Outcome == SemanticSpecificationOutcome.Passed).ShouldBeTrue();
    [Fact] void should_have_no_validation_failures() => _failures.ShouldBeEmpty();
    [Fact] void should_admit_the_nested_checksum_shape() => SemanticValueShapeAdmission.Refusal(new(_plan), _command.Properties.Single(property => property.Name == "value").Type).ShouldBeNull();
}
