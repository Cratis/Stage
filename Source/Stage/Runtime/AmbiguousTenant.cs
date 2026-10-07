// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Runtime;

/// <summary>
/// The exception that is thrown when a named provider tenant collides with a portable sentinel.
/// </summary>
/// <param name="value">The conflicting provider tenant name.</param>
public sealed class AmbiguousTenant(string value) : Exception($"Provider tenant '{value}' collides with a reserved portable tenant identifier.");
