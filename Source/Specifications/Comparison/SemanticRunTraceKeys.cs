// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.Comparison;

internal static class SemanticRunTraceKeys
{
    internal static string Canonical(SemanticValue key, SemanticReadModel model)
    {
        if (key is not SemanticCompositeValue composite) return SemanticRunContext.Canonical(key);

        // Use the same ordinal part-name ordering as SemanticQueryKeys.ToText, not declaration order.
        var properties = model.Properties.Where(property => property.IsIdentifier).ToDictionary(property => property.Id);
        var parts = composite.Properties.OrderBy(part => properties[part.TargetProperty].Name, StringComparer.Ordinal)
            .ToDictionary(part => properties[part.TargetProperty].Name, part => JsonSerializer.Deserialize<JsonElement>(SemanticRunContext.Canonical(part.Value)), StringComparer.Ordinal);

        return JsonSerializer.Serialize(parts);
    }
}
