// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// The exception that is thrown when an artifact-render contract is malformed.
/// </summary>
/// <param name="message">The reason the contract is malformed.</param>
public sealed class InvalidArtifactRenderContract(string message) : Exception(message);
