// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_scoping_translation_refusals;

public class and_only_given_events_feed_an_outbound_projection : given.a_translated_application
{
    protected override string Scenario => "given";

    async Task Because() => await Execute();

    [Fact] void should_run_the_specification() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
}
