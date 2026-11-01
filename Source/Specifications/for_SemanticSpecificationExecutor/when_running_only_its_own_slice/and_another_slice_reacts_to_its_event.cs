// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_only_its_own_slice;

public class and_another_slice_reacts_to_its_event : given.an_invoicing_application
{
    void Establish() => Compile("when append InvoicePaid\n  for \"one\"\n  id = \"one\"\nthen no events");

    Task Because() => Run("Registering");

    [Fact] void should_pass() => Assert.True(_result.Outcome == SemanticSpecificationOutcome.Passed, string.Join(';', _result.Failures));
    [Fact] void should_agree_with_the_reference() => Assert.True(_reference.Passed, string.Join(';', _reference.Failures));
    [Fact] void should_not_run_the_other_slices_reaction() => _result.Trace!.Facts.ShouldNotContain(fact => fact.EventContract == Event("InvoiceWelcomed"));

    string Event(string name) => _plan.Events.Values.Single(@event => @event.Name == name).Id.ToString();
}
