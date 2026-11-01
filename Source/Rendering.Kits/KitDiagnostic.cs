// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Kits;

/// <summary>A kit rejection at a source or JSON path.</summary>
/// <param name="Code">Stable rejection code.</param>
/// <param name="Message">Author-facing explanation.</param>
/// <param name="Path">Source or JSON path.</param>
public sealed record KitDiagnostic(string Code, string Message, string Path);
