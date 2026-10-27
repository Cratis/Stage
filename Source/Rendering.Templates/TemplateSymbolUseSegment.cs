// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>A symbol reference to be qualified by the renderer.</summary>
public sealed record TemplateSymbolUseSegment(SymbolDescriptor Symbol) : TemplateSegment;
