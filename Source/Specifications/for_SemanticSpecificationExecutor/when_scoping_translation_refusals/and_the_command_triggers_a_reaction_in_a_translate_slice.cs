// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_scoping_translation_refusals;

public class and_the_command_triggers_a_reaction_in_a_translate_slice : given.a_translated_application
{
    protected override string Scenario => "translated-reaction";

    async Task Because() => await Execute();

    [Fact] void should_refuse_translation() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_preserve_the_translation_diagnostic() => _result.Unsupported!.Details.StartsWith("STAGE-ESM-024:", StringComparison.Ordinal).ShouldBeTrue();
}
