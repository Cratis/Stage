// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>Prepared sources or safety refusals. Refusals have no usable preparation.</summary>
public sealed record TemplatePrepareResult(PreparedTemplates? Prepared, ImmutableArray<TemplateDiagnostic> Diagnostics);
