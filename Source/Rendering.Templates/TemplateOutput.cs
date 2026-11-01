// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>Structured output and recoverable degradation gaps.</summary>
public sealed record TemplateOutput(ImmutableArray<TemplateSegment> Segments, ImmutableArray<TemplateGap> Gaps);
