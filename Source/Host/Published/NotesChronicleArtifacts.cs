// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors;
using Cratis.Showrunner.Notes;
using Cratis.Showrunner.Notes.Contracts.Responses;
using Cratis.Showrunner.Notes.Publishing;
using Cratis.Types;

namespace Cratis.Stage.Host.Published;

/// <summary>
/// The Chronicle artifacts of the Notes backend, and the one way they are registered in a published host.
/// </summary>
/// <remarks>
/// <para>
/// Stage turns the Chronicle client's <c language="shell">AutoDiscoverAndRegister</c> off: it registers the modeled artifacts itself over
/// raw contracts, and the client's registration is a full set (a projection registration that claims to be everything
/// the client declares, which the kernel answers by retiring every client-owned projection it does not list). Switching
/// discovery on for Notes would therefore register an empty full set next to Stage's own projections and retire them.
/// Explicit artifacts do not help either: they only take effect for stores that discover.
/// </para>
/// <para>
/// So Notes is registered store by store, with the client's own building blocks and without ever running the full
/// registration: the event types, the read models (a batch of just the Notes read models, which carries no full-set
/// claim), the reducers and the reactors. Projections are never registered here. The client's discovery is scoped to the
/// two Notes assemblies, so nothing of Stage's own is picked up, and an artifact kind Notes starts using that this
/// registration does not know how to register safely is refused loudly instead of being left out.
/// </para>
/// </remarks>
internal static class NotesChronicleArtifacts
{
    static readonly Assembly[] _assemblies = [typeof(NotesOptions).Assembly, typeof(NoteAcknowledged).Assembly];

    static readonly (Type Type, Func<IEventStore, Task> Register)[] _reactors =
    [
        (typeof(OutboxPublisher), store => store.Reactors.Register<OutboxPublisher>())
    ];

    /// <summary>
    /// Gets the assemblies the Notes artifacts live in: the backend and its public contracts.
    /// </summary>
    internal static IReadOnlyList<Assembly> Assemblies => _assemblies;

    /// <summary>
    /// Creates the artifacts provider that discovers the Notes assemblies and nothing else.
    /// </summary>
    /// <returns>The provider.</returns>
    internal static IClientArtifactsProvider CreateProvider() => new DefaultClientArtifactsProvider(new NotesAssemblies());

    /// <summary>
    /// Gets the artifact kinds this registration does not register that the provider nevertheless finds in the Notes assemblies.
    /// </summary>
    /// <param name="artifacts">The provider to look at.</param>
    /// <returns>A description of each unsupported artifact, empty when everything found can be registered.</returns>
    internal static IReadOnlyList<string> Unsupported(IClientArtifactsProvider artifacts)
    {
        var unsupported = new List<string>();
        void Add(string kind, IEnumerable<Type> types) => unsupported.AddRange(types.Select(type => $"{kind} {type.FullName}"));

        Add("projection", artifacts.Projections);
        Add("model-bound projection", artifacts.ModelBoundProjections);
        Add("constraint", artifacts.ConstraintTypes);
        Add("unique constraint", artifacts.UniqueConstraints.Concat(artifacts.UniqueEventTypeConstraints));
        Add("remove-constraint event", artifacts.RemoveConstraintEventTypes);
        Add("closes-stream event", artifacts.ClosesStreamEventTypes);
        Add("event source", artifacts.EventSources);
        Add("event seeder", artifacts.EventSeeders);
        Add("event type migrator", artifacts.EventTypeMigrators);
        Add("read model reactor", artifacts.ReadModelReactors);
        Add("occurrence handler", artifacts.OccurrenceHandlers);
        Add("trigger", artifacts.Triggers);
        Add("reactor", artifacts.Reactors.Where(reactor => _reactors.All(known => known.Type != reactor)));
        return unsupported;
    }

    /// <summary>
    /// Registers the Notes artifacts in an event store, and again each time the store reconnects.
    /// </summary>
    /// <param name="store">The event store of one namespace.</param>
    /// <param name="artifacts">The provider the client discovers the Notes artifacts from.</param>
    /// <returns>An awaitable task.</returns>
    /// <exception cref="InvalidOperationException">The Notes assemblies hold an artifact this registration cannot register safely.</exception>
    internal static async Task Register(IEventStore store, IClientArtifactsProvider artifacts)
    {
        if (Unsupported(artifacts) is { Count: > 0 } unsupported)
        {
            throw new InvalidOperationException($"The Notes backend declares Chronicle artifacts Stage cannot register without a full registration: {string.Join(", ", unsupported)}.");
        }

        // Local only. The event types come first because the reducers and read models refer to them.
        await store.EventTypes.Discover();
        await store.Reducers.Discover();

        await RegisterDefinitions(store);
        store.Connection.Lifecycle.OnConnected += () => RegisterDefinitions(store);

        // Observers register once; the handlers pick the kernel up again on their own after a reconnect.
        await store.Reducers.Register();
        foreach (var reactor in _reactors)
        {
            await reactor.Register(store);
        }
    }

    static async Task RegisterDefinitions(IEventStore store)
    {
        await store.EventTypes.Register();
        await store.ReadModels.Register();
    }

    sealed class NotesAssemblies : ICanProvideAssembliesForDiscovery
    {
        Type[] _types = [];

        public IEnumerable<Assembly> Assemblies => _assemblies;

        public IEnumerable<Type> DefinedTypes => _types;

        public void Initialize() => _types = [.. _assemblies.SelectMany(assembly => assembly.GetTypes())];
    }
}
