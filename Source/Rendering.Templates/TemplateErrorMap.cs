// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Scriban.Syntax;

namespace Cratis.Stage.Rendering.Templates;

internal static class TemplateErrorMap
{
    internal static TemplateDiagnostic Map(Exception error)
    {
        for (var inner = error; inner is not null; inner = inner.InnerException)
        {
            if (inner is TemplateRefusal refusal) return refusal.Diagnostic;
            if (inner is TemplateLanguageRefusal language) return new(TemplateCodes.Input, language.Message, string.Empty, 0, 0);
            if (inner is OperationCanceledException) return new(TemplateCodes.Deadline, "Plan deadline exceeded.", string.Empty, 0, 0);
        }

        var message = error.Message;
        var code = TemplateCodes.Input;
        if (error is ScriptAbortException || message.Contains("cancelled", StringComparison.Ordinal)) code = TemplateCodes.Deadline;
        else if (message.Contains("iteration limit", StringComparison.Ordinal) || message.Contains("LoopLimit", StringComparison.Ordinal)) code = TemplateCodes.Steps;
        else if (message.Contains("LimitToString", StringComparison.Ordinal)) code = TemplateCodes.StringLength;
        else if (message.Contains("OutputLimit", StringComparison.Ordinal)) code = TemplateCodes.Output;
        else if (message.Contains("recursive depth limit", StringComparison.Ordinal)) code = TemplateCodes.Recursion;
        else if (message.Contains("readonly", StringComparison.Ordinal)) code = TemplateCodes.Mutation;
        return error is ScriptRuntimeException runtime
            ? new(code, message, runtime.Span.FileName ?? string.Empty, runtime.Span.Start.Line + 1, runtime.Span.Start.Column + 1)
            : new(code, message, string.Empty, 0, 0);
    }
}
