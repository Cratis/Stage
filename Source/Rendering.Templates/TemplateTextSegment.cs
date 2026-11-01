// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>Literal output text.</summary>
public sealed record TemplateTextSegment(string Text) : TemplateSegment;
