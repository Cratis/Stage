// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// A setting a system declares. A setting is public or secret, never both, so a secret cannot reach a browser.
/// </summary>
/// <param name="System">The name of the system.</param>
/// <param name="Name">The name of the setting.</param>
/// <param name="IsSecret">Whether the setting is secret. A setting that is not secret is public.</param>
public sealed record SettingDeclaration(string System, string Name, bool IsSecret);
