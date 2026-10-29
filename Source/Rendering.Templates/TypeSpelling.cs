// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>
/// Structured target type spelling.
/// </summary>
/// <param name="Parts">Text and symbol parts.</param>
public sealed record TypeSpelling(ImmutableArray<TemplateSegment> Parts);
