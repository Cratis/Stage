// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// A successful outer response cannot be retained across a nested command by the pinned Scene runtime.
/// </summary>
/// <param name="name">The captured response name.</param>
public class UnsupportedFormResponseCapture(string name) : Exception($"Form continuation captures outer response '{name}' across a nested command; the Scene runtime cannot preserve that response scope.");
