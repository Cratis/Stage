// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Contains the final Java text and allocated primary type for one symbol file.
/// </summary>
/// <param name="Id">The stable input file identity.</param>
/// <param name="Package">The input package.</param>
/// <param name="PrimaryType">The first top-level declaration in source order, or null for a file without one.</param>
/// <param name="Text">The fully substituted text. Authored native spans retain their original bytes.</param>
public sealed record ResolvedSymbolFile(string Id, string Package, TypeName? PrimaryType, string Text);
