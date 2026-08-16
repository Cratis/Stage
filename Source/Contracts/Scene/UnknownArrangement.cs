// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception that is thrown when an arrangement is a subtype of <c>Arrangement</c> that
/// <see cref="ArrangementSelector"/> does not yet know how to evaluate. Unlike a
/// <see cref="RenderFinding"/> - which reports something the authored model left unresolved - this means
/// <c>Cratis.Scene.Model</c> gained an arrangement shape Stage has not been taught, which no authored input
/// can cause and no build can work around.
/// </summary>
/// <param name="typeName">The name of the unrecognized <c>Arrangement</c> subtype.</param>
public class UnknownArrangement(string typeName) : Exception($"'{typeName}' is not a known arrangement");
