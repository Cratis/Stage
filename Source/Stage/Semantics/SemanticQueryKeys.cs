// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Semantics;

internal static class SemanticQueryKeys
{
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
