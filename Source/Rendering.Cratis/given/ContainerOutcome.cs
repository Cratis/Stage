// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// The result of creating or starting a container.
/// </summary>
/// <param name="Succeeded">Whether the step succeeded.</param>
/// <param name="Detail">Evidence text for a failure, otherwise empty.</param>
public sealed record ContainerOutcome(bool Succeeded, string Detail);
