// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_only_its_own_slice;

public class and_the_stated_invocation_differs : given.an_invoicing_application
{
    const string Failure = "Invocation at index 0 does not match the expected command and values. Expected: CloseInvoice(reason = \"cancelled\"). Actual: CloseInvoice(id = \"one\", reason = \"paid\").";

    void Establish() => Compile("when append InvoicePaid\n  for \"one\"\n  id = \"one\"\nthen invokes CloseInvoice\n  reason = \"cancelled\"");

    Task Because() => Run("Collecting");

    [Fact] void should_fail() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_name_the_invocation_and_both_values() => _result.Failures.ShouldContainOnly([Failure]);
    [Fact] void should_fail_as_the_reference_does() => _reference.Failures.ShouldContainOnly([Failure]);
}
