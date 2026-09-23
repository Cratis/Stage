// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// The exception that is thrown when a projection syntax construct has no faithful Chronicle definition equivalent.
/// </summary>
/// <param name="construct">The unsupported syntax construct.</param>
public sealed class UnsupportedProjectionConversion(string construct)
    : Exception($"Projection construct '{construct}' cannot be converted without changing its meaning.");
