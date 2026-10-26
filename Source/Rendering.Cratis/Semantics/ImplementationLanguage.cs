// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class ImplementationLanguage
{
    internal const string Remedy = "Supply the body in C# (or render it with a target that realizes this language) and render again.";

    internal static bool IsCSharp(SemanticImplementationRequirement requirement) =>
        (requirement.Language == "csharp" && requirement.File is null) ||
        (requirement.Language is null && requirement.File?.EndsWith(".cs", StringComparison.Ordinal) == true);

    internal static string Describe(SemanticImplementationRequirement requirement)
    {
        var language = requirement.Language;
        if (string.IsNullOrWhiteSpace(language) || language == "csharp")
        {
            language = Path.GetExtension(requirement.File)?.TrimStart('.');
        }

        return string.IsNullOrWhiteSpace(language) ? $"file '{requirement.File}'" : $"language '{language}'";
    }

    internal static string Reason(SemanticImplementationRequirement requirement) =>
        $"The Cratis target only realizes C# bodies; {Describe(requirement)} is not supported.";

    internal static ArtifactRenderDiagnostic Warning(SemanticImplementationRequirement requirement, SemanticId owner, string name) =>
        new(
            ArtifactRenderDegradationCodes.InlineMismatch,
            ArtifactRenderDiagnosticSeverity.Warning,
            $"{name} (requirement {requirement.RequirementId}) rendered as a throwing placeholder: {Reason(requirement)} {Remedy}",
            owner);

    internal static string ThrowingBody() => $"throw new global::System.NotSupportedException(\"{ArtifactRenderDegradationCodes.InlineMismatch}\");";
}
