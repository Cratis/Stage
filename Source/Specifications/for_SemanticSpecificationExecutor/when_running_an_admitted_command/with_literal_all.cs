// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_literal_all : a_universal_projection
{
    void Establish() => _plan = CreatePlan(true, mapping: "\"fixed\"");

    Task Because() => Run();

    [Fact] void should_report_the_expected_outcome() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
}
