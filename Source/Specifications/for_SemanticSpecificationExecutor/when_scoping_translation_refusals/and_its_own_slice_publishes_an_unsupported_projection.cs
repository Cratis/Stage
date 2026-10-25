// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_scoping_translation_refusals;

// The publishing slice's own specification runs its outbound projection, so the unsupported shape still refuses it, with the slice's refusal.
public class and_its_own_slice_publishes_an_unsupported_projection : given.a_translated_application
{
    protected override string Scenario => "own-command";

    async Task Because() => await Execute();

    [Fact] void should_refuse_translation() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_preserve_the_translation_diagnostic() => _result.Unsupported!.Details.StartsWith("STAGE-ESM-024:", StringComparison.Ordinal).ShouldBeTrue();
}
