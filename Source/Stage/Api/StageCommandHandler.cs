// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;

namespace Cratis.Stage.Api;

/// <summary>
/// An <see cref="ICommandHandler"/> for a modeled command. Foundation behavior accepts the command and echoes
/// its payload; producing schema-faithful events and enforcing constraints is a follow-up.
/// </summary>
/// <param name="commandType">The emitted runtime command type.</param>
/// <param name="location">The route location segments for the command.</param>
public sealed class StageCommandHandler(Type commandType, IReadOnlyList<string> location) : ICommandHandler
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
    public ValueTask<object?> Handle(CommandContext commandContext)
    {
        var payload = commandContext.Command is DynamicCommand command ? command.Data : null;

        return ValueTask.FromResult<object?>(payload);
    }
}
