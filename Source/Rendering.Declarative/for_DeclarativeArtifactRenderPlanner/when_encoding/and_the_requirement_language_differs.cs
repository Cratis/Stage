// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_encoding;

public class and_the_requirement_language_differs : given.a_body_planner
{
    const string Template = "{{ if item.name == 'Echo' }}{{ impl.body 'r1' }}{{ else }}ok{{ end }}";

    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanBody(new() { ["templates/command.sbn"] = Template }, Requirement("r1", "body", language: "other"), "body");

    [Fact] void should_emit_the_placeholder() => Text(_plan, "Commands/Echo.test").ShouldEqual("/* ThrowingOperationBody STAGE-DEGRADE-003 */");
    [Fact] void should_report_an_inline_mismatch_warning() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([ArtifactRenderDegradationCodes.InlineMismatch]);
}
