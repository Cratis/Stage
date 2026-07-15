// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Commands;
using Cratis.Stage.Contracts;

namespace Cratis.Stage.Api;

/// <summary>
/// Provides command handlers to Arc by convention — one per command defined in the event model.
/// </summary>
public sealed class StageCommandHandlerProvider : ICommandHandlerProvider
{
    readonly List<ICommandHandler> _handlers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="StageCommandHandlerProvider"/> class. Arc discovers this
    /// provider in every host that references the Stage assembly, but only the Stage host registers an event model
    /// to run — so the dependencies are taken as optional collections and the provider exposes no handlers when no
    /// event model is present.
    /// </summary>
    /// <param name="models">The event model the engine runs, when one is registered.</param>
    /// <param name="typeFactories">The factory used to emit a runtime type per command, when one is registered.</param>
    public StageCommandHandlerProvider(IEnumerable<EventModel> models, IEnumerable<DynamicTypeFactory> typeFactories)
    {
        var model = models.FirstOrDefault();
        var typeFactory = typeFactories.FirstOrDefault();
        if (model is null || typeFactory is null)
        {
            return;
        }

        foreach (var located in StageModelWalker.Slices(model))
        {
            if (located.Slice.Command is not { } command)
            {
                continue;
            }

            var commandType = typeFactory.CreateCommandType(located.TypeNamespace, ModelNaming.ToIdentifier(command.Name));
            _handlers.Add(new StageCommandHandler(commandType, located.Location));
        }
    }

    /// <inheritdoc/>
    public IEnumerable<ICommandHandler> Handlers => _handlers;

    /// <inheritdoc/>
    public bool TryGetHandlerFor(object command, [NotNullWhen(true)] out ICommandHandler? handler)
    {
        handler = _handlers.Find(candidate => candidate.CommandType == command.GetType());

        return handler is not null;
    }
}
