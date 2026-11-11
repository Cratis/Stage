// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Extensions;

/// <summary>
/// Describes an extension backend. Declare what it realizes, then run it with <see cref="ExtensionRunner"/>.
/// </summary>
public sealed class Extension
{
    readonly string _name;
    readonly string _version;
    readonly List<AbilityRegistration> _abilities = [];
    readonly List<string> _triggers = [];
    readonly List<(string Name, bool Secret, bool Required)> _settings = [];
    readonly List<(string Prefix, Func<HttpCall, CancellationToken, Task<HttpReply>> Handler)> _routes = [];

    Extension(string name, string version)
    {
        _name = name;
        _version = version;
    }

    /// <summary>
    /// Starts describing an extension.
    /// </summary>
    /// <param name="name">The extension's name.</param>
    /// <param name="version">The extension's version.</param>
    /// <returns>The <see cref="Extension"/>.</returns>
    public static Extension Create(string name, string version) => new(name, version);

    /// <summary>
    /// Declares an ability the extension realizes. Stage checks it against the contract of the system at the handshake.
    /// </summary>
    /// <param name="name">The name of the ability.</param>
    /// <param name="effect">The <see cref="EffectClass"/> the contract declares.</param>
    /// <param name="handler">Performs one attempt.</param>
    /// <param name="outcomes">The named outcomes the contract declares besides returned and failed.</param>
    /// <param name="contractMajor">The major version of the ability's contract.</param>
    /// <returns>The same <see cref="Extension"/>.</returns>
    public Extension Ability(string name, EffectClass effect, Func<AbilityContext, Task<AbilityOutcome>> handler, string[]? outcomes = null, int contractMajor = 1)
    {
        _abilities.RemoveAll(existing => existing.Name == name);
        _abilities.Add(new AbilityRegistration(name, contractMajor, effect, [.. outcomes ?? []], handler));
        return this;
    }

    /// <summary>
    /// Declares a trigger the extension raises.
    /// </summary>
    /// <param name="name">The name of the trigger.</param>
    /// <returns>The same <see cref="Extension"/>.</returns>
    public Extension Trigger(string name)
    {
        _triggers.Add(name);
        return this;
    }

    /// <summary>
    /// Declares a public setting the extension reads.
    /// </summary>
    /// <param name="name">The name of the setting.</param>
    /// <param name="required">Whether the extension cannot work without it.</param>
    /// <returns>The same <see cref="Extension"/>.</returns>
    public Extension Setting(string name, bool required = false)
    {
        _settings.Add((name, false, required));
        return this;
    }

    /// <summary>
    /// Declares a secret the extension reads. It is delivered by the host services and nowhere else.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="required">Whether the extension cannot work without it.</param>
    /// <returns>The same <see cref="Extension"/>.</returns>
    public Extension Secret(string name, bool required = false)
    {
        _settings.Add((name, true, required));
        return this;
    }

    /// <summary>
    /// Handles inbound HTTP requests below a path prefix, such as a webhook.
    /// </summary>
    /// <param name="prefix">The path prefix, starting with a slash.</param>
    /// <param name="handler">Handles a request.</param>
    /// <returns>The same <see cref="Extension"/>.</returns>
    public Extension Http(string prefix, Func<HttpCall, CancellationToken, Task<HttpReply>> handler)
    {
        _routes.Add((prefix, handler));
        return this;
    }

    /// <summary>
    /// Completes the description.
    /// </summary>
    /// <returns>The <see cref="ExtensionDefinition"/>.</returns>
    public ExtensionDefinition Build() => new(_name, _version, [.. _abilities], [.. _triggers], [.. _settings], [.. _routes]);
}
