// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// An input that could not be compiled into an executable semantic model and plan.
/// </summary>
/// <param name="diagnostics">The compilation or admission diagnostics.</param>
public sealed class InvalidSemanticModel(IReadOnlyList<string> diagnostics) : Exception(string.Join(Environment.NewLine, diagnostics))
{
    /// <summary>
    /// The diagnostics in compiler order.
    /// </summary>
    public IReadOnlyList<string> Diagnostics { get; } = diagnostics;
}
