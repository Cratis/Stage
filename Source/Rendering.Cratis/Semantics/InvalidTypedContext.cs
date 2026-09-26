// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Signals a descriptor the C# provider cannot interpret without guessing.</summary>
/// <param name="message">The precise reason.</param>
public sealed class InvalidTypedContext(string message) : Exception(message)
{
    /// <summary>The diagnostic emitted when planning a descriptor that cannot be admitted.</summary>
    public const string DiagnosticCode = "STAGE-ESM-021";
}
