// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Authorization;

/// <summary>
/// The exception that is thrown when an authorization rendering outcome is not handled.
/// </summary>
/// <param name="outcome">The unhandled outcome type.</param>
public sealed class UnreachableAuthorizationRendering(Type outcome) : Exception(
    $"Authorization rendering outcome '{outcome.FullName}' is not handled");
