// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>A semantic type with an optional element and optionality.</summary>
public sealed record TemplateType(string Kind, TemplateType? Element = null, bool Optional = false);
