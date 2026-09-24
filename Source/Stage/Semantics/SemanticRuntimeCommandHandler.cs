// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.DependencyInjection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Api;
using Cratis.Stage.Runtime;
using Microsoft.AspNetCore.Http;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Handles one semantic command after the Arc filter's non-mutating check.
/// </summary>
[IgnoreConvention]
public sealed class SemanticRuntimeCommandHandler : ICommandHandler
{
    readonly SemanticCommand _command;
    readonly ISemanticRuntime _runtime;
    readonly IHttpContextAccessor _context;

    internal SemanticRuntimeCommandHandler(Type commandType, IReadOnlyList<string> location, SemanticCommand command, ISemanticRuntime runtime, IHttpContextAccessor context)
    {
        CommandType = commandType;
        Location = location;
        _command = command;
        _runtime = runtime;
        _context = context;
    }

    /// <inheritdoc/>
    public IEnumerable<string> Location { get; }

    /// <inheritdoc/>
    public Type CommandType { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Dependencies => [];

    /// <inheritdoc/>
    public IEnumerable<ParameterInfo> Parameters => [];

    /// <inheritdoc/>
    public bool AllowsAnonymousAccess => true;

    /// <inheritdoc/>
    public async ValueTask<object?> Handle(CommandContext commandContext)
    {
        var principal = SemanticCommandAccess.Principal(_context);
        var outcome = await _runtime.Execute(
            _command,
            SemanticCommandAccess.Payload(commandContext),
            principal,
            SemanticCommandAccess.Occurrence(commandContext, principal),
            false);
        if (outcome is SemanticAccepted && commandContext.Command is DynamicCommand dynamic)
        {
            return dynamic.Data;
        }

        if (outcome is SemanticRejected rejected)
        {
            throw new ProducedEventConstraintRejected(ValidationResult.Error(
                rejected.Details,
                reason: rejected.Category == SemanticRejectionCategory.Constraint ? ValidationResultReason.ConstraintViolation : ValidationResultReason.Rule,
                reasonDetail: rejected.Code));
        }

        var result = SemanticCommandOutcome.Map(outcome, commandContext.CorrelationId, _context.HttpContext, _command.Id.ToString());
        throw new SemanticCommandExecutionFailed(string.Join("; ", result.ExceptionMessages));
    }
}

public sealed class SemanticCommandExecutionFailed(string message) : Exception(message);
