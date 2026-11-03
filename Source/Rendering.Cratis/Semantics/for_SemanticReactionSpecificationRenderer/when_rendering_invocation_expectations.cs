// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer;

public class when_rendering_invocation_expectations : given.an_invocation_expectation
{
    void Because()
    {
        ValidateExpectations();
        Render();
    }

    [Fact] void should_admit_the_v34_golden() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_compare_the_recorded_count() => _source.ShouldContain("should_match_authored_invocation_count() => _reactor.Produced.Count.ShouldEqual(1)");
    [Fact] void should_compare_the_recorded_command_type() => _source.ShouldContain("should_match_authored_invocation_1_command()");
    [Fact] void should_compare_the_concept_value() => _source.ShouldContain("should_match_authored_invocation_1_invoice_id()");
    [Fact] void should_compare_the_stated_reason() => _source.ShouldContain("should_match_authored_invocation_1_reason()");
    [Fact] void should_use_the_returned_command() => _source.ShouldContain("_reactor.Produced.ElementAtOrDefault(0) is CloseInvoice command");
    [Fact] void should_keep_foreign_decisions_without_foreign_facts() => _source.ShouldNotContain("should_append_event_1");
}
#endif
