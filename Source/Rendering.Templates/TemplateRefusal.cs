// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Scriban.Parsing;

namespace Cratis.Stage.Rendering.Templates;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "The profile contract keeps the internal engine exception out of its public API.")]
internal sealed class TemplateRefusal(TemplateDiagnostic diagnostic) : Exception(diagnostic.Message)
{
    internal TemplateDiagnostic Diagnostic { get; } = diagnostic;

    internal static TemplateRefusal At(string code, string message, SourceSpan span) => new(new(code, message, span.FileName ?? string.Empty, span.Start.Line + 1, span.Start.Column + 1));
}
