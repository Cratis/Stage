// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_encoding;

public class and_the_body_is_missing : given.a_body_planner
{
    const string Template = "{{ if item.name == 'Echo' }}{{ impl.body 'r1' }}{{ else }}ok{{ end }}";

    ArtifactRenderPlan _unknown = null!;
    ArtifactRenderPlan _noContent = null!;
    ArtifactRenderPlan _unresolved = null!;

    void Because()
    {
        _unknown = PlanBody(new() { ["templates/command.sbn"] = Template }, null, null);
        _noContent = PlanBody(new() { ["templates/command.sbn"] = Template }, Requirement("r1", "body"), null);
        _unresolved = PlanBody(new() { ["templates/command.sbn"] = Template }, Requirement("r1", "body", hash: string.Empty), "body");
    }

    [Fact] void should_degrade_an_unknown_requirement() => Text(_unknown, "Commands/Echo.test").ShouldEqual("/* ThrowingOperationBody STAGE-DEGRADE-003 */");
    [Fact] void should_degrade_a_requirement_without_content() => Text(_noContent, "Commands/Echo.test").ShouldEqual("/* ThrowingOperationBody STAGE-DEGRADE-003 */");
    [Fact] void should_emit_nothing_for_an_unresolved_file() => Text(_unresolved, "Commands/Echo.test").ShouldEqual("/* ThrowingOperationBody STAGE-DEGRADE-003 */");
    [Fact] void should_report_one_warning_each() => new[] { _unknown, _noContent, _unresolved }.Select(_ => _.Diagnostics.Length).ShouldEqual([1, 1, 1]);
}
