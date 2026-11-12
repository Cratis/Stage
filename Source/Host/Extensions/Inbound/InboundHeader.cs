// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Inbound;

/// <summary>
/// A header of a stored request.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="Values">The values.</param>
[GenerateSerializer]
[Alias("Cratis.Stage.Host.Extensions.Inbound.InboundHeader")]
public sealed record InboundHeader(
    [property: Id(0)] string Name,
    [property: Id(1)] string[] Values);
