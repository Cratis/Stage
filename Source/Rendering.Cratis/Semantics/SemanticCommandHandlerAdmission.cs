// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class SemanticCommandHandlerAdmission
{
    internal enum HandlerKind
    {
        None,
        Pending,
        CSharp,
        Foreign,
        Invalid
    }

    internal static Classification Classify(SemanticCommand command, ArtifactRenderRequest request)
    {
        if (command.Handler is not { } handler) return new(HandlerKind.None);
        if (request.ImplementationRequirements.IsDefault) return new(HandlerKind.Invalid, Reason: "The implementation requirement envelope is missing.");

        var requirements = request.ImplementationRequirements.Where(requirement => requirement.RequirementId == handler.RequirementId).ToArray();
        if (requirements.Length != 1) return new(HandlerKind.Invalid, Reason: "The handler must reference a unique implementation requirement.");

        return ClassifyRequirement(command, requirements[0]);
    }

    static Classification ClassifyRequirement(SemanticCommand command, SemanticImplementationRequirement requirement)
    {
        if (requirement.Role != SemanticImplementationRole.CommandHandler) return new(HandlerKind.Invalid, requirement, "The requirement role is not CommandHandler.");
        if (requirement.Source?.SemanticId != command.Id) return new(HandlerKind.Invalid, requirement, "The requirement does not belong to this command.");

        return requirement.AttachmentResolution switch
        {
            SemanticAttachmentResolution.Pending => new(HandlerKind.Pending, requirement),
            SemanticAttachmentResolution.Resolved => ClassifyLanguage(requirement),
            _ => new(HandlerKind.Invalid, requirement, "The handler attachment is not resolved.")
        };
    }

    static Classification ClassifyLanguage(SemanticImplementationRequirement requirement)
    {
        var selection = SemanticImplementationBodySelector.Select(requirement);
        if (selection.Outcome == SemanticImplementationBodySelector.SelectionOutcome.UndeterminedLanguage) return new(HandlerKind.Invalid, requirement, "The resolved handler has no implementation language.");
        return new(selection.Outcome == SemanticImplementationBodySelector.SelectionOutcome.Selected ? HandlerKind.CSharp : HandlerKind.Foreign, requirement);
    }

    internal sealed record Classification(HandlerKind Kind, SemanticImplementationRequirement? Requirement = null, string? Reason = null);
}
