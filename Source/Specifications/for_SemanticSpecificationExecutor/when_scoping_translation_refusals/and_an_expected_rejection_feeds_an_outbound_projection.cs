// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_scoping_translation_refusals;

// Screenplay decision 0115: the outbound projection belongs to another slice, so it does not run and its unsupported shape does not refuse this slice's specification,
// which executes and fails on its own unmet rejection instead.
public class and_an_expected_rejection_feeds_an_outbound_projection : given.a_translated_application
{
    protected override string Scenario => "rejected";

    async Task Because() => await Execute();

    [Fact] void should_execute_the_specification() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_not_refuse_the_other_slices_publication() => _result.Unsupported.ShouldBeNull();
}
