// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplaySceneVisitor.given;

/// <summary>Authored attachment sites, independent of Stage's guard resolution API.</summary>
public static class guard_context_application
{
    static readonly SourceLocation _at = new(7, 5, "guards.play");

    /// <summary>Creates a guard at the requested attachment site.</summary>
    /// <param name="site">The attachment scenario.</param>
    /// <param name="operation">The authored comparison.</param>
    /// <returns>The syntax to translate.</returns>
    public static ApplicationSyntax Create(string site, ComparisonOperator operation)
    {
        var path = site switch { "nested" or "nested-enumeration" => "item.address.note", "unknown" => "item.missing", _ => "item.note" };
        var action = Action(path, operation);
        var text = Model("TextSubject", "String");
        var number = Model("NumberSubject", "Int");
        var parent = new ReadModelSyntax("Parent", [Property("note", "Int"), Property("rows", "Row", true), Property("address", "Address")], _at);
        var row = Model("Row", "String");
        var screen = new ScreenSyntax("Guards", null, [Data("TextSubject"), action], _at);
        var slice = Slice("Owner", screen, [text, number, parent, row]);
        var module = new ModuleSyntax("First", [], [new FeatureSyntax("Feature", [], [slice], _at)], _at);
        var application = new ApplicationSyntax([], [], [], [module], _at);
        switch (site)
        {
            case "nested":
            case "nested-enumeration":
                screen = screen with { Directives = [Data("Parent"), action] };
                application = application with
                {
                    Types = [new TypeSyntax("Address", [Property("note", site == "nested" ? "String" : "Status")], _at)],
                    Concepts = [new ConceptSyntax("Status", "Enum", [], ["open", "closed"], _at)]
                };
                break;
            case "row-enumeration":
                slice = slice with { ReadModels = [parent, Model("Row", "Status")] };
                application = application with { Concepts = [new ConceptSyntax("Status", "Enum", [], ["open", "closed"], _at)] };
                screen = screen with { Directives = [Data("Parent"), Table("rows", operation)] };
                break;
            case "row":
            case "mixed-row":
                screen = screen with
                {
                    Directives = site == "row"
                        ? [Data("Parent"), Table("rows", operation)]
                        : [Data("Parent"), Data("Row", true), Table("Row", operation)]
                };
                break;
            case "template":
                module = module with
                {
                    ScreenTemplates = [new ScreenTemplateSyntax("Workspace", [], _at)
                    {
                        Content = [new TemplateSlotContentSyntax("first", [Data("TextSubject"), action], _at), new TemplateSlotContentSyntax("second", [Data("NumberSubject"), Action("item.note", operation)], _at)]
                    }]
                };
                screen = screen with { Directives = [] };
                break;
            case "outlets":
                screen = screen with
                {
                    Directives = [Data("NumberSubject"), new ScreenComponentSyntax("core.section", "host", _at)
                    {
                        Outlets = [new ComponentOutletSyntax("first", [Data("TextSubject"), action], _at), new ComponentOutletSyntax("second", [Action("item.note", operation)], _at)]
                    }]
                };
                break;
            case "contribution":
            case "isolated-contribution":
                screen = screen with
                {
                    Directives = [Data("TextSubject")],
                    Contributions = [new ScreenContributionSyntax("actions", null, site == "contribution" ? [Data("TextSubject"), action] : [action], _at)]
                };
                break;
            case "reused":
                var behavior = Behavior(operation) with { Name = "Shared" };
                application = application with { Behaviors = [behavior] };
                screen = screen with
                {
                    Directives = [new ScreenSectionSyntax("first", [Data("TextSubject"), Use()], _at), new ScreenSectionSyntax("second", [Data("NumberSubject"), Use()], _at)]
                };
                break;
            case "duplicate-slice":
                slice = slice with { ReadModels = [Model("TextSubject", "Int")] };
                module = module with { Features = [new FeatureSyntax("Feature", [], [Slice("Other", null, [text]), slice], _at)] };
                break;
            case "duplicate-module":
            case "qualified":
                slice = slice with { ReadModels = [Model("TextSubject", "Int")] };
                var other = new ModuleSyntax("Second", [], [new FeatureSyntax("Feature", [], [Slice("Other", null, [text])], _at)], _at);
                application = application with { Modules = [other, module] };
                if (site == "qualified") screen = screen with { Directives = [Data("Second.Feature.Other.TextSubject"), action] };
                break;
            case "number":
                screen = screen with { Directives = [Data("NumberSubject"), action] };
                break;
            case "enumeration":
                slice = slice with { ReadModels = [Model("TextSubject", "Status")] };
                application = application with { Concepts = [new ConceptSyntax("Status", "Enum", [], ["open", "closed"], _at)] };
                break;
            case "ambiguous":
                screen = screen with { Directives = [Data("TextSubject"), Data("NumberSubject"), action] };
                break;
        }

        slice = slice with { Screens = [screen] };
        var feature = module.Features.Single();
        module = module with { Features = [feature with { Slices = [.. feature.Slices.Select(candidate => candidate.Name == "Owner" ? slice : candidate)] }] };
        return application with { Modules = [.. application.Modules.Select(candidate => candidate.Name == "First" ? module : candidate)] };
    }

    static ScreenDataSyntax Data(string name, bool collection = false) => new(new(name, collection, false, _at), name, null, _at);

    static PropertySyntax Property(string name, string type, bool collection = false) => new(name, new(type, collection, false, _at), _at);

    static ReadModelSyntax Model(string name, string type) => new(name, [Property("note", type)], _at);

    static SliceSyntax Slice(string name, ScreenSyntax? screen, ReadModelSyntax[] models) =>
        new(global::Cratis.Screenplay.Syntax.SliceType.StateView, name, [], [], [], [], [], [], screen is null ? [] : [screen], [], [], _at, ReadModels: models);

    static ScreenGuardedActionSyntax Action(string path, ComparisonOperator operation) =>
        new("Apply", [new ScreenActionAlternativeSyntax(Condition(path, operation), "Apply", _at)], _at);

    static ComparisonConditionSyntax Condition(string path, ComparisonOperator operation) =>
        new(path, operation, new LiteralExpressionSyntax("open", _at), _at);

    static BehaviorSyntax Behavior(ComparisonOperator operation) => new(
        null,
        [],
        [new InteractionBindingSyntax(new BuiltInInteractionTriggerSyntax(InteractionTriggerKind.DoubleClick, _at), null, [], _at)
        {
            Alternatives = [new InteractionAlternativeSyntax(Condition("item.note", operation), [new NavigateActionSyntax("Details", _at)], _at)]
        }],
        _at);

    static ScreenTableSyntax Table(string target, ComparisonOperator operation) => new(target, [], null, _at) { Behaviors = [Behavior(operation)] };

    static ScreenUsesBehaviorSyntax Use() => new(new UsesBehaviorSyntax("Shared", [], _at), _at);
}
