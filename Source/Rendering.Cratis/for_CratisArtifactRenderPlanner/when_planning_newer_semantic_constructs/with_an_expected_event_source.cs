// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_an_expected_event_source : given.an_invoice_model
{
    string _assertion = null!;

    void Because()
    {
        Plan(Invoices.Replace(FirstExpectation, "then InvoiceIssued\n          for \"invoice-stated\"\n          description = \"First payload\"", StringComparison.Ordinal));
        _assertion = Artifact("when_issuing_first_invoice.cs").Split('\n').Single(_ => _.Contains("ShouldHaveAppendedEvent", StringComparison.Ordinal));
    }

    [Fact] void should_compile_an_esm_v2_model() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V2);
    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_assert_the_event_on_the_stated_stream() => _assertion.ShouldContain("\"invoice-stated\"");
    [Fact] void should_not_substitute_the_command_destination() => _assertion.ShouldNotContain("\"invoice-one\"");
}
