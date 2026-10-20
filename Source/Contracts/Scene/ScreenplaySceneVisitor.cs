// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneContributionPoints = Cratis.Scene.Model.ContributionPoints;
using SceneLayouts = Cratis.Scene.Model.Layouts;
using SceneModel = Cratis.Scene.Model.Interactions;
using SceneScreens = Cratis.Scene.Model.Screens;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Visits a compiled Screenplay <see cref="ScreenplaySyntax.ApplicationSyntax"/> and produces the Scene
/// translation of it - part of Cratis/Stage#37. A separate, parallel visitor from
/// <see cref="Cratis.Stage.Contracts.Screenplay.ScreenplayEventModelVisitor"/> (which produces the unrelated <c language="csharp">EventModel</c>) -
/// this one does not touch it.
/// </summary>
/// <remarks>
/// The roots follow Screenplay's taxonomy: <c language="csharp">layout</c> is top-level (the application's one navigational
/// shell), while <c language="csharp">screen template</c> and <c language="csharp">dialog template</c> - the reusable shapes inside it - are
/// module-scoped. A document may declare several layouts so that different <c language="csharp">ui profile</c>s can select
/// different shells; every screen resolves against the first declared one, because
/// <see cref="SceneScreens.Screen.Layout"/> holds a single name. Carrying the per-profile selection through
/// would mean one <see cref="SceneScreens.Screen"/> per profile, which this translation deliberately does not
/// do - per-target resolution is <see cref="RenderPlanner"/>'s job (Cratis/Stage#39), and a screen left on a
/// shell its target does not select is reported as <see cref="RenderFindingKind.ScreenNotOnSelectedLayout"/>
/// rather than silently rendered against the wrong one.
/// </remarks>
public sealed class ScreenplaySceneVisitor : ScreenplaySyntax.IApplicationSyntaxVisitor<SceneApplication>
{
    readonly List<RenderFinding> _findings = [];

    /// <summary>
    /// Gets what the translation could not resolve - currently a <c language="csharp">uses</c> naming a behavior the document
    /// does not declare.
    /// </summary>
    public IReadOnlyList<RenderFinding> Findings => _findings;

    /// <inheritdoc/>
    public SceneApplication Visit(ScreenplaySyntax.ApplicationSyntax syntax) => Convert(syntax, null);

    /// <summary>
    /// Converts an application while retaining diagnostics for omitted Scene constructs.
    /// </summary>
    /// <param name="syntax">The application to convert.</param>
    /// <returns>The translated Scene with conversion diagnostics.</returns>
    public SceneApplication Convert(ScreenplaySyntax.ApplicationSyntax syntax) => Convert(syntax, new ConversionDiagnostics());

    static List<SceneLayouts.Layout> ConvertLayouts(ScreenplaySyntax.ApplicationSyntax syntax, BehaviorScope scope, ConversionDiagnostics? diagnostics) =>
        [
            .. (syntax.Layouts ?? [])
                .Select(layout => LayoutConverter.Convert(layout, diagnostics) with
                {
                    Behaviors = scope.Resolve(layout.Behaviors, layout.UsedBehaviors, layout.Name)
                })
        ];

    static void ConvertModuleScreens(
        ScreenplaySyntax.ModuleSyntax module,
        string layoutName,
        List<SceneScreens.Screen> screens,
        BehaviorScope scope)
    {
        var forms = module.Forms?.ToList() ?? [];
        var inherited = scope.Resolve(module.Behaviors, module.UsedBehaviors, module.Name);

        // What a module contributes belongs to every screen beneath it, recorded against the module alone.
        IReadOnlyList<string> path = [module.Name];
        IReadOnlyList<SceneContributionPoints.Contribution> contributed = [.. (module.Contributions ?? [])
            .Select((contribution, index) => ContributionConverter.Convert(contribution, $"{module.Name}.contribution[{index}]", path, scope.Diagnostics))];
        foreach (var feature in module.Features)
        {
            ConvertFeatureScreens(feature, path, layoutName, forms, screens, scope, inherited, contributed);
        }
    }

    static void ConvertFeatureScreens(
        ScreenplaySyntax.FeatureSyntax feature,
        IReadOnlyList<string> parentPath,
        string layoutName,
        IReadOnlyList<ScreenplaySyntax.FormSyntax> forms,
        List<SceneScreens.Screen> screens,
        BehaviorScope scope,
        IReadOnlyList<SceneModel.Behavior> inherited,
        IReadOnlyList<SceneContributionPoints.Contribution> moduleContributions)
    {
        IReadOnlyList<string> hierarchy = [.. parentPath, feature.Name];
        var path = string.Join('.', hierarchy);
        var contributions = moduleContributions
            .Concat((feature.Contributions ?? [])
                .Select((contribution, index) => ContributionConverter.Convert(contribution, $"{path}.contribution[{index}]", hierarchy, scope.Diagnostics)))
            .ToList();

        // Outermost first: a module's behaviors precede a feature's, which precede the screen's own. That is the
        // order the engine will run them in, and the reason a module can gate everything beneath it.
        IReadOnlyList<SceneModel.Behavior> inheritedHere =
            [.. inherited, .. scope.Resolve(feature.Behaviors, feature.UsedBehaviors, path)];

        screens.AddRange(feature.Slices
            .SelectMany(slice => slice.Screens)
            .Select(screen => ScreenConverter.Convert(screen, layoutName, forms, contributions, scope, inheritedHere, hierarchy, scope.Diagnostics))
            .OfType<SceneScreens.Screen>());

        foreach (var subFeature in feature.Features)
        {
            ConvertFeatureScreens(subFeature, hierarchy, layoutName, forms, screens, scope, inheritedHere, moduleContributions);
        }
    }

    SceneApplication Convert(ScreenplaySyntax.ApplicationSyntax syntax, ConversionDiagnostics? diagnostics)
    {
        new UiSyntaxAdmission(diagnostics).VisitApplication(syntax);
        var uiProfiles = syntax.UiProfiles?.SelectMany(profile => UiProfileConverter.Convert(profile, diagnostics)).ToList() ?? [];
        var themes = syntax.Themes?.Select(ThemeConverter.Convert).ToList() ?? [];
        var scope = BehaviorScope.For(syntax, _findings) with { Diagnostics = diagnostics };
        var layouts = ConvertLayouts(syntax, scope, diagnostics);

        var screenTemplates = new List<SceneScreens.ScreenTemplate>();
        var dialogTemplates = new List<SceneScreens.DialogTemplate>();
        var screens = new List<SceneScreens.Screen>();

        foreach (var module in syntax.Modules)
        {
            screenTemplates.AddRange((module.ScreenTemplates ?? []).Select(template =>
                ScreenTemplateConverter.Convert(template, scope, diagnostics) with
                {
                    Behaviors = scope.Resolve(template.Behaviors, template.UsedBehaviors, template.Name)
                }));
            dialogTemplates.AddRange((module.DialogTemplates ?? []).Select(template =>
                DialogTemplateConverter.Convert(template, scope, diagnostics) with
                {
                    Behaviors = scope.Resolve(template.Behaviors, template.UsedBehaviors, template.Name)
                }));
            ConvertModuleScreens(module, layouts.FirstOrDefault()?.Name ?? string.Empty, screens, scope);
        }

        var runtime = new SceneRuntimeAdmission();
        runtime.VisitApplication(syntax);

        var structure = scope.Structure ?? StructureConverter.Convert(syntax);

        return new SceneApplication(uiProfiles, themes, layouts, screenTemplates, dialogTemplates, screens)
        {
            Exposures = [.. (syntax.Exposures ?? []).Select(exposure => CompositionConverter.Exposure(exposure, diagnostics))],
            InstanceContributions = [.. (syntax.InstanceContributions ?? []).SelectMany(CompositionConverter.Instance)],
            Navigation = NavigationConverter.Convert(syntax, diagnostics, structure),
            Structure = structure,
            TemplateReferences = TemplateReferenceConverter.Collect(syntax),
            SampleDataPacks = SampleDataConverter.Packs(syntax),
            BlueprintSampleDataPacks = SampleDataConverter.BlueprintPacks(syntax),
            RuntimeIssues = runtime.Issues,
            ConversionDiagnostics = diagnostics?.All ?? []
        };
    }
}
