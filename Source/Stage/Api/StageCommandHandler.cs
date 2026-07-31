// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Runtime;

namespace Cratis.Stage.Api;

/// <summary>
/// An <see cref="ICommandHandler"/> for a modeled command. Evaluates the command's <c>produces</c> declarations
/// against the incoming payload and appends the resulting events, then echoes the payload back as the response.
/// </summary>
/// <param name="commandType">The emitted runtime command type.</param>
/// <param name="location">The route location segments for the command.</param>
/// <param name="definition">The modeled command being handled.</param>
/// <param name="appender">The system appending the produced events.</param>
/// <param name="identity">The system resolving the identity behind the command.</param>
public sealed class StageCommandHandler(
    Type commandType,
    IReadOnlyList<string> location,
    CommandDefinition definition,
    IAppendProducedEvents appender,
    IProvideStageIdentity identity) : ICommandHandler
{
    /// <inheritdoc/>
    public IEnumerable<string> Location => location;

    /// <inheritdoc/>
    public Type CommandType => commandType;

    /// <inheritdoc/>
    public IEnumerable<Type> Dependencies => [];

    /// <inheritdoc/>
    public IEnumerable<ParameterInfo> Parameters => [];

    /// <inheritdoc/>
    public bool AllowsAnonymousAccess => true;

    /// <inheritdoc/>
    public async ValueTask<object?> Handle(CommandContext commandContext)
    {
        if (commandContext.Command is not DynamicCommand command)
        {
            return null;
        }

        await AppendProducedEvents(command.Data);

        return command.Data;
    }

    async Task AppendProducedEvents(IDictionary<string, JsonElement> payload)
    {
        if (definition.Produces.Count == 0)
        {
            return;
        }

        var caller = identity.Current();
        var events = ProducedEventPayloads.Build(definition.Produces, payload.AsReadOnly(), DateTimeOffset.UtcNow, caller);

        // Screenplay does not declare which command value identifies the event source, so every command execution
        // opens its own stream. Modeled correlation across commands is a follow-up that needs the language to say
        // which property is the identity.
        await appender.Append(Guid.NewGuid().ToString(), events, caller);
    }
}
