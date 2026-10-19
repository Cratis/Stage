// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// Reads the generated values a specification pins for the command it runs.
/// </summary>
/// <remarks>
/// <c>given generated &lt;Command&gt;.&lt;property&gt; = &lt;value&gt;</c> is declared on the specification, ahead of its action. A reaction takes the values pinned for the
/// command it invokes; a specification whose action is the command itself takes them as the generated values of that action, exactly as if the
/// action had stated them, so the generated property is a fixture rather than a draw (the destination of an event, the value an expectation names).
/// </remarks>
public static class SemanticSpecificationPins
{
    /// <summary>
    /// Moves the values a specification pins for its own command to the generated values of its action.
    /// </summary>
    /// <param name="specification">The specification.</param>
    /// <returns>The specification whose action carries every pinned value of its command; the specification itself when nothing is pinned.</returns>
    public static SemanticSpecification WithPinnedActionValues(this SemanticSpecification specification)
    {
        if (specification.When is not { } action || specification.GivenGenerated.IsDefaultOrEmpty)
        {
            return specification;
        }

        var existing = action.GeneratedValues.IsDefault ? [] : action.GeneratedValues;
        var pinned = specification.GivenGenerated
            .Where(value => value.Command == action.Command && !existing.Any(known => known.TargetProperty == value.Property))
            .GroupBy(value => value.Property)
            .Select(group => new SemanticPropertyValue(group.Key, group.First().Value))
            .ToArray();

        return pinned.Length == 0 ? specification : specification with { When = action with { GeneratedValues = [.. existing, .. pinned] } };
    }
}
