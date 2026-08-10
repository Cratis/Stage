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
        var values = payload.AsReadOnly();
        var events = ProducedEventPayloads.Build(definition.Produces, values, DateTimeOffset.UtcNow, caller);

        await appender.Append(EventSourceId(values), events, caller);
    }

    // The model names the property carrying the event source id, so successive commands for the same entity land on
    // the same stream. A command that declares no identifier - or whose payload carries no value for it - has no
    // entity to continue, so it opens a stream of its own rather than appending to an empty one every other such
    // command would share.
    string EventSourceId(IReadOnlyDictionary<string, JsonElement> payload)
    {
        if (definition.Identifier is not { Length: > 0 } identifier)
        {
            return Guid.NewGuid().ToString();
        }

        var value = CommandPayloadValues.Text(CommandPayloadValues.Lookup(payload, identifier));

        return value.Length > 0 ? value : Guid.NewGuid().ToString();
    }
}
