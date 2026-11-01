// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>A recoverable gap, distinct from a safety refusal.</summary>
public sealed record TemplateGap(string Code, GapRole Role, string Intent, string ModelPath, string Template, int Line);
