// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_encoding;

public class and_an_authored_body_mixes_crlf_and_lf : given.a_body_planner
{
    const string Body = "one\r\ntwo\nthree\r";

    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanBody(new() { ["templates/index.sbn"] = "x\r\n{{ impl.body 'r1' }} tail\r\n" }, Requirement("r1", Body), Body);

    [Fact] void should_normalize_the_generated_text_only() => Text(_plan, "Index.test").ShouldEqual("x\n" + Body + " tail\n");
    [Fact] void should_keep_the_body_bytes() => _plan.Artifacts.Single(_ => _.RelativePath == "Index.test").Bytes.ToArray().ShouldEqual(System.Text.Encoding.UTF8.GetBytes("x\n" + Body + " tail\n"));
    [Fact] void should_have_no_diagnostics() => _plan.Diagnostics.ShouldBeEmpty();
}
