// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Commands;

/// <summary>
/// Represents how one property of a produced event gets its value.
/// </summary>
/// <param name="Property">The name of the property on the event payload.</param>
/// <param name="Kind">Where the value comes from.</param>
/// <param name="Expression">The source, interpreted according to <paramref name="Kind"/> — a command property name, JSON literal text, an identity path, an environment variable name or a template.</param>
public record ProducedEventProperty(string Property, ProducedValueKind Kind, string Expression);
