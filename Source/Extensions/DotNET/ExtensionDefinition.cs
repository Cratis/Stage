// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Extensions;

/// <summary>
/// An extension backend: what it realizes and how. Build one with <see cref="Extension.Create"/>.
/// </summary>
public sealed class ExtensionDefinition
{
    internal ExtensionDefinition(
        string name,
        string version,
        ImmutableArray<AbilityRegistration> abilities,
        ImmutableArray<string> triggers,
        ImmutableArray<(string Name, bool Secret, bool Required)> settings,
        ImmutableArray<(string Prefix, Func<HttpCall, CancellationToken, Task<HttpReply>> Handler)> routes)
    {
        Name = name;
        Version = version;
        Abilities = abilities;
        Triggers = triggers;
        Settings = settings;
        Routes = routes;
    }

    /// <summary>
    /// Gets the extension's name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the extension's version.
    /// </summary>
    public string Version { get; }

    internal ImmutableArray<AbilityRegistration> Abilities { get; }

    internal ImmutableArray<string> Triggers { get; }

    internal ImmutableArray<(string Name, bool Secret, bool Required)> Settings { get; }

    internal ImmutableArray<(string Prefix, Func<HttpCall, CancellationToken, Task<HttpReply>> Handler)> Routes { get; }
}

internal sealed record AbilityRegistration(string Name, int ContractMajor, EffectClass Effect, ImmutableArray<string> Outcomes, Func<AbilityContext, Task<AbilityOutcome>> Handler);
