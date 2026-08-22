// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Concepts;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts the Screenplay <c>concept</c> declarations of an application into Stage
/// <see cref="ConceptDefinition"/> records.
/// </summary>
/// <remarks>
/// A concept already reaches a consumer indirectly, as the <see cref="SchemaSynthesizer.ConceptKeyword"/> and
/// <see cref="SchemaSynthesizer.ConceptAttributesKeyword"/> annotations on the properties typed as one. That
/// only names the concepts some property happens to use, so a concept declared and not yet used arrived
/// nowhere at all, and the enumeration values of one arrived only on the properties using it.
/// </remarks>
public static class ConceptConverter
{
    /// <summary>
    /// Converts an application's concept declarations into their Stage records.
    /// </summary>
    /// <param name="concepts">The concept declarations.</param>
    /// <param name="modelName">The name of the event model, used to derive stable identifiers.</param>
    /// <returns>The Stage concept definitions, in declaration order.</returns>
    public static IReadOnlyList<ConceptDefinition> Convert(IEnumerable<ConceptSyntax> concepts, string modelName) =>
    [
        .. concepts.Select(concept => new ConceptDefinition(
            DeterministicId.From($"model:{modelName}:concept:{concept.Name}"),
            concept.Name,
            concept.Type,
            concept.IsEnum,
            [.. concept.Values],
            [.. concept.Attributes.Select(attribute => new ConceptAttribute(attribute.Name, attribute.Reason ?? string.Empty))]))
    ];
}
