// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;

namespace Cratis.Stage.Host.Published;

/// <summary>
/// Keeps the commands and queries of a referenced library that the host does not enable out of Arc.
/// </summary>
/// <remarks>
/// Arc discovers commands and queries from every assembly the host references, so a library that is only configured by an
/// <c language="shell">Add...</c> call (Notes) still has its routes mapped when the call is never made, and they then run unconfigured. Arc
/// has no option to leave an assembly out, but it resolves its handlers and performers through two replaceable catalogs.
/// These wrap them, so the routes are neither mapped nor executable, and the introspection catalogs do not list them.
/// </remarks>
internal static class ExcludingProviders
{
    /// <summary>
    /// Leaves everything the given assemblies define out of Arc's command and query catalogs.
    /// </summary>
    /// <param name="services">The service collection, after Arc was added.</param>
    /// <param name="assemblies">The assemblies to leave out.</param>
    /// <returns>The same service collection.</returns>
    internal static IServiceCollection ExcludeFromArc(this IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        var excluded = assemblies.ToHashSet();
        services.AddSingleton<ICommandHandlerProviders>(provider => new ExcludingCommandHandlerProviders(
            ActivatorUtilities.CreateInstance<CommandHandlerProviders>(provider), excluded));
        services.AddSingleton<IQueryPerformerProviders>(provider => new ExcludingQueryPerformerProviders(
            ActivatorUtilities.CreateInstance<QueryPerformerProviders>(provider), excluded));
        return services;
    }

    sealed class ExcludingCommandHandlerProviders(ICommandHandlerProviders inner, HashSet<Assembly> excluded) : ICommandHandlerProviders
    {
        public IEnumerable<ICommandHandler> Handlers => [.. inner.Handlers.Where(handler => !excluded.Contains(handler.CommandType.Assembly))];

        public bool TryGetHandlerFor(object command, [NotNullWhen(true)] out ICommandHandler? handler)
        {
            if (inner.TryGetHandlerFor(command, out handler) && !excluded.Contains(handler.CommandType.Assembly))
            {
                return true;
            }

            handler = null;
            return false;
        }
    }

    sealed class ExcludingQueryPerformerProviders(IQueryPerformerProviders inner, HashSet<Assembly> excluded) : IQueryPerformerProviders
    {
        public IEnumerable<IQueryPerformer> Performers => [.. inner.Performers.Where(performer => !excluded.Contains(performer.Type.Assembly))];

        public bool TryGetPerformersFor(FullyQualifiedQueryName query, [NotNullWhen(true)] out IQueryPerformer? performer)
        {
            if (inner.TryGetPerformersFor(query, out performer) && !excluded.Contains(performer.Type.Assembly))
            {
                return true;
            }

            performer = null;
            return false;
        }
    }
}
