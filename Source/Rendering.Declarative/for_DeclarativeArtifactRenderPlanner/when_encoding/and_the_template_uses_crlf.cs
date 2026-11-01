// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_encoding;

public class and_the_template_uses_crlf : given.a_body_planner
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanBody(new() { ["templates/index.sbn"] = "a\r\nb\rc\n" }, null, null);

    [Fact] void should_emit_lf() => Text(_plan, "Index.test").ShouldEqual("a\nb\nc\n");
}
