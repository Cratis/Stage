// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Screenplay.Semantics.Execution;
using Microsoft.AspNetCore.Http;

namespace Cratis.Stage.Semantics;

internal static class SemanticCommandOutcome
{
    internal static CommandResult Map(SemanticExecutionResult result, CorrelationId correlationId, HttpContext? httpContext, string artifact) => result switch
    {
        SemanticAccepted => CommandResult.Success(correlationId),
        SemanticRejected { Category: SemanticRejectionCategory.Unauthorized } rejected => CommandResult.Unauthorized(correlationId, rejected.Details),
        SemanticRejected { Category: SemanticRejectionCategory.Constraint } rejected => new CommandResult
        {
            ValidationResults = [ValidationResult.Error(rejected.Details, reason: ValidationResultReason.ConstraintViolation, reasonDetail: rejected.Code)]
        },
        SemanticRejected rejected => new CommandResult
        {
            ValidationResults = rejected.ValidationFailures.IsEmpty
                ? [ValidationResult.Error(rejected.Details)]
                : rejected.ValidationFailures.Select(failure => ValidationResult.Error(failure.Message)).ToArray()
        },
        SemanticUnsupported unsupported => Unsupported(unsupported, httpContext, artifact),
        _ => new CommandResult { ExceptionMessages = ["Semantic execution returned an unrecognized outcome."] }
    };

    static CommandResult Unsupported(SemanticUnsupported unsupported, HttpContext? context, string artifact)
    {
        var capability = unsupported.Capability == SemanticExecutionCapability.Unknown ? "World" : unsupported.Capability.ToString();
        if (context is not null)
        {
            context.Items[SemanticRuntimeMarkers.Unsupported] = true;
            context.Items[SemanticRuntimeMarkers.UnsupportedMessage] = $"Unsupported({capability}) {artifact}: {unsupported.Details}";
            context.Response.Headers["Stage-Unsupported-Capability"] = capability;
            context.Response.Headers["Stage-Unsupported-Artifact"] = artifact;
        }

        return new CommandResult { ExceptionMessages = [$"Unsupported({capability}) {artifact}: {unsupported.Details}"] };
    }
}
