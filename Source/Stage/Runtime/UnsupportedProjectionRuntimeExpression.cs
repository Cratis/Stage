// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Runtime;

/// <summary>
/// The exception that is thrown when a projection expression cannot be safely adapted to the pinned runtime.
/// </summary>
/// <param name="expression">The expression that cannot be translated.</param>
public sealed class UnsupportedProjectionRuntimeExpression(string expression)
    : Exception($"Projection expression '{expression}' cannot be safely translated for the pinned Chronicle runtime.");
