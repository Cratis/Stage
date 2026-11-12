// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// An <see cref="IHttpAbilityDefinitions"/> holding the definitions it was given.
/// </summary>
/// <param name="definitions">The <see cref="HttpAbilityDefinition"/> to hold.</param>
public sealed class HttpAbilityDefinitions(IEnumerable<HttpAbilityDefinition> definitions) : IHttpAbilityDefinitions
{
    readonly Dictionary<(string System, string Ability), HttpAbilityDefinition> _definitions =
        definitions.ToDictionary(definition => (definition.System, definition.Ability));

    /// <inheritdoc/>
    public HttpAbilityDefinition? Find(string system, string ability) => _definitions.GetValueOrDefault((system, ability));
}
