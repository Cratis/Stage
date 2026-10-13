// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

// A guarded production appends only when its condition holds; an unconditional handler would always append.
public class with_a_production_guard : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace(
        "        produces InvoiceIssued\n          for streamReference\n          description = description\n",
        "        produces when description != \"skip\"\n          InvoiceIssued\n            for streamReference\n            description = description\n",
        StringComparison.Ordinal));

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_render_the_text_condition() => Slice.ShouldContain("!global::System.Object.Equals(Description, \"skip\")");
    [Fact] void should_return_a_typed_collection() => Slice.ShouldContain("IEnumerable<global::Cratis.Chronicle.EventSequences.EventForEventSourceId> Handle()");
    [Fact] void should_guard_the_append() => Slice.ShouldContain("if (stageProduces0)");

    string Slice => System.Text.Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Issue.cs", StringComparison.Ordinal)).Bytes.AsSpan());
}
