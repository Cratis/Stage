// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Describes the shape of the composite and collection types an event property can carry.
/// </summary>
internal static class SemanticStructuredTypes
{
    /// <summary>
    /// Determines whether a type reaches only a finite set of members, which is true unless a composite type contains itself.
    /// </summary>
    /// <param name="plan">The semantic execution plan holding the model types.</param>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is not recursive; otherwise <see langword="false"/>.</returns>
    internal static bool HasFiniteShape(SemanticExecutionPlan plan, SemanticTypeReference type) => Finite(plan, type, []);

    static bool Finite(SemanticExecutionPlan plan, SemanticTypeReference type, HashSet<SemanticId> visiting)
    {
        if (type.Kind != SemanticTypeReferenceKind.CompositeType)
        {
            return true;
        }

        if (!visiting.Add(type.Target))
        {
            return false;
        }

        var finite = plan.Model.Application.Types.Single(candidate => candidate.Id == type.Target).Properties.All(property => Finite(plan, property.Type, visiting));
        visiting.Remove(type.Target);
        return finite;
    }
}
