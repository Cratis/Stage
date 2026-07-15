// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.EventSequences;
using Cratis.Stage.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ChronicleProjections = Cratis.Chronicle.Contracts.Projections;
using ChronicleReadModels = Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Stage.Runtime;

/// <summary>
/// Registers the read models and projections modeled in an <see cref="EventModel"/> with a running Chronicle at
/// startup, purely from the runtime model data — no compiled read-model type or attributes. Once registered, Chronicle
/// builds and maintains the read-model documents as events are appended.
/// </summary>
public static class StageRuntimeRegistrar
{
    /// <summary>
    /// Registers the model's read models and their projections with the given event store.
    /// </summary>
    /// <param name="services">The application services used to resolve the Chronicle client.</param>
    /// <param name="eventStoreName">The name of the event store the engine runs against.</param>
    /// <param name="model">The event model being run.</param>
    /// <param name="logger">The logger used to report the outcome.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RegisterAsync(IServiceProvider services, string eventStoreName, EventModel model, ILogger logger)
    {
        try
        {
            var client = services.GetRequiredService<IChronicleClient>();
            var eventStore = await client.GetEventStore(eventStoreName);

            await eventStore.Connection.Connect();

            var accessor = (IChronicleServicesAccessor)eventStore.Connection;
            await accessor.Services.EventStores.Ensure(new EnsureEventStore { Name = eventStore.Name });

            string logSequence = EventSequenceId.Log;
            var (readModels, projections) = StageChronicleDefinitions.Build(model, logSequence);

            if (readModels.Count == 0)
            {
                StageRuntimeRegistrarLogging.NoReadModels(logger, model.Name);
                return;
            }

            await accessor.Services.ReadModels.RegisterMany(new ChronicleReadModels.RegisterManyRequest
            {
                EventStore = eventStore.Name,
                Owner = ChronicleReadModels.ReadModelOwner.Client,
                ReadModels = readModels,
                Source = ChronicleReadModels.ReadModelSource.User,
            });

            await accessor.Services.Projections.Register(new ChronicleProjections.RegisterRequest
            {
                EventStore = eventStore.Name,
                Owner = ChronicleProjections.ProjectionOwner.Client,
                Projections = projections,
            });

            StageRuntimeRegistrarLogging.Registered(logger, readModels.Count, eventStoreName);
        }
        catch (Exception exception)
        {
            StageRuntimeRegistrarLogging.RegistrationFailed(logger, exception);
        }
    }
}
