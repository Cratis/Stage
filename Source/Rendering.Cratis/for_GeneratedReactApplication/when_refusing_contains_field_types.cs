// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Scene.for_SceneRuntimeAdmission.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

public class when_refusing_contains_field_types : given.a_screen_composition_render
{
    [Fact]
    void should_render_contains_for_a_known_string_field_on_both_surfaces()
    {
        foreach (var action in new[] { false, true })
        {
            var scene = new ScreenplaySceneVisitor().Visit(contains_guard_application.Create("String", action, "open"));
            var plan = Plan(CanonicalSceneJson.Serialize(scene));
            plan.Success.ShouldBeTrue();
            plan.Diagnostics.ShouldBeEmpty();
            plan.Artifacts.Any().ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData("String[]", "open")]
    [InlineData("Int", "2")]
    [InlineData("Bool", "true")]
    [InlineData("Object", "open")]
    [InlineData("Unknown", "open")]
    [InlineData("Dynamic", "open")]
    [InlineData("String", 1)]
    [InlineData("String", true)]
    [InlineData("String", null)]
    void should_refuse_each_form_before_producing_any_artifacts(string fieldType, object? literal)
    {
        foreach (var action in new[] { false, true })
        {
            var scene = new ScreenplaySceneVisitor().Visit(contains_guard_application.Create(fieldType, action, literal));
            var code = action ? UnsupportedGuardedScreenAction.DiagnosticCode : UnsupportedGuardedInteraction.DiagnosticCode;
            scene.RuntimeIssues.Select(issue => issue.Code).ShouldContainOnly(code);
            var plan = Plan(CanonicalSceneJson.Serialize(scene));
            plan.Success.ShouldBeFalse();
            plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);
            plan.Artifacts.ShouldBeEmpty();
        }
    }
}
