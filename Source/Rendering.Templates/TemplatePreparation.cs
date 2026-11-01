// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Templates;

internal sealed record TemplatePreparation(PreparedTemplateKit? Value, ImmutableArray<TemplateDiagnostic> Diagnostics)
{
    internal bool Succeeded => Diagnostics.IsEmpty;
}
