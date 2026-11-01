// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_only_its_own_slice;

// The command another slice owns is still decided: its constraint refuses it and the reaction's refusal branch runs.
public class and_the_other_slices_command_refuses : given.an_invoicing_application
{
    void Establish() => Compile("given InvoiceClosed\n  for \"one\"\n  reason = \"earlier\"\nwhen append InvoicePaid\n  for \"one\"\n  id = \"one\"\nthen ClosingRefused\n  for \"one\"\n  message = \"Already closed\"");

    Task Because() => Run("Collecting");

    [Fact] void should_pass() => Assert.True(_result.Outcome == SemanticSpecificationOutcome.Passed, string.Join(';', _result.Failures));
    [Fact] void should_agree_with_the_reference() => Assert.True(_reference.Passed, string.Join(';', _reference.Failures));
    [Fact] void should_append_the_given_the_action_and_the_refusal_branch() => _worldFacts.ShouldEqual(3);
}
