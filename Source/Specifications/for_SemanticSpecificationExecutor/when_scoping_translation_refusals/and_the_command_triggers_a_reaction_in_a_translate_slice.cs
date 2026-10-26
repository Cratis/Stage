// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_scoping_translation_refusals;

// Screenplay decision 0115: the Translate slice's reaction belongs to another slice, so it does not run and does not refuse this slice's specification.
public class and_the_command_triggers_a_reaction_in_a_translate_slice : given.a_translated_application
{
    protected override string Scenario => "translated-reaction";

    async Task Because() => await Execute();

    [Fact] void should_run_the_specification() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_not_refuse_the_other_slices_publication() => _result.Unsupported.ShouldBeNull();
}
