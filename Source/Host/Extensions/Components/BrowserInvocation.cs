// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// An invocation an extension component asks for from the browser.
/// </summary>
/// <param name="Screen">The name of the screen the component is placed on.</param>
/// <param name="System">The name of the system the component belongs to.</param>
/// <param name="Component">The name of the component.</param>
/// <param name="Ability">The name of the ability to invoke.</param>
/// <param name="Interaction">The identity of the interaction, chosen by the browser. Asking again with the same one finds the same invocation, so a retry or a double click does not do the effect twice.</param>
/// <param name="Input">The input of the ability, as JSON.</param>
public sealed record BrowserInvocation(string Screen, string System, string Component, string Ability, string Interaction, string Input = "{}");
