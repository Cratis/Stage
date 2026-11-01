// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Selects the authored body rendered by the Cratis target.</summary>
/// <remarks>
/// This is the only language selection rule. Screenplay#1086 alternatives plug in here.
/// The handler file rule also accepts explicit csharp on a .cs file for every role; the compiler
/// currently supplies that combination only for handlers.
/// </remarks>
internal static class SemanticImplementationBodySelector
{
    internal const string TargetLanguage = CSharpInlineLanguagePack.Language;
    internal const string LanguageCode = "STAGE-IMPL-LANGUAGE";

    internal enum SelectionOutcome
    {
        Selected,
        NotInTargetLanguage,
        UndeterminedLanguage
    }

    internal static Selection Select(SemanticImplementationRequirement requirement)
    {
        var language = EffectiveLanguage(requirement);
        if (string.IsNullOrWhiteSpace(language)) return new(SelectionOutcome.UndeterminedLanguage, requirement, null, requirement.RequirementId, []);
        var selected = language == TargetLanguage && (requirement.File?.EndsWith(".cs", StringComparison.Ordinal) ?? true);
        return new(selected ? SelectionOutcome.Selected : SelectionOutcome.NotInTargetLanguage, requirement, selected ? requirement : null, requirement.RequirementId, [language]);
    }

    internal static bool IsTarget(SemanticImplementationRequirement requirement) => Select(requirement).Outcome == SelectionOutcome.Selected;

    internal static string? EffectiveLanguage(SemanticImplementationRequirement requirement) =>
        requirement.Language ?? (requirement.File?.EndsWith(".cs", StringComparison.Ordinal) == true ? TargetLanguage : null);

    internal static ArtifactRenderDiagnostic LanguageRefusal(Selection selection, string member, SemanticId artifact) =>
        new(LanguageCode, ArtifactRenderDiagnosticSeverity.Error, $"{member} (requirement {selection.Requirement.RequirementId}) has implementation bodies in {string.Join(", ", selection.AuthoredLanguages)} but none in {TargetLanguage}. Add a {TargetLanguage} body to render it with the Cratis target.", artifact);

    internal static ArtifactRenderDiagnostic? LanguageDiagnostic(SemanticImplementationRequirement requirement, SemanticId owner, string operation) =>
        requirement.AttachmentResolution == SemanticAttachmentResolution.Resolved && Select(requirement) is { Outcome: SelectionOutcome.NotInTargetLanguage } selection ?
            LanguageRefusal(selection, operation, owner) : null;

    internal sealed record Selection(SelectionOutcome Outcome, SemanticImplementationRequirement Requirement, SemanticImplementationRequirement? Selected, string ContentKey, ImmutableArray<string> AuthoredLanguages);
}
