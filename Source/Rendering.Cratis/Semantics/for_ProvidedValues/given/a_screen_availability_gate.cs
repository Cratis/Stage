// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Arc.Availability;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;

internal sealed class a_screen_availability_gate(IReadOnlyCollection<Type> parts, Type closed) : IAvailabilityGate
{
    public IReadOnlyCollection<Type> GatedParts => parts;

    public bool IsGated(Type part) => parts.Contains(part);

    public ValueTask<AvailabilityDecision> Decide(Type part, IServiceProvider services, CancellationToken cancellationToken) =>
        ValueTask.FromResult(part.Equals(closed) ? new(AvailabilityVerdict.Unavailable, ["Plus"]) : AvailabilityDecision.Available);

    public IDisposable OnChanged(Func<ValueTask> handler) => throw new NotSupportedException("Snapshot specifications do not subscribe to changes.");
}
#endif
