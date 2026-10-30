// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_only_its_own_slice;

public class and_a_stated_invocation_is_not_made : given.an_invoicing_application
{
    void Establish() => Compile("when append InvoicePaid\n  for \"one\"\n  id = \"one\"\nthen invokes CloseInvoice\n  reason = \"paid\"\nthen invokes CloseInvoice\n  reason = \"paid\"");

    Task Because() => Run("Collecting");

    [Fact] void should_fail() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_fail_with_the_references_count_message() => _result.Failures.ShouldContainOnly(_reference.Failures);
    [Fact] void should_name_the_count() => _result.Failures.Single().StartsWith("Expected 2 invocation(s), got 1.", StringComparison.Ordinal).ShouldBeTrue();
}
