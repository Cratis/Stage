// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

/// <summary>
/// Planning the screen-composition corpus with its guarded Close action given a guard the Stage runtime cannot
/// evaluate - compared with another field instead of a literal.
/// </summary>
/// <remarks>
/// A guard the runtime cannot evaluate must not reach a generated application, and a refused plan must leave
/// nothing behind: no partial application a caller could publish.
/// </remarks>
public class when_planning_the_screen_composition_with_its_guarded_actions : a_screen_composition_render
{
    ArtifactRenderPlan _plan = null!;
    string _authored = null!;

    void Because()
    {
        _authored = CanonicalSceneJson.Serialize(_scene);
        _plan = Plan(_authored.Replace("\"right\":\"open\"", "\"right\":{\"kind\":\"PathExpressionSyntax\"}", StringComparison.Ordinal));
    }

    [Fact] void should_change_the_authored_close_guard() => _authored.ShouldContain("\"right\":\"open\"");
    [Fact] void should_report_the_guarded_screen_action() => _plan.Diagnostics.Select(_ => _.Code).ShouldContainOnly(UnsupportedGuardedScreenAction.DiagnosticCode);
    [Fact] void should_not_succeed() => _plan.Success.ShouldBeFalse();
    [Fact] void should_plan_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
