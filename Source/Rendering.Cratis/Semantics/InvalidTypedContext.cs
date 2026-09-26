// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Signals a descriptor the C# provider cannot interpret without guessing.</summary>
/// <param name="message">The precise reason.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Only the internal renderer throws this diagnostic exception; it is not a public API.")]
internal sealed class InvalidTypedContext(string message) : Exception(message);
