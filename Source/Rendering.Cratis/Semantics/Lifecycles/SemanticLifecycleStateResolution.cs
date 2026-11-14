// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Lifecycles;

/// <summary>
/// Derives, by convention, the read model member that records the state of a state machine. Screenplay declares no binding between a
/// machine's states and a member, so a single required text member named State, Status, CurrentState, CurrentStatus or the machine's
/// name followed by State or Status is taken to hold the state names.
/// </summary>
internal static class SemanticLifecycleStateResolution
{
    static readonly string[] _suffixes = ["Lifecycle", "StateMachine"];

    /// <summary>
    /// Finds the member of the followed read model that records the state of the machine.
    /// </summary>
    /// <param name="machine">The state machine.</param>
    /// <param name="model">The read model the machine follows.</param>
    /// <returns>The member when exactly one qualifies; otherwise null, because an ambiguous member is not derivable.</returns>
    internal static SemanticProperty? Member(SemanticStateMachine machine, SemanticReadModel model)
    {
        var names = Names(machine.Name);
        var candidates = model.Properties
            .Where(property => property is { Type: { Kind: SemanticTypeReferenceKind.Primitive, IsCollection: false, IsOptional: false, Primitive: SemanticPrimitiveType.Text } } && names.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();
        return candidates.Length == 1 ? candidates[0] : null;
    }

    /// <summary>
    /// Gets the values of the member, upper-cased, that select each state; a value is claimed by the first state that accepts it.
    /// </summary>
    /// <param name="machine">The state machine.</param>
    /// <returns>The state name and the values that select it.</returns>
    internal static IEnumerable<(string State, string[] Values)> Values(SemanticStateMachine machine)
    {
        var claimed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var state in machine.States)
        {
            var values = new[] { state.Name, Identifiers.ToPascalCase(state.Name) }
                .Select(value => value.ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .Where(claimed.Add)
                .ToArray();
            if (values.Length > 0) yield return (state.Name, values);
        }
    }

    static string[] Names(string machine)
    {
        var prefix = _suffixes.Where(suffix => machine.Length > suffix.Length && machine.EndsWith(suffix, StringComparison.Ordinal)).Select(suffix => machine[..^suffix.Length]).FirstOrDefault() ?? string.Empty;
        string[] names = ["State", "Status", "CurrentState", "CurrentStatus"];
        return prefix.Length == 0 ? names : [.. names, $"{prefix}State", $"{prefix}Status"];
    }
}
