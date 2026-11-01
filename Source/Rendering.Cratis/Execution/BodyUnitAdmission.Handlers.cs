// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Execution;

internal static partial class BodyUnitAdmission
{
    /// <summary>Admits one command handler with exactly the diagnostics the renderer's own admission reports.</summary>
    /// <param name="context">The loaded application.</param>
    /// <param name="requirement">The handler's implementation requirement.</param>
    /// <returns>The command that owns the handler, or the refused manifest entry.</returns>
    internal static (SemanticCommand? Command, BodyUnitEntry? Refusal) AdmitHandler(SemanticApplicationContext context, SemanticImplementationRequirement requirement)
    {
        var commands = context.Commands.Values.Where(command => command.Handler?.RequirementId == requirement.RequirementId).ToArray();
        if (commands.Length != 1) return (null, Refuse(requirement, "STAGE-ESM-021", "The command handler is not uniquely bound to a command."));
        var command = commands[0];
        var kind = SemanticCommandHandlerAdmission.Classify(command, context.Request).Kind;
        if (kind is SemanticCommandHandlerAdmission.HandlerKind.Pending or SemanticCommandHandlerAdmission.HandlerKind.Foreign)
        {
            var feature = SemanticVersionFeatures.DescribedHandlersInCommand(command).First();
            var diagnostic = SemanticVersionFeatureAdmission.HandlerDiagnostic(feature, context);
            return (null, Refuse(requirement, diagnostic.Code, diagnostic.Message));
        }
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        SemanticCommandBodyAdmission.Verify(command, context, diagnostics);
        return diagnostics.FirstOrDefault(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error) is { } refusal
            ? (null, Refuse(requirement, refusal.Code, refusal.Message))
            : (command, null);
    }
}
