// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>A value or safety refusals. A refused result never exposes a value.</summary>
public sealed class TemplateResult<T>
{
    TemplateResult(T? value, ImmutableArray<TemplateDiagnostic> diagnostics)
    {
        Value = value;
        Diagnostics = diagnostics;
    }

    /// <summary>Gets whether evaluation succeeded.</summary>
    public bool Succeeded => Diagnostics.IsEmpty;

    /// <summary>Gets the value on success, otherwise default.</summary>
    public T? Value { get; }

    /// <summary>Gets the stable safety refusals.</summary>
    public ImmutableArray<TemplateDiagnostic> Diagnostics { get; }

    internal static TemplateResult<T> Success(T value) => new(value, []);

    internal static TemplateResult<T> Refused(params TemplateDiagnostic[] diagnostics) => new(default, [.. diagnostics]);
}
