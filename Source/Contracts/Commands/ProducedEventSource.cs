// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Commands;

/// <summary>
/// Represents where the event source a produced event is appended to comes from — the modeled
/// <c>produces … for</c> clause.
/// </summary>
/// <param name="Kind">Where the value comes from.</param>
/// <param name="Expression">The source, interpreted according to <paramref name="Kind"/> — a command property name,
/// JSON literal text, an identity path, an environment variable name or a template.</param>
/// <remarks>
/// Carries no property name, unlike <see cref="ProducedEventProperty"/>: the value identifies the stream the event
/// is appended to rather than filling a property of its payload. A <c>produces</c> with none of these lands on the
/// command's own event source, which is the common case and stays unstated.
/// </remarks>
public record ProducedEventSource(ProducedValueKind Kind, string Expression);
