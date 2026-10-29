// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_encoding;

public class and_an_authored_body_starts_with_a_byte_order_mark : given.a_body_planner
{
    const string Body = "\uFEFFbody";

    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanBody(new() { ["templates/index.sbn"] = "[{{ impl.body 'r1' }}]" }, Requirement("r1", Body), Body);

    [Fact] void should_keep_the_mark() => _plan.Artifacts.Single(_ => _.RelativePath == "Index.test").Bytes.ToArray().ShouldEqual(System.Text.Encoding.UTF8.GetBytes("[" + Body + "]"));
}
