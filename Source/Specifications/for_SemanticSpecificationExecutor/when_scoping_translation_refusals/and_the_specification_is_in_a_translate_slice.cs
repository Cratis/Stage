// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_scoping_translation_refusals;

public class and_the_specification_is_in_a_translate_slice : given.a_translated_application
{
    protected override string Scenario => "translate";

    async Task Because() => await Execute();

    [Fact] void should_execute_the_supported_shape() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_have_no_translation_refusal() => _result.Unsupported.ShouldBeNull();
}
