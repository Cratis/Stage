// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_conditional_generated_productions : Specification
{
    ArtifactRenderPlan _plan = null!;
    string Slice => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Start.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    internal static string Source => string.Join('\n', GeneratedValuesCorpus.V15.SourceForms.Single(form => form.Name == "single").Documents.Select(document => document.Text));

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(Source.Replace("enabled == true", "receipt == \"ABCDEFGHIJKLMNOPQRSTU\" and enabled == true", StringComparison.Ordinal)));

    [Fact] void should_admit_the_generated_operand() => Assert.True(_plan.Success, string.Join('\n', _plan.Diagnostics));
    [Fact] void should_read_the_generated_handle_parameter() => Slice.ShouldContain("object.Equals(receipt.Value, \"ABCDEFGHIJKLMNOPQRSTU\")");
    [Fact] void should_guard_freshness_by_taken_productions() => Slice.ShouldContain("stageProduces0 || stageProduces1 ? new EventSourceId[]");
    [Fact] void should_return_only_selected_events() => Slice.ShouldContain("EventsWithConcurrencyScopes(stageEvents,");
    [Fact] void should_assert_that_skips_append_nothing() => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("when_skipped_production.cs", StringComparison.Ordinal)).Bytes.AsSpan()).ShouldContain("void should_append_no_new_events()");
}
