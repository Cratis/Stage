// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_scoping_translation_refusals;

public class and_the_specification_does_not_reach_translation : given.a_translated_application
{
    protected override string Scenario => "unrelated";

    async Task Because() => await Execute();

    [Fact] void should_run_the_specification() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
}
