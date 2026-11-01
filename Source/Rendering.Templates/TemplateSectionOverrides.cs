// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>Replacement bodies keyed by declared template and section.</summary>
/// <param name="Bodies">Section bodies to parse and lint during preparation.</param>
public sealed record TemplateSectionOverrides(IReadOnlyDictionary<(string Template, string Section), string> Bodies);
