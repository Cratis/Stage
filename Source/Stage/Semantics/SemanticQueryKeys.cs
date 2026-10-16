// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Semantics;

internal static class SemanticQueryKeys
{
    /// <summary>
    /// Builds the composite key of a single-instance query by key parts from the caller's text arguments.
    /// </summary>
    /// <param name="query">The query whose key parts are supplied.</param>
    /// <param name="argument">Looks up the text a caller supplied for an argument name, or null when it is absent.</param>
    /// <param name="application">The application declaring the part concepts.</param>
    /// <returns>One value per supplied part, keyed by its read-model property; the evaluator rejects a missing part.</returns>
    internal static SemanticValue FromParts(SemanticKeyedQuery query, Func<string, string?> argument, SemanticApplication application) =>
        SemanticValue.Composite([.. query.KeyParts
            .Select(part => (part, text: argument(part.Name)))
            .Where(supplied => supplied.text is not null)
            .Select(supplied => new SemanticPropertyValue(supplied.part.KeyProperty, From(supplied.text, supplied.part.Type, application)))]);

    /// <summary>
    /// Converts an instance key to its JSON-compatible form: the single identifier's value, or an object keyed by part name.
    /// </summary>
    /// <param name="key">The instance key.</param>
    /// <param name="model">The read model the instance belongs to.</param>
    /// <param name="application">The application declaring the key types.</param>
    /// <returns>The JSON-compatible key.</returns>
    internal static object? ToObject(SemanticValue key, SemanticReadModel model, SemanticApplication application)
    {
        var identifiers = model.Properties.Where(property => property.IsIdentifier).ToArray();
        if (identifiers.Length < 2 || key is not SemanticCompositeValue composite)
        {
            return SemanticJsonValues.ToObject(key, identifiers.Length == 1 ? identifiers[0].Type : null, application);
        }

        return composite.Properties
            .Select(part => (part, property: identifiers.Single(identifier => identifier.Id == part.TargetProperty)))
            .OrderBy(entry => entry.property.Name, StringComparer.Ordinal)
            .ToDictionary(entry => entry.property.Name, entry => SemanticJsonValues.ToObject(entry.part.Value, entry.property.Type, application), StringComparer.Ordinal);
    }

    /// <summary>
    /// Converts a composite instance key to the text a read-model document carries as its identity.
    /// </summary>
    /// <param name="key">The composite instance key.</param>
    /// <param name="model">The read model with several key parts.</param>
    /// <param name="application">The application declaring the part types.</param>
    /// <returns>The JSON object of the key's parts, ordered by part name.</returns>
    internal static string ToText(SemanticValue key, SemanticReadModel model, SemanticApplication application) =>
        JsonSerializer.Serialize(ToObject(key, model, application));

    /// <summary>
    /// Checks whether a read model is identified by several key parts.
    /// </summary>
    /// <param name="model">The read model.</param>
    /// <returns>Whether the read model has two or more key parts.</returns>
    internal static bool IsComposite(SemanticReadModel model) => model.Properties.Count(property => property.IsIdentifier) > 1;

    internal static SemanticValue From(string? key, SemanticTypeReference type, SemanticApplication application)
    {
        if (key is null)
        {
            return SemanticValue.Null;
        }

        var primitive = type.Kind == SemanticTypeReferenceKind.Concept
            ? application.Concepts.Single(concept => concept.Id == type.Target).Primitive
            : type.Primitive;
        if (primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber &&
            decimal.TryParse(key, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            return SemanticValue.Number(number);
        }

        return primitive == SemanticPrimitiveType.Boolean && bool.TryParse(key, out var boolean)
            ? SemanticValue.Boolean(boolean)
            : SemanticValue.Text(key);
    }
}
