// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Screenplay;
using SceneCommon = Cratis.Scene.Model.Common;
using SceneOutlets = Cratis.Scene.Model.Outlets;
using SceneStructure = Cratis.Scene.Model.Structure;
using SceneToolbar = Cratis.Scene.Model.Toolbar;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts Screenplay outlets - the item lists a layout or template offers and the levels below refine or contribute to - into
/// Scene's outlet model.
/// </summary>
/// <remarks>
/// <para>
/// An outlet with a kind is a declaration (<see cref="SceneOutlets.Outlet"/>) whose items are its defaults; an outlet without one is a
/// level's refinement (<see cref="SceneOutlets.OutletRefinement"/>) with its mode, its own items and the ids it hides; a
/// <c language="csharp">contribute to</c> with items is an <see cref="SceneOutlets.OutletContribution"/>. Nothing here resolves what an
/// outlet shows: Scene's engine does that from the levels (<c language="csharp">EffectiveOutlets.Resolve</c>).
/// </para>
/// <para>
/// A button carries its hook: the qualified slice, the built-in it runs, where it opens (a bare <c language="csharp">runs</c> is
/// <see cref="SceneOutlets.Opens.None"/>, run directly), what it acts on, when it is enabled, its confirmation and the policy it needs.
/// <c language="csharp">before</c> and <c language="csharp">after</c> become the item's position.
/// </para>
/// </remarks>
public static class OutletConverter
{
    /// <summary>
    /// The suffix Screenplay gives the actions outlet that holds a navigation outlet's buttons (<c language="csharp">topbarActions</c>).
    /// </summary>
    public const string ScreenplayButtonsSuffix = "Actions";

    /// <summary>
    /// The suffix Scene gives the actions outlet that holds a navigation outlet's buttons (<c language="csharp">topbarButtons</c>).
    /// </summary>
    public const string SceneButtonsSuffix = "Buttons";

    /// <summary>
    /// Converts an outlet declaration.
    /// </summary>
    /// <param name="outlet">The <see cref="ScreenplaySyntax.OutletSyntax"/> that declares a kind.</param>
    /// <param name="structure">The structure slices resolve against, if any.</param>
    /// <param name="name">The name Scene knows the outlet by, when it differs from the authored one.</param>
    /// <returns>The <see cref="SceneOutlets.Outlet"/>.</returns>
    public static SceneOutlets.Outlet Declaration(ScreenplaySyntax.OutletSyntax outlet, SceneStructure.ApplicationStructure? structure, string? name = null) =>
        new(
            name ?? outlet.Name,
            KindOf(outlet.Kind ?? ScreenplaySyntax.OutletKind.Actions),
            [.. Items(outlet.Items, structure)],
            SubjectOf(outlet.Subject),
            outlet.Slot);

    /// <summary>
    /// Converts the outlets a template declares.
    /// </summary>
    /// <param name="outlets">The template's <see cref="ScreenplaySyntax.OutletSyntax"/>.</param>
    /// <param name="structure">The structure slices resolve against, if any.</param>
    /// <returns>The declared <see cref="SceneOutlets.Outlet"/>s, or null when the template declares none.</returns>
    public static IReadOnlyList<SceneOutlets.Outlet>? Declarations(IEnumerable<ScreenplaySyntax.OutletSyntax> outlets, SceneStructure.ApplicationStructure? structure)
    {
        var declared = outlets.Where(IsDeclaration).Select(outlet => Declaration(outlet, structure)).ToList();
        return declared.Count > 0 ? declared : null;
    }

    /// <summary>
    /// Converts a level's refinement of an outlet it inherits.
    /// </summary>
    /// <param name="outlet">The <see cref="ScreenplaySyntax.OutletSyntax"/> without a kind.</param>
    /// <param name="structure">The structure slices resolve against, if any.</param>
    /// <param name="name">The name Scene knows the outlet by, when it differs from the authored one.</param>
    /// <returns>The <see cref="SceneOutlets.OutletRefinement"/>.</returns>
    public static SceneOutlets.OutletRefinement Refinement(ScreenplaySyntax.OutletSyntax outlet, SceneStructure.ApplicationStructure? structure, string? name = null)
    {
        var items = Items(outlet.Items, structure).ToList();
        var hides = outlet.Hides.Select(hide => hide.Id).ToList();
        return new(
            name ?? outlet.Name,
            ModeOf(outlet.Mode),
            items.Count > 0 ? items : null,
            hides.Count > 0 ? hides : null);
    }

    /// <summary>
    /// Converts the items a <c language="csharp">contribute to</c> adds to an outlet.
    /// </summary>
    /// <param name="contribution">The <see cref="ScreenplaySyntax.ContributionSyntax"/>.</param>
    /// <param name="structure">The structure slices resolve against, if any.</param>
    /// <param name="name">The name Scene knows the outlet by, when it differs from the authored one.</param>
    /// <returns>The <see cref="SceneOutlets.OutletContribution"/>, or null when the contribution lists no items.</returns>
    public static SceneOutlets.OutletContribution? Contribution(ScreenplaySyntax.ContributionSyntax contribution, SceneStructure.ApplicationStructure? structure, string? name = null)
    {
        var items = Items(contribution.Items, structure).ToList();
        return items.Count == 0 ? null : new(name ?? contribution.ContributionPoint, items);
    }

    /// <summary>
    /// Tells whether an outlet is a declaration rather than a refinement.
    /// </summary>
    /// <param name="outlet">The <see cref="ScreenplaySyntax.OutletSyntax"/>.</param>
    /// <returns>Whether it declares a kind.</returns>
    public static bool IsDeclaration(ScreenplaySyntax.OutletSyntax outlet) => outlet.Kind is not null;

    /// <summary>
    /// Converts outlet items.
    /// </summary>
    /// <param name="items">The <see cref="ScreenplaySyntax.OutletItemSyntax"/> items.</param>
    /// <param name="structure">The structure slices resolve against, if any.</param>
    /// <returns>The Scene <see cref="SceneOutlets.OutletItem"/> items.</returns>
    public static IEnumerable<SceneOutlets.OutletItem> Items(IEnumerable<ScreenplaySyntax.OutletItemSyntax> items, SceneStructure.ApplicationStructure? structure) =>
        items.Select(item => Item(item, structure)).OfType<SceneOutlets.OutletItem>();

    /// <summary>
    /// Converts one outlet item.
    /// </summary>
    /// <param name="item">The <see cref="ScreenplaySyntax.OutletItemSyntax"/>.</param>
    /// <param name="structure">The structure slices resolve against, if any.</param>
    /// <returns>The Scene <see cref="SceneOutlets.OutletItem"/>, or null for an item kind Scene does not have.</returns>
    public static SceneOutlets.OutletItem? Item(ScreenplaySyntax.OutletItemSyntax item, SceneStructure.ApplicationStructure? structure)
    {
        SceneOutlets.OutletItem? converted = item switch
        {
            ScreenplaySyntax.ToolbarButtonSyntax button => Button(button, structure),
            ScreenplaySyntax.OutletGroupSyntax group => new SceneOutlets.OutletGroup
            {
                Id = group.Id,
                Title = group.Title,
                Icon = group.Icon,
                Items = [.. group.Buttons.Select(button => Button(button, structure))]
            },
            ScreenplaySyntax.OutletFilterSyntax filter => new SceneOutlets.OutletFilter
            {
                Id = filter.Id,
                Title = filter.Title,
                Sets = filter.Query is { } query && filter.Parameter is { } parameter ? new SceneOutlets.QueryParameterReference(query, parameter) : null
            },
            ScreenplaySyntax.OutletTabSyntax tab => new SceneOutlets.OutletTab { Id = tab.Id, Title = tab.Title, Shows = tab.Shows ?? string.Empty },
            ScreenplaySyntax.OutletStatSyntax stat => new SceneOutlets.OutletStat
            {
                Id = stat.Id,
                Title = stat.Title,
                ValueFrom = stat.Query is { } query && stat.Field is { } field ? new SceneOutlets.QueryFieldReference(query, field) : null
            },
            ScreenplaySyntax.NavigationItemSyntax navigation => NavigationItem(navigation),
            _ => null
        };

        return converted is null ? null : Positioned(converted, item.Position);
    }

    /// <summary>
    /// Converts a navigation item and its sub-items.
    /// </summary>
    /// <param name="item">The <see cref="ScreenplaySyntax.NavigationItemSyntax"/>.</param>
    /// <returns>The <see cref="SceneOutlets.OutletNavigationItem"/>.</returns>
    public static SceneOutlets.OutletNavigationItem NavigationItem(ScreenplaySyntax.NavigationItemSyntax item)
    {
        var children = item.Children.Select(NavigationItem).ToList();
        return new()
        {
            Id = item.Id,
            Title = item.Title,
            Icon = item.Icon,
            Target = item.Target is null ? null : new SceneCommon.DestinationReference(Module: item.Target.Module, Feature: item.Target.Feature),
            Slot = item.Slot,
            Children = children.Count > 0 ? children : null
        };
    }

    /// <summary>
    /// Converts a button and the hook it carries.
    /// </summary>
    /// <param name="button">The <see cref="ScreenplaySyntax.ToolbarButtonSyntax"/>.</param>
    /// <param name="structure">The structure slices resolve against, if any.</param>
    /// <returns>The <see cref="SceneOutlets.OutletButton"/>.</returns>
    public static SceneOutlets.OutletButton Button(ScreenplaySyntax.ToolbarButtonSyntax button, SceneStructure.ApplicationStructure? structure)
    {
        var converted = new SceneOutlets.OutletButton
        {
            Id = button.Id,
            Title = button.Title,
            Icon = button.Icon,
            Slice = button.Slice is null ? null : StructureConverter.QualifiedSlice(structure, button.Slice),
            Emphasis = button.Emphasis is { } emphasis ? Enum.Parse<SceneToolbar.ToolbarEmphasis>(emphasis.ToString(), ignoreCase: true) : null,
            BuiltIn = BuiltInOf(button.Runs),
            Opens = OpensOf(button.Opens),
            OpensIn = button.Opens == ScreenplaySyntax.HookOpening.Slot ? button.OpensSlot : null,
            Subject = SubjectOf(button.Subject),
            EnabledWhen = button.EnabledWhen is null ? null : ConditionText(button.EnabledWhen),
            Confirm = button.Confirm,
            Authorize = button.Authorize is { } authorize ? PolicyText(authorize.Requirement) : null
        };
        return Positioned(converted, button.Position);
    }

    /// <summary>
    /// Gives the policy a button needs, as the requirement was written: a policy name, or names combined with <c language="csharp">and</c> and
    /// <c language="csharp">or</c>.
    /// </summary>
    /// <param name="requirement">The <see cref="ScreenplaySyntax.PolicyRequirementSyntax"/>.</param>
    /// <returns>The policy expression.</returns>
    public static string PolicyText(ScreenplaySyntax.PolicyRequirementSyntax requirement) => requirement switch
    {
        ScreenplaySyntax.PolicyReferenceSyntax reference => reference.Name,
        ScreenplaySyntax.LogicalPolicyRequirementSyntax logical => $"({PolicyText(logical.Left)} {logical.Operator.ToString().ToLowerInvariant()} {PolicyText(logical.Right)})",
        _ => string.Empty
    };

    /// <summary>
    /// Gives an <c language="csharp">enabled when</c> condition as text, the form a host answers it by.
    /// </summary>
    /// <param name="condition">The <see cref="ScreenplaySyntax.ConditionSyntax"/>.</param>
    /// <returns>The condition, written as Screenplay writes it.</returns>
    public static string ConditionText(ScreenplaySyntax.ConditionSyntax condition) => condition switch
    {
        ScreenplaySyntax.ComparisonConditionSyntax comparison =>
            $"{(comparison.LeftExpression is { } left ? ExpressionText(left) : comparison.Left)} {ComparisonText(comparison.Operator)} {ExpressionText(comparison.Right)}",
        ScreenplaySyntax.LogicalConditionSyntax logical =>
            $"{Grouped(logical.Left, logical.Operator)} {logical.Operator.ToString().ToLowerInvariant()} {Grouped(logical.Right, logical.Operator)}",
        _ => string.Empty
    };

    /// <summary>
    /// Gives the name Scene knows an authored outlet by: the actions outlet that holds a navigation outlet's buttons is
    /// <c language="csharp">&lt;outlet&gt;Buttons</c> in Scene and <c language="csharp">&lt;outlet&gt;Actions</c> in Screenplay.
    /// </summary>
    /// <param name="name">The authored outlet name.</param>
    /// <param name="navigationOutlets">The names of the application's navigation outlets.</param>
    /// <returns>The Scene outlet name.</returns>
    public static string SceneName(string name, IReadOnlySet<string> navigationOutlets) =>
        name.EndsWith(ScreenplayButtonsSuffix, StringComparison.Ordinal) &&
        name.Length > ScreenplayButtonsSuffix.Length &&
        navigationOutlets.Contains(name[..^ScreenplayButtonsSuffix.Length])
            ? $"{name[..^ScreenplayButtonsSuffix.Length]}{SceneButtonsSuffix}"
            : name;

    /// <summary>
    /// Converts an outlet kind.
    /// </summary>
    /// <param name="kind">The <see cref="ScreenplaySyntax.OutletKind"/>.</param>
    /// <returns>The <see cref="SceneOutlets.OutletKind"/>.</returns>
    public static SceneOutlets.OutletKind KindOf(ScreenplaySyntax.OutletKind kind) => kind switch
    {
        ScreenplaySyntax.OutletKind.Filters => SceneOutlets.OutletKind.Filters,
        ScreenplaySyntax.OutletKind.Tabs => SceneOutlets.OutletKind.Tabs,
        ScreenplaySyntax.OutletKind.Stats => SceneOutlets.OutletKind.Stats,
        ScreenplaySyntax.OutletKind.Navigation => SceneOutlets.OutletKind.Navigation,
        _ => SceneOutlets.OutletKind.Actions
    };

    /// <summary>
    /// Converts a refinement mode; an unstated mode stays unstated, which Scene reads as extending when the level has items or hides.
    /// </summary>
    /// <param name="mode">The <see cref="ScreenplaySyntax.OutletMode"/>.</param>
    /// <returns>The <see cref="SceneOutlets.OutletMode"/>, or null when none was written.</returns>
    public static SceneOutlets.OutletMode? ModeOf(ScreenplaySyntax.OutletMode mode) => mode switch
    {
        ScreenplaySyntax.OutletMode.Inherit => SceneOutlets.OutletMode.Inherits,
        ScreenplaySyntax.OutletMode.Extend => SceneOutlets.OutletMode.Extends,
        ScreenplaySyntax.OutletMode.Override => SceneOutlets.OutletMode.Overrides,
        _ => null
    };

    /// <summary>
    /// Converts what an outlet or action acts on.
    /// </summary>
    /// <param name="subject">The <see cref="ScreenplaySyntax.ScreenActionSubjectKind"/>, if any.</param>
    /// <returns>The <see cref="SceneOutlets.OutletSubject"/>, or null.</returns>
    public static SceneOutlets.OutletSubject? SubjectOf(ScreenplaySyntax.ScreenActionSubjectKind? subject) => subject switch
    {
        ScreenplaySyntax.ScreenActionSubjectKind.Item => SceneOutlets.OutletSubject.Item,
        ScreenplaySyntax.ScreenActionSubjectKind.Screen => SceneOutlets.OutletSubject.Screen,
        _ => null
    };

    /// <summary>
    /// Converts where a hook opens; no <c language="csharp">opens</c> is automatic, which Scene leaves unstated.
    /// </summary>
    /// <param name="opening">The <see cref="ScreenplaySyntax.HookOpening"/>, if any.</param>
    /// <returns>The <see cref="SceneOutlets.Opens"/>, or null for automatic.</returns>
    public static SceneOutlets.Opens? OpensOf(ScreenplaySyntax.HookOpening? opening) => opening switch
    {
        ScreenplaySyntax.HookOpening.Runs => SceneOutlets.Opens.None,
        ScreenplaySyntax.HookOpening.Dialog => SceneOutlets.Opens.Dialog,
        ScreenplaySyntax.HookOpening.Drawer => SceneOutlets.Opens.Drawer,
        ScreenplaySyntax.HookOpening.Slot => SceneOutlets.Opens.Slot,
        _ => null
    };

    /// <summary>
    /// Converts the built-in a button runs.
    /// </summary>
    /// <param name="runs">The authored built-in: <c language="csharp">close</c>, <c language="csharp">submit</c> or <c language="csharp">refresh</c>.</param>
    /// <returns>The <see cref="SceneOutlets.OutletBuiltInAction"/>, or null.</returns>
    public static SceneOutlets.OutletBuiltInAction? BuiltInOf(string? runs) => runs?.ToLowerInvariant() switch
    {
        "close" => SceneOutlets.OutletBuiltInAction.Close,
        "submit" => SceneOutlets.OutletBuiltInAction.Submit,
        "refresh" => SceneOutlets.OutletBuiltInAction.Refresh,
        _ => null
    };

    static TItem Positioned<TItem>(TItem item, ScreenplaySyntax.OutletPositionSyntax? position)
        where TItem : SceneOutlets.OutletItem
    {
        if (position is null) return item;
        if (position.Placement == ScreenplaySyntax.OutletPlacement.Before) return item with { Before = position.Id };
        return item with { After = position.Id };
    }

    static string Grouped(ScreenplaySyntax.ConditionSyntax condition, ScreenplaySyntax.LogicalOperator parent) =>
        condition is ScreenplaySyntax.LogicalConditionSyntax logical && logical.Operator != parent ? $"({ConditionText(condition)})" : ConditionText(condition);

    static string ExpressionText(ScreenplaySyntax.ExpressionSyntax expression) => ProducedValueConverter.Convert(expression).Expression;

    static string ComparisonText(ScreenplaySyntax.ComparisonOperator @operator) => @operator switch
    {
        ScreenplaySyntax.ComparisonOperator.NotEqual => "!=",
        ScreenplaySyntax.ComparisonOperator.GreaterThan => ">",
        ScreenplaySyntax.ComparisonOperator.GreaterThanOrEqual => ">=",
        ScreenplaySyntax.ComparisonOperator.LessThan => "<",
        ScreenplaySyntax.ComparisonOperator.LessThanOrEqual => "<=",
        _ => "=="
    };
}
