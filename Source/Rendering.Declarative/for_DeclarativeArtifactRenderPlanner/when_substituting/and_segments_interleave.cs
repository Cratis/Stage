// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_substituting;

public class and_segments_interleave : given.a_declarative_planner
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanWith(new() { ["templates/index.sbn"] = "a{{ sym.decl 'D' 'X' }}b{{ lit.string 'q' }}c{{ sym.ref 'E' 'Y' }}d" });

    [Fact] void should_keep_the_exact_bytes() => Text(_plan, "Index.test").ShouldEqual("aXb\"q\"cYd");
    [Fact] void should_encode_without_a_byte_order_mark() => _plan.Artifacts.Single(_ => _.RelativePath == "Index.test").Bytes[0].ShouldEqual((byte)'a');
}
