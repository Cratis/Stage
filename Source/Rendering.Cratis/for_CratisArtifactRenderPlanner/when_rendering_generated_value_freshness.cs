// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_generated_value_freshness : Specification
{
    ArtifactRenderPlan _plan = null!;
    string Slice => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Start.cs", StringComparison.Ordinal)).Bytes.AsSpan());

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(when_rendering_conditional_generated_productions.Source));

    [Fact] void should_admit_the_generated_values() => Assert.True(_plan.Success, string.Join('\n', _plan.Diagnostics));
    [Fact] void should_leave_freshness_to_the_rendered_scopes_instead_of_arcs_guard() => Slice.ShouldContain("Freshness = GeneratedValueFreshness.Unguarded)]");
    [Fact] void should_keep_the_rendered_freshness_scopes() => Slice.ShouldContain("EventsWithConcurrencyScopes(");
}
