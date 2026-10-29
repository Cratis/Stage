// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering.Model;

/// <summary>
/// The exception that is thrown when a render model uses an unsupported read contract.
/// </summary>
/// <param name="message">The reason the render model cannot be read.</param>
public sealed class InvalidRenderModel(string message) : Exception(message);
