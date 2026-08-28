// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// The exception that is thrown when a Cratis backend application scaffold contract is malformed.
/// </summary>
/// <param name="message">The reason the scaffold contract is malformed.</param>
public sealed class InvalidCratisBackendApplicationScaffold(string message) : Exception(message);
