// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Commands;
using Cratis.Stage.Api;
using Microsoft.AspNetCore.Http;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Discovers semantic command handlers only when the host explicitly registers a semantic runtime.
/// </summary>
public sealed class SemanticRuntimeCommandHandlerProvider : ICommandHandlerProvider
{
    readonly List<ICommandHandler> _handlers = [];

    /// <summary>
    /// Creates handlers for each admitted command.
    /// </summary>
    /// <param name="runtimes">The optional semantic runtime.</param>
    /// <param name="factories">The optional runtime type factory.</param>
    /// <param name="contexts">The optional HTTP accessor.</param>
    public SemanticRuntimeCommandHandlerProvider(
        IEnumerable<ISemanticRuntime> runtimes,
        IEnumerable<DynamicTypeFactory> factories,
        IEnumerable<IHttpContextAccessor> contexts)
    {
        var runtime = runtimes.FirstOrDefault();
        var factory = factories.FirstOrDefault();
        var context = contexts.FirstOrDefault();
        if (runtime is null || factory is null || context is null)
        {
            return;
        }

        foreach (var located in SemanticModelWalker.Slices(runtime.Plan.Model))
        {
            foreach (var command in located.Slice.Commands)
            {
                var type = factory.CreateCommandType(located.TypeNamespace, ModelNaming.ToIdentifier(command.Name));
                _handlers.Add(new SemanticRuntimeCommandHandler(type, located.CanonicalLocation, command, runtime, context));
            }
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
