// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Stage.Api;
using Microsoft.AspNetCore.Http;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Performs the semantic dry run before Arc handles or validates a semantic command.
/// </summary>
/// <param name="runtimes">Optional semantic runtimes.</param>
/// <param name="contexts">Optional HTTP accessors.</param>
public sealed class SemanticRuntimeCommandFilter(IEnumerable<ISemanticRuntime> runtimes, IEnumerable<IHttpContextAccessor> contexts) : ICommandFilter
{
    /// <inheritdoc/>
    public async Task<CommandResult> OnExecution(CommandContext context)
    {
        var runtime = runtimes.FirstOrDefault();
        var accessor = contexts.FirstOrDefault();
        if (runtime is null || accessor is null || context.Command is not DynamicCommand)
        {
            return CommandResult.Success(context.CorrelationId);
        }

        var command = SemanticModelWalker.Slices(runtime.Plan.Model)
            .SelectMany(located => located.Slice.Commands.Select(candidate => (TypeName: $"{located.TypeNamespace}.{ModelNaming.ToIdentifier(candidate.Name)}", Command: candidate)))
            .FirstOrDefault(candidate => candidate.TypeName == context.Type.FullName).Command;
        if (command is null)
        {
            return CommandResult.Success(context.CorrelationId);
        }

        var principal = SemanticCommandAccess.Principal(accessor);
        var occurrence = SemanticCommandAccess.Occurrence(context, principal);
        var outcome = await runtime.Execute(command, SemanticCommandAccess.Payload(context), principal, occurrence, true);
        return SemanticCommandOutcome.Map(outcome, context.CorrelationId, accessor.HttpContext, command.Id.ToString());
    }
}
