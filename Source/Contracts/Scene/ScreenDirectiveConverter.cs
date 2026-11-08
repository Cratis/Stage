// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using SceneCommon = Cratis.Scene.Model.Common;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneInteractions = Cratis.Scene.Model.Interactions;
using SceneOutlets = Cratis.Scene.Model.Outlets;
using SceneToolbar = Cratis.Scene.Model.Toolbar;
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
    /// The Scene component that draws an outlet where a screen places it.
    /// </summary>
    public const string OutletItemsComponent = "core:outletItems";

    /// <summary>
    /// The outlet a screen's toolbar draws when it names none.
    /// </summary>
    public const string DefaultToolbarOutlet = "toolbar";

    /// <summary>
    /// The Scene component a slot is drawn with.
    /// </summary>
    public const string SlotComponent = "core:slot";

    /// <summary>
    /// The property of a slot that names the screen it shows for a selection (<c language="screenplay">show &lt;Screen&gt;</c>).
    /// </summary>
    public const string ShowsProperty = "shows";

    /// <summary>
    /// The property of a slot that names the parameter the screen it shows is keyed by (<c language="screenplay">by &lt;param&gt;</c>).
    /// </summary>
    public const string ShowsByProperty = "showsBy";

    /// <summary>
    /// The property of a slot that names the table or component whose selection it shows (<c language="screenplay">from &lt;element&gt;.selection</c>).
    /// </summary>
    public const string ShowsFromProperty = "showsFrom";

    /// <summary>
    /// The property of a slot that holds the id of the element whose selection it shows, resolved within the screen.
    /// </summary>
    public const string ShowsFromElementProperty = "showsFromElement";

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
        ICollection<SceneInteractions.Behavior>? attached = null) =>
        Convert(directives, path, behaviors, attached, behaviors?.Diagnostics);

    /// <summary>
    /// Converts directives while omitting unsupported nodes and retaining their supported siblings.
    /// </summary>
    /// <param name="directives">The sibling directives.</param>
    /// <param name="path">The owning content path.</param>
    /// <param name="behaviors">The behavior scope.</param>
    /// <param name="attached">The attached behaviors to collect.</param>
    /// <param name="diagnostics">The collector, or null for legacy throwing behavior.</param>
    /// <returns>The supported elements in declaration order.</returns>
    public static IReadOnlyList<SceneElements.SceneElement> Convert(
        IEnumerable<ScreenplaySyntax.ScreenDirectiveSyntax> directives,
        string path,
        BehaviorScope? behaviors,
        ICollection<SceneInteractions.Behavior>? attached,
        ConversionDiagnostics? diagnostics)
    {
        var all = directives.ToList();
        var scope = (behaviors ?? BehaviorScope.None) with { Diagnostics = diagnostics };

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
                .Select((directive, index) => Convert(directive, $"{path}.{index}-{Kind(directive, scope.Diagnostics)}", scope))
                .OfType<SceneElements.SceneElement>()
        ];
    }

    static SceneElements.ExternalComponent? Convert(ScreenplaySyntax.ScreenDirectiveSyntax directive, string id, BehaviorScope scope) =>
        directive switch
        {
            ScreenplaySyntax.ScreenDataSyntax data => SceneElementFactory.Component(id, "core:data", new Dictionary<string, object?>
            {
                [SceneElementProperties.TypeName] = data.Type.Name,
                ["isCollection"] = data.Type.IsCollection,
                [SceneElementProperties.Query] = data.Query,
                ["by"] = data.By,
            }),
            ScreenplaySyntax.ScreenActionSyntax action => SceneElementFactory.Component(id, "core:action", new Dictionary<string, object?>
            {
                ["command"] = action.Command,
                ["label"] = action.Label,
                ["navigateToScreen"] = action.Navigate?.Screen,
                ["navigateByParameter"] = action.Navigate?.By,
                [SceneElementProperties.Destination] = action.Navigate is null ? null : CompositionConverter.Destination(action.Navigate),
            }.WithSubject(action.Subject, action.Inputs)),
            ScreenplaySyntax.ScreenGuardedActionSyntax action => ConvertGuardedAction(action, id, scope),
            ScreenplaySyntax.ScreenSectionSyntax section => ConvertSection(section, id, scope),
            ScreenplaySyntax.ScreenNavigateSyntax navigate => SceneElementFactory.Component(id, "core:navigate", new Dictionary<string, object?>
            {
                ["targetScreen"] = navigate.Screen,
                ["by"] = navigate.By,
                [SceneElementProperties.Destination] = CompositionConverter.Destination(navigate),
            }),
            ScreenplaySyntax.ScreenTitleSyntax title => SceneElementFactory.Component(id, "core:title", new Dictionary<string, object?> { ["text"] = title.Text }),
            ScreenplaySyntax.ScreenTableSyntax table => ConvertTable(table, id, scope),
            ScreenplaySyntax.ScreenSummarySyntax summary => ConvertSummary(summary, id),
            ScreenplaySyntax.ScreenToolbarSyntax toolbar => ConvertToolbar(toolbar, id, scope),
            ScreenplaySyntax.ScreenComponentSyntax component => ConvertComponent(component, id, scope),
            ScreenplaySyntax.ScreenShowSyntax show => ConvertShow(show, id),
            ScreenplaySyntax.ScreenCodeSyntax code => SceneElementFactory.Component(id, "core:code", new Dictionary<string, object?>
            {
                ["language"] = code.Code.Language,
                ["code"] = code.Code.Code,
            }),
            _ => Unsupported(directive, id, scope.Diagnostics),
        };

    static SceneElements.ExternalComponent? Unsupported(ScreenplaySyntax.ScreenDirectiveSyntax directive, string path, ConversionDiagnostics? diagnostics)
    {
        if (diagnostics is null) throw new UnknownScreenDirective(directive.GetType().Name);
        var construct = $"screen directive {directive.GetType().Name}";
        diagnostics.Report(new("STAGE-SCENE-DIRECTIVE-001", DiagnosticSeverity.Error, construct, path, directive.Location, $"Unsupported {construct} was omitted from '{path}'."));
        return null;
    }

    // A screen's toolbar is the outlet of actions it places (Scene 5's core:outletItems): its name names the outlet, `toolbar` when it
    // names none, and every item and button is an outlet button. A button keeps its hook - the qualified slice, where it opens, what it
    // acts on, when it is enabled. A navigate or dialog item opens its destination; an action item names its command. An outlet button
    // takes a command's inputs from the slice, so the parameters an action item passes are reported as omitted. A `placement` (or
    // `emphasis`) presentation of primary, secondary or quiet is the button's emphasis, and a `mode` presentation of icon, title or
    // iconAndTitle is what the button shows (its title stays the accessible name); an outlet button has nowhere to carry any other
    // presentation value, so those are reported as omitted too.
    static SceneElements.ExternalComponent ConvertToolbar(ScreenplaySyntax.ScreenToolbarSyntax toolbar, string id, BehaviorScope scope)
    {
        var items = new List<SceneOutlets.OutletItem>();
        foreach (var item in toolbar.Items)
        {
            if (item.Kind == ScreenplaySyntax.ToolbarItemKind.Action && item.Parameters.Any() && scope.Diagnostics is { } diagnostics)
            {
                diagnostics.Report(new("STAGE-SCENE-TOOLBAR-001", DiagnosticSeverity.Warning, "toolbar action parameters", $"{id}.{item.Name}", item.Location, $"The parameters of the toolbar action '{item.Name}' were omitted: an outlet button takes its command's inputs from the slice."));
            }

            SceneToolbar.ToolbarEmphasis? emphasis = null;
            SceneOutlets.OutletButtonDisplay? display = null;
            foreach (var presentation in item.Presentation)
            {
                if (emphasis is null && EmphasisOf(presentation) is { } authored)
                {
                    emphasis = authored;
                    continue;
                }

                if (display is null && DisplayOf(presentation) is { } shown)
                {
                    display = shown;
                    continue;
                }

                scope.Diagnostics?.Report(new("STAGE-SCENE-TOOLBAR-002", DiagnosticSeverity.Warning, "toolbar item presentation", $"{id}.{item.Name}", presentation.Location, $"The presentation '{presentation.Name}' of the toolbar item '{item.Name}' was omitted: an outlet button carries only its emphasis (placement primary, secondary or quiet) and what it shows (mode icon, title or iconAndTitle)."));
            }

            items.Add(new SceneOutlets.OutletButton
            {
                Id = item.Name,
                Title = item.Label ?? item.Name,
                Icon = item.Icon,
                Command = item.Kind == ScreenplaySyntax.ToolbarItemKind.Action ? item.Target : null,
                NavigateTo = ToolbarDestination(item),
                Subject = OutletConverter.SubjectOf(item.Subject),
                EnabledWhen = item.EnabledWhen is null ? null : OutletConverter.ConditionText(item.EnabledWhen),
                Confirm = item.Confirm,
                Emphasis = emphasis,
                Display = display
            });
        }

        items.AddRange(toolbar.Buttons.Select(button => OutletConverter.Button(button, scope.Structure)));

        return SceneElementFactory.Component(
            id,
            OutletItemsComponent,
            new Dictionary<string, object?>
            {
                ["name"] = string.IsNullOrEmpty(toolbar.Name) ? DefaultToolbarOutlet : toolbar.Name,
                ["kind"] = "actions",
                ["items"] = items
            });
    }

    // The explicit subject of an action (Screenplay decision 0110) is carried as Stage-local scene properties until Scene ships a typed
    // ActionSubject. Nothing is written for an omitted subject, so every existing Scene stays byte-identical.
    static Dictionary<string, object?> WithSubject(this Dictionary<string, object?> properties, ScreenplaySyntax.ScreenActionSubjectKind? subject, IEnumerable<ScreenplaySyntax.InteractionArgumentSyntax> inputs)
    {
        if (subject is null)
        {
            return properties;
        }

        properties["actionSubject"] = subject.Value.ToString().ToLowerInvariant();
        var arguments = ConvertArguments(inputs);
        if (arguments.Count > 0)
        {
            properties["subjectInputs"] = arguments;
        }

        return properties;
    }

    // Where a navigate or dialog item opens, so a renderer executes it through the same destination model every other
    // navigation uses. An action item opens nothing.
    static SceneToolbar.ToolbarEmphasis? EmphasisOf(ScreenplaySyntax.PresentationValueSyntax presentation) =>
        presentation.Name.Equals("placement", StringComparison.OrdinalIgnoreCase) || presentation.Name.Equals("emphasis", StringComparison.OrdinalIgnoreCase)
            ? presentation.Value.ToLowerInvariant() switch
            {
                "primary" => SceneToolbar.ToolbarEmphasis.Primary,
                "secondary" => SceneToolbar.ToolbarEmphasis.Secondary,
                "quiet" => SceneToolbar.ToolbarEmphasis.Quiet,
                _ => null
            }
            : null;

    // What a toolbar item's outlet button shows: a `mode` presentation of icon, title or iconAndTitle.
    static SceneOutlets.OutletButtonDisplay? DisplayOf(ScreenplaySyntax.PresentationValueSyntax presentation) =>
        presentation.Name.Equals("mode", StringComparison.OrdinalIgnoreCase)
            ? presentation.Value.ToLowerInvariant() switch
            {
                "icon" => SceneOutlets.OutletButtonDisplay.Icon,
                "title" => SceneOutlets.OutletButtonDisplay.Title,
                "iconandtitle" => SceneOutlets.OutletButtonDisplay.IconAndTitle,
                _ => null
            }
            : null;

    static SceneCommon.DestinationReference? ToolbarDestination(ScreenplaySyntax.ToolbarItemSyntax item)
    {
        var parameters = item.Parameters.ToDictionary(parameter => parameter.Name, parameter => ScreenplayRichSyntax.Binding(parameter.Binding), StringComparer.Ordinal);
        return item.Kind switch
        {
            ScreenplaySyntax.ToolbarItemKind.Navigate => new SceneCommon.DestinationReference(Screen: item.Target, RouteParameterBindings: parameters),
            ScreenplaySyntax.ToolbarItemKind.Dialog => new SceneCommon.DestinationReference(Kind: SceneCommon.DestinationKind.Dialog, Dialog: item.Target, RouteParameterBindings: parameters),
            _ => null,
        };
    }

    static SceneElements.ExternalComponent ConvertComponent(ScreenplaySyntax.ScreenComponentSyntax component, string id, BehaviorScope scope)
    {
        var stableId = ScreenplayRichSyntax.StableId(component);
        var properties = new Dictionary<string, object?>
        {
            ["name"] = component.Name,
            ["context"] = component.Context is null ? null : ScreenplayRichSyntax.Binding(component.Context),
            ["icon"] = component.Icon,
            ["presentation"] = component.Presentation.ToDictionary(presentation => presentation.Name, presentation => presentation.Value, StringComparer.Ordinal),
            ["exposes"] = component.Exposes.ToDictionary(exposed => exposed.Name, exposed => ScreenplayRichSyntax.Binding(exposed.Binding), StringComparer.Ordinal),
        };

        foreach (var property in component.Properties)
        {
            properties[property.Property] = property.Binding is null ? ScreenplayRichSyntax.ComponentPropertyValue(property) : ScreenplayRichSyntax.Binding(property.Binding);
        }

        var slots = component.Slots.ToDictionary(
            slot => slot.Name,
            slot => Convert(slot.Directives, $"{stableId}.{slot.Name}", scope),
            StringComparer.Ordinal);

        return SceneElementFactory.Component(stableId, component.Component, properties, slots) with
        {
            Behaviors = scope.Resolve(component.Behaviors, component.UsedBehaviors, stableId)
        };
    }

    static SceneElements.ExternalComponent ConvertGuardedAction(ScreenplaySyntax.ScreenGuardedActionSyntax action, string id, BehaviorScope scope)
    {
        var properties = new Cratis.Scene.Model.Guards.GuardedActionProperties(
            action.Label,
            [.. action.Alternatives.Select(alternative => new Cratis.Scene.Model.Guards.GuardedActionAlternative(
                alternative.Command,
                scope.GuardContexts is null
                    ? SceneGuards.ConvertToCondition(alternative.Condition, scope.EnumerationOf, null, scope.EnumerationNumbersOf)
                    : SceneGuardConversion.FromResolved(alternative.Condition, scope.At(action).FieldType).ToCondition(),
                ConvertArguments(alternative.Arguments)))],
            Otherwise: action.Otherwise is { } otherwise ? new Cratis.Scene.Model.Guards.GuardedActionFallback(
                Enum.TryParse<Cratis.Scene.Model.Guards.GuardedActionFallbackOutcome>(otherwise.Outcome.ToString(), out var outcome) ? outcome : (Cratis.Scene.Model.Guards.GuardedActionFallbackOutcome)(-1),
                otherwise.Command,
                ConvertArguments(otherwise.Arguments)) : null,
            NavigateToScreen: action.Navigate?.Screen,
            NavigateByParameter: action.Navigate?.By);
        var propertyBag = new Dictionary<string, object?>(properties.ToProperties()) { ["label"] = action.Label };
        return SceneElementFactory.Component(id, Cratis.Scene.Model.Guards.GuardedActionProperties.ComponentName, propertyBag);
    }

    static Dictionary<string, Cratis.Scene.Model.Guards.SubjectReference> ConvertArguments(IEnumerable<ScreenplaySyntax.InteractionArgumentSyntax> arguments) =>
        arguments.ToDictionary(argument => argument.Name, argument => SceneGuards.SubjectReferenceFor(argument.Binding), StringComparer.Ordinal);

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
        var row = scope.At(table);
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
            .Select((column, index) =>
            {
                var columnProperties = new Dictionary<string, object?> { ["property"] = column.Property, ["label"] = column.Label };
                var field = row.FieldType($"{SceneGuards.SubjectPrefix}{column.Property}");
                if (scope.GuardContexts is not null && field.Kind == GuardFieldKind.Enumeration)
                {
                    columnProperties["options"] = field.Members;
                    if (field.MemberNumbers is not null) columnProperties["optionValues"] = field.MemberNumbers;
                }
                else if (scope.GuardContexts is null && scope.Enumerations is { } enumerations && enumerations.TryGetValue(table.Target, out var fields) && fields.TryGetValue(column.Property, out var members))
                {
                    columnProperties["options"] = members;
                    if (scope.EnumerationNumbers is { } numbers && numbers.TryGetValue(table.Target, out var numberedFields) && numberedFields.TryGetValue(column.Property, out var memberNumbers))
                    {
                        columnProperties["optionValues"] = memberNumbers;
                    }
                }

                return (SceneElements.SceneElement)SceneElementFactory.Component($"{id}.{index}-column", "core:column", columnProperties);
            })
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

    // `show <Screen> by <param> from <element>.selection` is the slot it stands in, showing another screen for the selected row. The
    // slot's name, role and routing come from the template the screen fills (TemplateSlotPlacement), which this converter does not see.
    static SceneElements.ExternalComponent ConvertShow(ScreenplaySyntax.ScreenShowSyntax show, string id) =>
        SceneElementFactory.Component(id, SlotComponent, new Dictionary<string, object?>
        {
            [ShowsProperty] = show.Screen,
            [ShowsByProperty] = show.By,
            [ShowsFromProperty] = show.Element,
        });

    static string Kind(ScreenplaySyntax.ScreenDirectiveSyntax directive, ConversionDiagnostics? diagnostics) =>
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
            ScreenplaySyntax.ScreenShowSyntax => "show",
            _ => diagnostics is null ? throw new UnknownScreenDirective(directive.GetType().Name) : "unsupported",
        };
}
