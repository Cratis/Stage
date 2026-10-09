// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Elements;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

public class and_a_package_component_declares_rich_metadata : Specification
{
    ExternalComponent _result = null!;

    void Establish()
    {
        var component = new ScreenComponentSyntax("scene.web.DataGrid", "workItems", SourceLocation.Start)
        {
            Context = new(UiBindingKind.QueryResult, string.Empty, SourceLocation.Start)
            {
                Query = "AllWorkItems"
            },
            Properties =
            [
                new("title", null, new LiteralExpressionSyntax("Work items", SourceLocation.Start), SourceLocation.Start),
                new(
                    "selectedItem",
                    new UiBindingSyntax(UiBindingKind.ComponentProperty, "selectedItem.workItemId", SourceLocation.Start)
                    {
                        ComponentId = "work-items:list",
                        ComponentPropertyPath = "selectedItem.workItemId",
                        NullBehavior = UiBindingNullBehavior.Clear
                    },
                    null,
                    SourceLocation.Start)
            ],
            Exposes =
            [
                new(
                    "selectedWorkItem",
                    new UiBindingSyntax(UiBindingKind.ComponentProperty, "selectedItem.workItemId", SourceLocation.Start)
                    {
                        ComponentId = "workItems",
                        ComponentPropertyPath = "selectedItem.workItemId"
                    },
                    SourceLocation.Start)
            ],
            Presentation =
            [
                new("density", "compact", SourceLocation.Start)
            ],
            Icon = "table"
        };

        _result = (ExternalComponent)ScreenDirectiveConverter.Convert([component], "WorkItemList")[0];
    }

    [Fact] void should_use_the_authored_component_name_as_the_stable_scene_id() => _result.Id.ShouldEqual("workItems");
    [Fact] void should_carry_the_component_identity() => _result.ComponentName.ShouldEqual("scene.web.DataGrid");
    [Fact] void should_carry_the_query_context_binding() => ((BindingExpression)_result.Properties["context"]!).Kind.ShouldEqual(BindingSourceKind.QueryResult);
    [Fact] void should_carry_literal_property_values() => _result.Properties["title"].ShouldEqual("Work items");
    [Fact] void should_carry_component_property_bindings() => ((BindingExpression)_result.Properties["selectedItem"]!).ComponentId.ShouldEqual("work-items:list");
    [Fact] void should_carry_binding_null_behavior() => ((BindingExpression)_result.Properties["selectedItem"]!).NullBehavior.ShouldEqual(BindingNullBehavior.Clear);
    [Fact] void should_carry_presentation_values() => ((IReadOnlyDictionary<string, string>)_result.Properties["presentation"]!)["density"].ShouldEqual("compact");
    [Fact] void should_carry_exposed_values() => ((IReadOnlyDictionary<string, BindingExpression>)_result.Properties["exposes"]!)["selectedWorkItem"].ComponentPropertyPath.ShouldEqual("selectedItem.workItemId");
    [Fact] void should_carry_the_icon() => _result.Properties["icon"].ShouldEqual("table");
}
