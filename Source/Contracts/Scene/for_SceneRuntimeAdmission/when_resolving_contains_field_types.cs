// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SceneRuntimeAdmission;

public class when_resolving_contains_field_types : Specification
{
    [Theory]
    [InlineData("String", true)]
    [InlineData("String[]", false)]
    [InlineData("Int", false)]
    [InlineData("Bool", false)]
    [InlineData("Object", false)]
    [InlineData("Unknown", false)]
    [InlineData("Dynamic", false)]
    void should_apply_declared_types_to_actions_and_interactions(string type, bool supported)
    {
        foreach (var action in new[] { false, true })
        {
            var scene = new ScreenplaySceneVisitor().Visit(given.contains_guard_application.Create(type, action, "open"));
            if (supported)
            {
                scene.RuntimeIssues.ShouldBeEmpty();
            }
            else
            {
                scene.RuntimeIssues.Select(issue => issue.Code).ShouldContainOnly(action ? UnsupportedGuardedScreenAction.DiagnosticCode : UnsupportedGuardedInteraction.DiagnosticCode);
            }
        }
    }

    [Fact]
    void should_resolve_query_result_fields()
    {
        var syntax = given.contains_guard_application.Create("String", false, "open");
        var module = syntax.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var at = syntax.Location;
        slice = slice with { Queries = [new QuerySyntax("FindSubject", new("GuardSubject", false, false, at), null, [], null, at)] };
        syntax = syntax with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] };
        new SceneGuardFieldTypes(syntax).IsStringField("FindSubject", "item.value").ShouldBeTrue();
    }
}
