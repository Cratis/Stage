// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>
/// A manifest-expression value or a whole-plan safety refusal.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <param name="Value">Value on success.</param>
/// <param name="Refusal">Safety refusal, otherwise null.</param>
public sealed record TemplateResult<T>(T? Value, TemplateDiagnostic? Refusal)
{
    /// <summary>
    /// Gets whether evaluation succeeded.
    /// </summary>
    public bool Succeeded => Refusal is null;

    internal static TemplateResult<T> Success(T value) => new(value, null);

    internal static TemplateResult<T> Refused(TemplateDiagnostic diagnostic) => new(default, diagnostic);
}
