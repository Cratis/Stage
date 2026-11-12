// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Ui;
using Cratis.Stage.Host.Extensions.Activation;
using Cratis.Stage.Host.Extensions.Packages;
using Cratis.Stage.Host.Invocations;

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// Builds what the host knows about extensions from the two places that own it: the compiled model, which says which systems exist, which
/// abilities they offer and which screens place which components, and the loaded packages, whose contracts say what each component may
/// invoke, which signals it raises and which settings its system declares.
/// </summary>
public static class ExtensionModelCatalogs
{
    /// <summary>
    /// The contract major every ability starts at. A model does not version an ability yet.
    /// </summary>
    public const int ContractMajor = 1;

    /// <summary>
    /// Describes the abilities of every system a loaded package realizes.
    /// </summary>
    /// <param name="application">The <see cref="SemanticApplication"/>.</param>
    /// <param name="packages">The <see cref="IExtensionPackages"/>.</param>
    /// <returns>The <see cref="AbilityDescriptor"/> of each ability. A system no package realizes has none, and an invocation of it is refused.</returns>
    public static IReadOnlyList<AbilityDescriptor> Abilities(SemanticApplication application, IExtensionPackages packages) =>
    [
        .. application.Systems
            .Where(system => packages.ForSystem(system.Name) is not null)
            .SelectMany(system => system.Abilities.Select(ability => Describe(system.Name, ability)))
    ];

    /// <summary>
    /// Finds every extension component a screen places and what its package's contract lets it do.
    /// </summary>
    /// <param name="contract">The <see cref="UiContract"/> of the model, which says which screen places which component of which system.</param>
    /// <param name="packages">The <see cref="IExtensionPackages"/>.</param>
    /// <returns>The placements. A component the package's contract does not declare is not placed.</returns>
    public static IReadOnlyList<ExtensionComponentPlacement> Placements(UiContract contract, IExtensionPackages packages)
    {
        var placements = new List<ExtensionComponentPlacement>();
        foreach (var screen in contract.Screens)
        {
            foreach (var placed in (screen.Scene?.Components ?? []).Select(component => component.Extension).OfType<UiExtensionComponent>())
            {
                var declared = packages.ForSystem(placed.System)?.Contract.Components.FirstOrDefault(component => component.Name == placed.Component);
                if (declared is null) continue;

                placements.Add(new(screen.Name, placed.System, declared.Name, KindOf(placed.Kind ?? declared.Kind), [.. declared.Invokes], [.. declared.Signals.Select(signal => signal.Name)]));
            }
        }

        return placements;
    }

    /// <summary>
    /// Declares the settings of every system a loaded package realizes, from the package's contract.
    /// </summary>
    /// <param name="application">The <see cref="SemanticApplication"/>.</param>
    /// <param name="packages">The <see cref="IExtensionPackages"/>.</param>
    /// <returns>
    /// The declarations. Only a setting the contract exposes as public may reach a browser, so the others are declared as not public,
    /// whether the contract calls them secret or backend.
    /// </returns>
    public static IReadOnlyList<SettingDeclaration> Settings(SemanticApplication application, IExtensionPackages packages) =>
    [
        .. application.Systems
            .Select(system => (system.Name, Package: packages.ForSystem(system.Name)))
            .Where(realized => realized.Package is not null)
            .SelectMany(realized => realized.Package!.Contract.Settings.Select(setting => new SettingDeclaration(realized.Name, setting.Name, setting.Exposure != ExtensionSettingExposure.Public)))
    ];

    /// <summary>
    /// Finds the systems each screen needs, so a backend can be started when someone reaches the screen.
    /// </summary>
    /// <param name="contract">The <see cref="UiContract"/> of the model, which lists the extensions each screen needs.</param>
    /// <returns>The <see cref="IScreenExtensionUsage"/>.</returns>
    public static ModelScreenExtensionUsage Usage(UiContract contract) =>
        new(contract.Screens.ToDictionary(
            screen => screen.Name,
            screen => (IReadOnlyList<string>)[.. (screen.Extensions ?? []).Select(need => need.System).Distinct(StringComparer.Ordinal)],
            StringComparer.OrdinalIgnoreCase));

    static ExtensionComponentKind KindOf(string? kind) => kind switch
    {
        "mount" => ExtensionComponentKind.Mount,
        "screenplay" => ExtensionComponentKind.Screenplay,
        _ => ExtensionComponentKind.React
    };

    static AbilityDescriptor Describe(string system, SemanticAbility ability) => new(
        new(system, ability.Name, ContractMajor),
        ability.Effect switch
        {
            SemanticAbilityEffect.Safe => EffectClass.Safe,
            SemanticAbilityEffect.Idempotent => EffectClass.Idempotent,
            _ => EffectClass.Once
        },
        ability.Outcomes,
        string.Empty)
    {
        AnswersLater = ability.AnswersLater
    };
}
