// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// The single place that names the classes generated for a semantic specification, shared by the renderers and the generated-type inventory.
/// </summary>
internal static class SemanticSpecificationClassNames
{
    /// <summary>
    /// Gets the class name of a specification that is named after the specification alone.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <returns>The class name.</returns>
    internal static string Behavior(SemanticSpecification specification) =>
        $"when_{Identifiers.ToSnakeCase(specification.Name)}";

    /// <summary>
    /// Gets the class name of a specification that asserts a read model.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="readModel">The asserted read model.</param>
    /// <param name="absentIndex">The position of the absence expectation, or null for a present one.</param>
    /// <returns>The class name.</returns>
    internal static string Projected(SemanticSpecification specification, SemanticReadModel readModel, int? absentIndex)
    {
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}_is_projected";
        if (specification.ThenReadModels.Length > 1)
        {
            behavior += $"_into_{Identifiers.ToSnakeCase(readModel.Name)}";
        }
        if (absentIndex is { } index)
        {
            behavior += $"_without_{Identifiers.ToSnakeCase(readModel.Name)}_{index + 1}";
        }

        return behavior;
    }

    /// <summary>
    /// Gets the class name of a specification that asserts a query result.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="query">The asserted query.</param>
    /// <param name="allowThrough">Whether several asserted queries are told apart by the query name.</param>
    /// <returns>The class name.</returns>
    internal static string Queried(SemanticSpecification specification, SemanticKeyedQuery query, bool allowThrough)
    {
        var name = $"when_{Identifiers.ToSnakeCase(specification.Name)}_is_queried";
        if (allowThrough && specification.ThenQueries.Length > 1)
        {
            name += $"_through_{Identifiers.ToSnakeCase(query.Name)}";
        }

        return name;
    }
}
