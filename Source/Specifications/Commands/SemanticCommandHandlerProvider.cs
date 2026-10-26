// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cratis.Arc.Commands;

namespace Cratis.Stage.Specifications.Commands;

/// <summary>
/// Supplies an Arc handler only inside a semantic specification scenario.
/// </summary>
/// <param name="contexts">The current run context, if one was registered.</param>
internal sealed class SemanticCommandHandlerProvider(IEnumerable<SemanticRunContext> contexts) : ICommandHandlerProvider
{
    readonly ICommandHandler[] _handlers = [.. contexts.Select(context => (ICommandHandler)new SemanticHandler(context))];

    /// <inheritdoc/>
    public IEnumerable<ICommandHandler> Handlers => _handlers;

    /// <inheritdoc/>
    public bool TryGetHandlerFor(object command, [NotNullWhen(true)] out ICommandHandler? handler)
    {
        handler = _handlers.FirstOrDefault(candidate => candidate.CommandType == command.GetType());
        return handler is not null;
    }

    sealed class SemanticHandler(SemanticRunContext context) : ICommandHandler
    {
        public IEnumerable<string> Location => [];
        public Type CommandType => context.CommandType;
        public IEnumerable<Type> Dependencies => [];
        public IEnumerable<ParameterInfo> Parameters => [];
        public bool AllowsAnonymousAccess => true;

        public async ValueTask<object?> Handle(CommandContext commandContext)
        {
            commandContext.CancellationToken.ThrowIfCancellationRequested();
            if (context.Command.Handler is not null)
            {
                context.HandlerBodyReached = true;
                return null;
            }

            // A command another slice owns is that slice's behavior: in an isolated run it is decided, but its facts are not kept.
            var adopted = !context.IsInvocation || context.Plan.Runs(context.Command.Id);
            foreach (var produced in context.Command.Produces.Where(context.Holds))
            {
                var (fact, destination) = context.Produce(produced);
                if (adopted) await context.Append(fact, destination, commandContext.CancellationToken);
            }

            context.CompleteResponse();
            return context.Response;
        }
    }
}
