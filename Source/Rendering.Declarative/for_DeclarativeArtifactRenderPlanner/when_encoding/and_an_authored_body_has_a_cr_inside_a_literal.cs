// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_encoding;

public class and_an_authored_body_has_a_cr_inside_a_literal : given.a_body_planner
{
    const string Body = "var s = \"a\rb\";";
    const string Template = "{{ if item.name == 'Echo' }}{{ impl.body 'r1' }}{{ else }}ok{{ end }}";

    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanBody(new() { ["templates/command.sbn"] = Template }, Requirement("r1", Body), Body);

    [Fact] void should_emit_the_placeholder() => Text(_plan, "Commands/Echo.test").ShouldEqual("/* ThrowingOperationBody STAGE-DEGRADE-003 */");
    [Fact] void should_still_render_the_other_artifacts() => Text(_plan, "Commands/Shout.test").ShouldEqual("ok");
    [Fact] void should_report_an_inline_mismatch_warning() => _plan.Diagnostics.Select(_ => (_.Code, _.Severity)).ShouldEqual([(ArtifactRenderDegradationCodes.InlineMismatch, ArtifactRenderDiagnosticSeverity.Warning)]);
    [Fact] void should_name_the_language_code_in_the_remedy() => _plan.Degradations.Single().Remedy.ShouldContain(given.a_test_language.UnsupportedBody);
    [Fact] void should_not_refuse_the_plan() => _plan.Artifacts.Length.ShouldEqual(4);
}
