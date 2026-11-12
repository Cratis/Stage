// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// An extension component placed on a screen, with what the contract lets it reach.
/// </summary>
/// <param name="Screen">The name of the screen.</param>
/// <param name="System">The name of the system the component belongs to.</param>
/// <param name="Component">The name of the component in the system's extension contract.</param>
/// <param name="Kind">The <see cref="ExtensionComponentKind"/>.</param>
/// <param name="Invokes">The abilities the contract lets the component invoke. Anything else is refused.</param>
/// <param name="Signals">The signals the component raises, which the screen's <c lang="text">on &lt;signal&gt;</c> interactions handle.</param>
/// <param name="Policy">The policy the person using the screen must satisfy for the component to invoke anything, or empty when any authenticated person may.</param>
public sealed record ExtensionComponentPlacement(
    string Screen,
    string System,
    string Component,
    ExtensionComponentKind Kind,
    IReadOnlyList<string> Invokes,
    IReadOnlyList<string> Signals,
    string Policy = "");
