// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_command_inputs;

public class with_explicit_null_members : given.a_composite_input
{
    void Establish()
    {
        Prepare("Details", "{}", "Label required");
        var type = _plan.Model.Application.Types.Single(type => type.Name == "Details");
        ChangeInput(SemanticValue.Composite([.. type.Properties.Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Null))]));
    }
    Task Because() => Execute();

    [Fact] void should_run_concept_rules_on_explicit_null() => (_reference.Passed && _result.Outcome == SemanticSpecificationOutcome.Passed).ShouldBeTrue();
    [Fact] void should_fail_only_not_empty() => _failures.Select(failure => failure.Message).ShouldEqual(["Label required"]);
}
