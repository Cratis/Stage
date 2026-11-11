// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Where an ability invocation came from: the behavior and the occurrence that caused it.
/// </summary>
/// <param name="Kind">The <see cref="InvocationOriginKind"/>.</param>
/// <param name="Address">The semantic address of the invoking use in the model, such as <c lang="text">Fulfillment/PostToWarehouse/branch 2</c>. It does not change between occurrences.</param>
/// <param name="Causation">What caused this occurrence: the triggering event's sequence number, the command's correlation, the transition's event, or the run's node execution. It never includes the attempt.</param>
[GenerateSerializer]
[Alias("Cratis.Stage.Host.Invocations.InvocationOrigin")]
public sealed record InvocationOrigin(
    [property: Id(0)] InvocationOriginKind Kind,
    [property: Id(1)] string Address,
    [property: Id(2)] string Causation);
