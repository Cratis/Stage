// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

public class when_rendering_guard_attachment_contexts : given.a_screen_composition_render
{
    [Theory]
    [InlineData("nested", "Contains", true)]
    [InlineData("nested", "StartsWith", true)]
    [InlineData("row", "Contains", true)]
    [InlineData("mixed-row", "Contains", true)]
    [InlineData("duplicate-slice", "Contains", false)]
    [InlineData("duplicate-module", "Contains", false)]
    [InlineData("qualified", "Contains", true)]
    [InlineData("number", "StartsWith", false)]
    [InlineData("unknown", "Equal", false)]
    [InlineData("ambiguous", "Equal", false)]
    public void should_only_produce_artifacts_for_resolved_guard_subjects(string site, string operation, bool supported)
    {
        var syntax = Contracts.Scene.for_ScreenplaySceneVisitor.given.guard_context_application.Create(site, Enum.Parse<ComparisonOperator>(operation));
        var scene = new ScreenplaySceneVisitor().Visit(syntax);
        var plan = Plan(CanonicalSceneJson.Serialize(scene));

        plan.Success.ShouldEqual(supported);
        if (supported)
        {
            scene.RuntimeIssues.ShouldBeEmpty();
            plan.Diagnostics.ShouldBeEmpty();
            plan.Artifacts.ShouldNotBeEmpty();
        }
        else
        {
            scene.RuntimeIssues.ShouldNotBeEmpty();
            plan.Artifacts.ShouldBeEmpty();
        }
    }
}
