// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Prevents syntax-based Stage paths from interpreting one generation as the entire event contract.
/// The executable model does not yet carry event lineage.
/// </summary>
public static class EventGenerationAdmission
{
    /// <summary>
    /// Rejects marked or multiply declared events before their properties can be used as a contract.
    /// </summary>
    /// <param name="events">The event declarations in one slice.</param>
    /// <param name="slicePath">The slice being inspected.</param>
    public static void EnsureSupported(IEnumerable<EventSyntax> events, string slicePath)
    {
        foreach (var group in events.GroupBy(@event => @event.Name, StringComparer.Ordinal))
        {
            if (group.Count() > 1 || group.Any(@event => @event.HasGenerationMarker))
            {
                throw new InvalidEventModel(
                    slicePath,
                    [$"STAGE-EVENT-001: Event '{group.Key}' declares generations; Stage cannot use its event shape until the executable model carries generation lineage."]);
            }
        }
    }
}
