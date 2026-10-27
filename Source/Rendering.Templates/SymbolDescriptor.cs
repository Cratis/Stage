// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>Target-neutral symbol identity and optional semantic hints.</summary>
public sealed record SymbolDescriptor(string Identity, string? Namespace = null, string? Name = null);
