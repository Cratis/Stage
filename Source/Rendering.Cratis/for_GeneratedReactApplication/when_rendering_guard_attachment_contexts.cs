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
    [Fact]
    public void should_render_a_table_with_a_guard_on_a_uuid_row_field()
    {
        var syntax = Contracts.Scene.for_ScreenplaySceneVisitor.given.guard_context_application.Create("row", ComparisonOperator.Equal);
        var module = syntax.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var screen = slice.Screens.Single();
        var table = screen.Directives.OfType<ScreenTableSyntax>().Single();
        var behavior = table.Behaviors.Single();
        var binding = behavior.Bindings.Single();
        var alternative = binding.Alternatives.Single();
        var condition = (ComparisonConditionSyntax)alternative.Condition!;
        condition = condition with { Right = new LiteralExpressionSyntax("c1df4b56-5481-4e75-8558-03d894aeb512", condition.Location) };
        binding = binding with { Alternatives = [alternative with { Condition = condition }] };
        table = table with { Behaviors = [behavior with { Bindings = [binding] }] };
        screen = screen with { Directives = [screen.Directives.OfType<ScreenDataSyntax>().Single(), table] };
        slice = slice with
        {
            Screens = [screen],
            ReadModels = [.. slice.ReadModels.Select(model => model.Name == "Row"
                ? model with { Properties = [.. model.Properties.Select(property => property with { Type = property.Type with { Name = "Uuid" } })] }
                : model)]
        };
        syntax = syntax with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] };

        var scene = new ScreenplaySceneVisitor().Visit(syntax);
        var plan = Plan(CanonicalSceneJson.Serialize(scene));

        scene.RuntimeIssues.ShouldBeEmpty();
        plan.Success.ShouldBeTrue();
        plan.Diagnostics.ShouldBeEmpty();
        plan.Artifacts.ShouldNotBeEmpty();
    }

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
