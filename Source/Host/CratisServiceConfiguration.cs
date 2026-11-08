// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Neo4j;
using Cratis.Chronicle;
using Cratis.Chronicle.AspNetCore;
using Cratis.Chronicle.Connections;
using Cratis.Json;
using Cratis.OpenTelemetry;
using Cratis.Serialization;
using Cratis.Stage.Host.Published;

namespace Cratis.Stage.Host;

/// <summary>
/// Extension methods for configuring Cratis and telemetry for the Stage host.
/// </summary>
public static class CratisServiceConfiguration
{
    /// <summary>
    /// Adds the Cratis service configuration for the Stage host with camel-case naming and OpenTelemetry.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/> to configure.</param>
    /// <param name="eventStore">The Chronicle event store name for the session.</param>
    /// <param name="programIdentifier">The human-readable program name sent to Chronicle.</param>
    /// <returns>The same <see cref="WebApplicationBuilder"/> for chaining.</returns>
    public static WebApplicationBuilder AddStageCratis(this WebApplicationBuilder builder, string eventStore, string programIdentifier) =>
        builder.AddStageCratis(eventStore, programIdentifier, StageHttpRouteOptions.FromConfiguration(builder.Configuration));

    /// <summary>
    /// Gets whether the configuration names a Neo4j server for graph queries (<c language="text">Cratis:Neo4j:Server</c>, or <c language="text">Cratis__Neo4j__Server</c> in the environment).
    /// </summary>
    /// <param name="configuration">The host configuration.</param>
    /// <returns><see langword="true"/> when a server is configured.</returns>
    internal static bool HasNeo4j(IConfiguration configuration) => !string.IsNullOrWhiteSpace(configuration["Cratis:Neo4j:Server"]);

    internal static WebApplicationBuilder AddStageCratis(this WebApplicationBuilder builder, string eventStore, string programIdentifier, StageHttpRouteOptions routeOptions, PublishedOptions? published = null)
    {
        if (published is not null)
        {
            // The sign-in proxy in front of the host sets the principal headers; Arc reads them only when told to trust them.
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Cratis:Arc:TrustForwardedIdentityHeaders"] = published.UsesTrustedProxy.ToString() });
        }

        builder.AddCratis(
            options =>
            {
                options.GeneratedApis = routeOptions.Canonical;

                // Use the global Cratis JSON configuration which includes DerivedTypeJsonConverterFactory
                // for polymorphic type support in commands and events.
                foreach (var converter in Globals.JsonSerializerOptions.Converters)
                {
                    options.JsonSerializerOptions.Converters.Add(converter);
                }
            },

            // The Stage runs fully in-memory: the Chronicle kernel uses in-memory storage and its projections
            // persist to the kernel's in-memory sink, so no MongoDB-backed Arc read-model store is configured here.
            configureArcBuilder: arc =>
            {
                // Graph queries read through Arc's graph session, so the connection is registered only when the host is told where Neo4j is.
                // Otherwise a model without a graph query, and a sandbox without Neo4j, start exactly as before.
                if (HasNeo4j(builder.Configuration))
                {
                    arc.WithNeo4j(options => options.EventStore ??= eventStore);
                }
            },
            configureChronicleOptions: options =>
            {
                options.EventStore = eventStore;
                options.ProgramIdentifier = programIdentifier;

                // Stage registers modeled artifacts over raw contracts. An empty discovered Client full set
                // would retire those projections on reconnect; Stage owns their registration lifecycle instead.
                options.AutoDiscoverAndRegister = false;

                // No credentials. The kernel this connects to is in the same container, reached over loopback,
                // and started with authentication turned off (see entrypoint-stage.sh) — there is nothing for a
                // token to prove. Acquiring one is not free either: warming the kernel's token endpoint for that
                // first call took 1.9 seconds unconstrained and 3.7 under the CPU limit a play session runs on.
                // Left to itself the client would still exchange the development credentials, because omitting
                // them means client credentials rather than none; auth=none is how you say none.
                //
                // Applied to whatever address is already configured rather than restating it, so pointing a Stage
                // at a different kernel through cratis-stage.json keeps working.
                // A runtime that mounted the connection string of the kernel to use (it carries a secret, so it is a file) wins, as supplied.
                options.ConnectionString = ResolveConnection(options.ConnectionString, SandboxRuntimeConventions.MountedConnectionString(Environment.GetEnvironmentVariable), published);

                // A published kernel on PostgreSQL or SQL Server has no MongoDB to project into; the read models the client registers
                // (Notes') follow the storage engine so they land in the same database. Stage's own modeled read models name their sink.
                if (published is not null)
                {
                    options.DefaultSinkTypeId = published.ReadModelSinkTypeId;
                }

                // Give the Chronicle client's initial handshake more headroom than the 5 second default — the
                // bundled kernel competes for CPU with the host at container startup.
                options.ConnectTimeout = 30;

                // Chronicle's client builds its own JSON converter list without DerivedTypeJsonConverterFactory;
                // seed it here so read models containing polymorphic types round-trip.
                options.JsonSerializerOptions.Converters.Add(new DerivedTypeJsonConverterFactory(DerivedTypes.Instance));
            },
            configureChronicleBuilder: chronicleBuilder =>
            {
                chronicleBuilder.WithCamelCaseNamingPolicy();

                // Nothing discovers on its own here (see AutoDiscoverAndRegister above), but when the published host
                // registers the Notes artifacts it is this provider the client discovers them from. Scoping it to the
                // Notes assemblies keeps the rest of the process out of Chronicle.
                if (published is not null)
                {
                    chronicleBuilder.WithArtifactsProvider(NotesChronicleArtifacts.CreateProvider());
                }
            });

        if (published is null)
        {
            SandboxIdentityOptions.Configure(builder.Services, builder.Configuration);
        }
        else
        {
            // Personas are a play-session device. A published application only ever sees the real principal.
            builder.Services.AddSingleton(new SandboxIdentityOptions(false));
            builder.Services.AddSingleton(new SandboxUsersOptions(false));
            builder.Services.AddSingleton(SandboxUsers.None);
            if (published.UsesOidc)
            {
                PublishedAuthentication.Configure(builder.Services, published);
            }
        }

        builder.Services.AddSingleton<Action<ChronicleTelemetryOptions>>(_ => { });
        StageHttpRouteOptions.AlignIntrospection(builder.Services);

        builder.AddCratisOpenTelemetry();

        return builder;
    }

    static ChronicleConnectionString ResolveConnection(ChronicleConnectionString configured, ChronicleConnectionString? mounted, PublishedOptions? published)
    {
        if (published is not null)
        {
            KernelConnectionGuard.EnsureAllowed(mounted?.ToString() ?? configured.ToString(), published, finalConnection: mounted is not null);
        }
        var connection = mounted ?? ConnectionFor(new ChronicleConnectionStringBuilder(configured.ToString()), published).ToConnectionString();
        if (published is not null)
        {
            KernelConnectionGuard.EnsureAllowed(connection.ToString(), published);
        }
        return connection;
    }

    static ChronicleConnectionStringBuilder ConnectionFor(ChronicleConnectionStringBuilder connection, PublishedOptions? published)
    {
        if (published is null)
        {
            return connection.WithoutAuthentication();
        }

        // A published kernel runs with authentication on. Without configured credentials the client presents the kernel's
        // built-in client, which is only acceptable over loopback, checked by KernelConnectionGuard.
        return published.HasChronicleCredentials
            ? connection.WithCredentials(published.ChronicleClientId!, published.ChronicleClientSecret!)
            : connection.WithDevelopmentCredentials();
    }
}
