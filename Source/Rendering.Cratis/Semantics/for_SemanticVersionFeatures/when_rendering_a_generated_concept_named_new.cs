// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_rendering_a_generated_concept_named_new : Specification
{
    ArtifactRenderPlan _plan = null!;
    string Concept => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/New.cs", StringComparison.Ordinal) || artifact.RelativePath == "New.cs").Bytes.AsSpan());

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(when_admitting_routed_generated_values.Source.Replace("Receipt", "New", StringComparison.Ordinal)));

    [Fact] void should_render_the_collision() => Assert.True(_plan.Success, string.Join('\n', _plan.Diagnostics));
    [Fact] void should_keep_the_concept_name() => Concept.ShouldContain("record New(");
    [Fact] void should_implement_the_factory_explicitly() => Concept.ShouldContain("IGeneratableInContext<New>.New(");
    [Fact] void should_not_declare_a_public_factory() => Concept.ShouldNotContain("public static New New(");
}
