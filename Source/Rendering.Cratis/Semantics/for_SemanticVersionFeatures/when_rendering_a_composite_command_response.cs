// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_rendering_a_composite_command_response : given.a_composite_command_response
{
    ArtifactRenderPlan _plan = null!;
    string Code => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Move.cs", StringComparison.Ordinal)).Bytes.AsSpan());

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(Composite));

    [Fact] void should_render_the_composite_response() => Assert.True(_plan.Success, string.Join('\n', _plan.Diagnostics));
    [Fact] void should_return_the_composite_value_with_its_event() => Assert.Matches(@"\((global::[\w.]+\.)?Address, [^()]+\) Handle\(", Code);
}
