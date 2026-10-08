// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Specifications;

/// <summary>
/// The exception that is thrown when a legacy specification cannot identify which production supplies an expected event.
/// </summary>
/// <param name="specification">The authored specification name.</param>
/// <param name="eventType">The expected event type.</param>
public sealed class UnsupportedSpecificationDestination(string specification, string eventType) : Exception(
    $"Specification '{specification}' cannot unambiguously match '{eventType}' to a production destination; conditional repeated event types are not supported by the legacy specification renderer.");
