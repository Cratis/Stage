// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception that is thrown when a Screenplay <c>target size</c>/<c>when width</c>/<c>when height</c>
/// value does not match a known <c>WidthSizeClass</c>/<c>HeightSizeClass</c> member. Screenplay's own
/// compiler does not validate this name against a known set, so a diagnostic-clean tree can still carry
/// one the Scene translation cannot resolve.
/// </summary>
/// <param name="name">The unrecognized size class name.</param>
public class UnknownSizeClassName(string name) : Exception($"'{name}' is not a known size class name - expected 'compact' or 'regular'");
