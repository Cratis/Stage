// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Preserves model-owned enum numbers in generated and analysis declarations.
/// </summary>
internal static class SemanticEnumMembers
{
    internal static IEnumerable<string> Declarations(SemanticConcept concept) =>
        Declarations(concept.Values, concept.Numbers is { } numbers ? numbers : null);

    internal static IEnumerable<string> Declarations(IEnumerable<string> values, IEnumerable<int>? numbers)
    {
        var identities = numbers?.ToArray();

        return values.Select((value, index) => identities is not null
            ? $"{Identifiers.ToPascalCase(value)} = {identities[index].ToString(CultureInfo.InvariantCulture)}"
            : Identifiers.ToPascalCase(value));
    }
}
