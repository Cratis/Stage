// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception that is thrown when a Screenplay <c>arrangement</c> container has an
/// <see cref="ArrangementContainerKind"/> that <see cref="ArrangementConverter"/> does not yet know how to
/// convert.
/// </summary>
/// <param name="kind">The unrecognized <see cref="ArrangementContainerKind"/>.</param>
public class UnknownArrangementContainerKind(ArrangementContainerKind kind) : Exception($"'{kind}' is not a known arrangement container kind");
