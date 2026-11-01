// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Emission;

/// <summary>
/// The exception thrown when a generated file would escape its output root.
/// </summary>
/// <param name="relativePath">The generated file's supplied path.</param>
/// <param name="root">The absolute output root.</param>
public sealed class GeneratedPathOutsideOutputRoot(string relativePath, string root) : Exception(
    $"Generated path '{relativePath}' is not strictly inside output root '{root}'.");
