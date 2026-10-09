// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneCommon = Cratis.Scene.Model.Common;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneInteractions = Cratis.Scene.Model.Interactions;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a Screenplay screen's <see cref="ScreenplaySyntax.ScreenDirectiveSyntax"/> tree into
/// <see cref="SceneElements.SceneElement"/>s - part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// Every directive kind (including <c language="csharp">data</c>, which is not itself visual) becomes one
/// <see cref="SceneElements.ExternalComponent"/> named <c language="csharp">core:&lt;directive-kind&gt;</c>, with the
/// directive's own fields folded into its open <c language="csharp">Properties</c> bag and any nested directives placed in
/// its <c language="csharp">Slots["content"]</c>. This is a deliberately mechanical, uniform mapping rather than a bespoke
/// <c language="csharp">SceneElement</c> subtype per widget - the <c language="csharp">core:*</c> components don't have to exist in
/// <c language="csharp">Scene.React</c> yet for the translated model to be correct; rendering them is a separate, later
/// concern (a <c language="csharp">core</c> package addition), the same reasoning Cratis/Scene#5 used to leave
/// <c language="csharp">Scene.React</c> untouched. Guarded actions keep their alternatives and fallback in the
/// component's open property bag so a runtime can evaluate them without the converter choosing a branch.
/// </remarks>
public static class ScreenDirectiveConverter
{
    /// <summary>
    /// Converts a sequence of sibling <see cref="ScreenplaySyntax.ScreenDirectiveSyntax"/> into
    /// <see cref="SceneElements.SceneElement"/>s.
    /// </summary>
    /// <param name="directives">The sibling directives to convert.</param>
    /// <param name="path">The id path of the directives' parent, used to derive unique element ids.</param>
    /// <returns>The converted elements, in declaration order.</returns>
    /// <remarks>
    /// Behavior directives are skipped rather than converted. They share the screen body with content because
    /// that is where they are written, but they are what the content <em>does</em>, not more of it - they are
    /// collected as attachments by <see cref="ScreenConverter"/> instead.
    /// </remarks>
    public static IReadOnlyList<SceneElements.SceneElement> Convert(
        IEnumerable<ScreenplaySyntax.ScreenDirectiveSyntax> directives,
        string path) =>
        Convert(directives, path, null);

    /// <summary>
    /// Converts a screen's directives into Scene elements, collecting what is attached at this level.
    /// </summary>
    /// <param name="directives">The directives to convert.</param>
    /// <param name="path">The id path of the directives' parent, used to derive unique element ids.</param>
    /// <param name="behaviors">What a <c language="csharp">uses</c> clause resolves against.</param>
    /// <param name="attached">Where to collect what was attached at this level, when anything is listening.</param>
    /// <returns>The converted elements, in declaration order.</returns>
    public static IReadOnlyList<SceneElements.SceneElement> Convert(
        IEnumerable<ScreenplaySyntax.ScreenDirectiveSyntax> directives,
        string path,
        BehaviorScope? behaviors,
        ICollection<SceneInteractions.Behavior>? attached = null)
    {
        var all = directives.ToList();
        var scope = behaviors ?? BehaviorScope.None;

        // What was written at this level attaches to whatever encloses it - the section, or the screen when
        // nothing else does. Collecting it here rather than dropping it is the point: an interaction that
        // silently does not exist is worse than one that is reported.
        if (attached is not null)
        {
            foreach (var behavior in scope.Resolve(
                all.OfType<ScreenplaySyntax.ScreenBehaviorSyntax>().Select(directive => directive.Behavior),
                all.OfType<ScreenplaySyntax.ScreenUsesBehaviorSyntax>().Select(directive => directive.Uses),
                path))
            {
                attached.Add(behavior);
            }
        }

        return
        [
            .. all
                .Where(directive => directive is not ScreenplaySyntax.ScreenBehaviorSyntax and not ScreenplaySyntax.ScreenUsesBehaviorSyntax)
                .Select((directive, index) => Convert(directive, $"{path}.{index}-{Kind(directive)}", scope))
        ];
    }

    static SceneElements.ExternalComponent Convert(ScreenplaySyntax.ScreenDirectiveSyntax directive, string id, BehaviorScope scope) =>
        directive switch
        {
            ScreenplaySyntax.ScreenDataSyntax data => SceneElementFactory.Component(id, "core:data", new Dictionary<string, object?>
            {
                ["typeName"] = data.Type.Name,
                ["isCollection"] = data.Type.IsCollection,
                ["query"] = data.Query,
                ["by"] = data.By,
            }),
            ScreenplaySyntax.ScreenActionSyntax action => SceneElementFactory.Component(id, "core:action", new Dictionary<string, object?>
            {
                ["command"] = action.Command,
                ["label"] = action.Label,
                ["navigateToScreen"] = action.Navigate?.Screen,
                ["navigateByParameter"] = action.Navigate?.By,
            }),
            ScreenplaySyntax.ScreenGuardedActionSyntax action => ConvertGuardedAction(action, id),
            ScreenplaySyntax.ScreenSectionSyntax section => ConvertSection(section, id, scope),
            ScreenplaySyntax.ScreenNavigateSyntax navigate => SceneElementFactory.Component(id, "core:navigate", new Dictionary<string, object?>
            {
                ["targetScreen"] = navigate.Screen,
                ["by"] = navigate.By,
            }),
            ScreenplaySyntax.ScreenTitleSyntax title => SceneElementFactory.Component(id, "core:title", new Dictionary<string, object?> { ["text"] = title.Text }),
            ScreenplaySyntax.ScreenTableSyntax table => ConvertTable(table, id, scope),
            ScreenplaySyntax.ScreenSummarySyntax summary => ConvertSummary(summary, id),
            ScreenplaySyntax.ScreenToolbarSyntax toolbar => ConvertToolbar(toolbar, id),
            ScreenplaySyntax.ScreenComponentSyntax component => ConvertComponent(component, id, scope),
            ScreenplaySyntax.ScreenCodeSyntax code => SceneElementFactory.Component(id, "core:code", new Dictionary<string, object?>
            {
                ["language"] = code.Code.Language,
                ["code"] = code.Code.Code,
            }),
            _ => throw new UnknownScreenDirective(directive.GetType().Name),
        };

    static SceneElements.ExternalComponent ConvertToolbar(ScreenplaySyntax.ScreenToolbarSyntax toolbar, string id)
    {
        var items = toolbar.Items.Select((item, index) => (SceneElements.SceneElement)SceneElementFactory.Component(
            $"{id}.{index}-item",
            "core:toolbarItem",
            new Dictionary<string, object?>
            {
                ["name"] = item.Name,
                ["kind"] = item.Kind.ToString(),
                ["target"] = item.Target,
                ["label"] = item.Label,
                ["icon"] = item.Icon,
                ["parameters"] = item.Parameters.ToDictionary(parameter => parameter.Name, parameter => ConvertBinding(parameter.Binding), StringComparer.Ordinal),
                ["presentation"] = item.Presentation.ToDictionary(presentation => presentation.Name, presentation => presentation.Value, StringComparer.Ordinal),
            })).ToList();

        return SceneElementFactory.Component(id, "core:toolbar", new Dictionary<string, object?> { ["name"] = toolbar.Name }, new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>> { ["items"] = items });
    }

    static SceneElements.ExternalComponent ConvertComponent(ScreenplaySyntax.ScreenComponentSyntax component, string id, BehaviorScope scope)
    {
        var properties = new Dictionary<string, object?>
        {
            ["name"] = component.Name,
            ["context"] = component.Context is null ? null : ConvertBinding(component.Context),
            ["icon"] = component.Icon,
            ["presentation"] = component.Presentation.ToDictionary(presentation => presentation.Name, presentation => presentation.Value, StringComparer.Ordinal),
            ["exposes"] = component.Exposes.ToDictionary(exposed => exposed.Name, exposed => ConvertBinding(exposed.Binding), StringComparer.Ordinal),
        };

        foreach (var property in component.Properties)
        {
            properties[property.Property] = property.Binding is null ? property.Value : ConvertBinding(property.Binding);
        }

        var slots = component.Outlets.ToDictionary(
            outlet => outlet.Name,
            outlet => Convert(outlet.Directives, $"{id}.{outlet.Name}", scope),
            StringComparer.Ordinal);

        return SceneElementFactory.Component(id, component.Component, properties, slots) with
        {
            Behaviors = scope.Resolve(component.Behaviors, component.UsedBehaviors, id)
        };
    }

    static SceneCommon.BindingExpression ConvertBinding(ScreenplaySyntax.UiBindingSyntax binding) =>
        new(
            binding.Path,
            BindingKind(binding.BindingKind),
            binding.Query,
            binding.ComponentId,
            binding.ComponentPropertyPath,
            BindingMode(binding.Mode),
            BindingNullBehavior(binding.NullBehavior),
            binding.ExpectedValueType);

    static SceneCommon.BindingSourceKind BindingKind(ScreenplaySyntax.UiBindingKind kind) =>
        kind switch
        {
            ScreenplaySyntax.UiBindingKind.QueryResult => SceneCommon.BindingSourceKind.QueryResult,
            ScreenplaySyntax.UiBindingKind.ComponentProperty => SceneCommon.BindingSourceKind.ComponentProperty,
            _ => SceneCommon.BindingSourceKind.DataContext,
        };

    static SceneCommon.BindingMode? BindingMode(ScreenplaySyntax.UiBindingMode? mode) =>
        mode switch
        {
            ScreenplaySyntax.UiBindingMode.TwoWay => SceneCommon.BindingMode.TwoWay,
            ScreenplaySyntax.UiBindingMode.OneWay => SceneCommon.BindingMode.OneWay,
            _ => null,
        };

    static SceneCommon.BindingNullBehavior? BindingNullBehavior(ScreenplaySyntax.UiBindingNullBehavior? behavior) =>
        behavior switch
        {
            ScreenplaySyntax.UiBindingNullBehavior.Clear => SceneCommon.BindingNullBehavior.Clear,
            ScreenplaySyntax.UiBindingNullBehavior.Preserve => SceneCommon.BindingNullBehavior.Preserve,
            ScreenplaySyntax.UiBindingNullBehavior.Propagate => SceneCommon.BindingNullBehavior.Propagate,
            _ => null,
        };

    static SceneElements.ExternalComponent ConvertGuardedAction(ScreenplaySyntax.ScreenGuardedActionSyntax action, string id)
    {
        var properties = new Dictionary<string, object?>
        {
            ["label"] = action.Label,
            ["navigateToScreen"] = action.Navigate?.Screen,
            ["navigateByParameter"] = action.Navigate?.By,
            ["alternatives"] = action.Alternatives.Select(alternative => new Dictionary<string, object?>
            {
                ["command"] = alternative.Command,
                ["condition"] = ConvertCondition(alternative.Condition),
                ["arguments"] = ConvertArguments(alternative.Arguments),
            }).ToList(),
        };

        if (action.Otherwise is not null)
        {
            properties["otherwise"] = new Dictionary<string, object?>
            {
                ["outcome"] = action.Otherwise.Outcome.ToString(),
                ["command"] = action.Otherwise.Command,
                ["arguments"] = ConvertArguments(action.Otherwise.Arguments),
            };
        }

        return SceneElementFactory.Component(id, "core:action", properties);
    }

    static Dictionary<string, SceneCommon.BindingExpression> ConvertArguments(IEnumerable<ScreenplaySyntax.InteractionArgumentSyntax> arguments) =>
        arguments.ToDictionary(argument => argument.Name, argument => new SceneCommon.BindingExpression(argument.Binding), StringComparer.Ordinal);

    static Dictionary<string, object?> ConvertCondition(ScreenplaySyntax.ConditionSyntax condition) =>
        condition switch
        {
            ScreenplaySyntax.ComparisonConditionSyntax comparison => new Dictionary<string, object?>
            {
                ["kind"] = "comparison",
                ["left"] = new SceneCommon.BindingExpression(comparison.Left),
                ["operator"] = comparison.Operator.ToString(),
                ["right"] = ConvertExpression(comparison.Right),
            },
            _ => new Dictionary<string, object?>
            {
                ["kind"] = condition.GetType().Name,
            },
        };

    static object? ConvertExpression(ScreenplaySyntax.ExpressionSyntax expression) =>
        expression switch
        {
            ScreenplaySyntax.LiteralExpressionSyntax literal => literal.Value,
            _ => new Dictionary<string, object?> { ["kind"] = expression.GetType().Name },
        };

    static SceneElements.ExternalComponent ConvertSection(ScreenplaySyntax.ScreenSectionSyntax section, string id, BehaviorScope scope)
    {
        var attached = new List<SceneInteractions.Behavior>();
        var content = Convert(section.Directives, id, scope, attached);

        return SceneElementFactory.Component(
            id,
            "core:section",
            new Dictionary<string, object?> { ["name"] = section.Name },
            new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>> { ["content"] = content }) with
        {
            Behaviors = attached
        };
    }

    static SceneElements.ExternalComponent ConvertTable(ScreenplaySyntax.ScreenTableSyntax table, string id, BehaviorScope scope)
    {
        var properties = new Dictionary<string, object?>
        {
            // The route-resolution pass a running Stage attaches routes with (StageSceneRoutes.WithRoutes)
            // looks for this exact property on any element, regardless of component - matching the read model
            // name against a registered query is what lets the table showing InvoiceSummary and the data
            // directive that named it agree on where the data comes from, without the converter knowing
            // anything about routing.
            [SceneElementProperties.TypeName] = table.Target,
            ["navigateOnRowClickToScreen"] = table.RowClick?.Screen,
            ["navigateOnRowClickByParameter"] = table.RowClick?.By,
        };

        var columns = table.Columns
            .Select((column, index) => (SceneElements.SceneElement)SceneElementFactory.Component(
                $"{id}.{index}-column",
                "core:column",
                new Dictionary<string, object?> { ["property"] = column.Property, ["label"] = column.Label }))
            .ToList();

        return SceneElementFactory.Component(id, "core:table", properties, new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>> { ["columns"] = columns }) with
        {
            Behaviors = scope.Resolve(table.Behaviors, table.UsedBehaviors, id)
        };
    }

    static SceneElements.ExternalComponent ConvertSummary(ScreenplaySyntax.ScreenSummarySyntax summary, string id)
    {
        var fields = summary.Fields
            .Select((field, index) => (SceneElements.SceneElement)SceneElementFactory.Component(
                $"{id}.{index}-field",
                "core:field",
                new Dictionary<string, object?> { ["property"] = field.Property, ["label"] = field.Label }))
            .ToList();

        return SceneElementFactory.Component(
            id,
            "core:summary",
            new Dictionary<string, object?> { [SceneElementProperties.TypeName] = summary.Target },
            new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>> { ["fields"] = fields });
    }

    static string Kind(ScreenplaySyntax.ScreenDirectiveSyntax directive) =>
        directive switch
        {
            ScreenplaySyntax.ScreenDataSyntax => "data",
            ScreenplaySyntax.ScreenActionSyntax => "action",
            ScreenplaySyntax.ScreenGuardedActionSyntax => "guarded-action",
            ScreenplaySyntax.ScreenSectionSyntax => "section",
            ScreenplaySyntax.ScreenNavigateSyntax => "navigate",
            ScreenplaySyntax.ScreenTitleSyntax => "title",
            ScreenplaySyntax.ScreenTableSyntax => "table",
            ScreenplaySyntax.ScreenSummarySyntax => "summary",
            ScreenplaySyntax.ScreenToolbarSyntax => "toolbar",
            ScreenplaySyntax.ScreenComponentSyntax => "component",
            ScreenplaySyntax.ScreenCodeSyntax => "code",
            _ => throw new UnknownScreenDirective(directive.GetType().Name),
        };
}
