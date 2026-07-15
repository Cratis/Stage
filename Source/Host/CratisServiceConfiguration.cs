// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle;
using Cratis.Chronicle.AspNetCore;
using Cratis.Json;
using Cratis.Serialization;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

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
    public static WebApplicationBuilder AddStageCratis(this WebApplicationBuilder builder, string eventStore, string programIdentifier)
    {
        builder.AddCratis(
            options =>
            {
                options.GeneratedApis.RoutePrefix = "api";
                options.GeneratedApis.IncludeCommandNameInRoute = false;
                options.GeneratedApis.SegmentsToSkipForRoute = 1;

                // Use the global Cratis JSON configuration which includes DerivedTypeJsonConverterFactory
                // for polymorphic type support in commands and events.
                foreach (var converter in Globals.JsonSerializerOptions.Converters)
                {
                    options.JsonSerializerOptions.Converters.Add(converter);
                }
            },
            configureArcBuilder: arcBuilder =>
                arcBuilder.WithMongoDB(configureMongoDB: mongoDBBuilder => mongoDBBuilder.WithCamelCaseNamingPolicy(pluralizeReadModels: true)),
            configureChronicleOptions: options =>
            {
                options.EventStore = eventStore;
                options.ProgramIdentifier = programIdentifier;

                // Give the Chronicle client's initial handshake more headroom than the 5 second default — the
                // bundled kernel competes for CPU with the host at container startup.
                options.ConnectTimeout = 30;

                // Chronicle's client builds its own JSON converter list without DerivedTypeJsonConverterFactory;
                // seed it here so read models containing polymorphic types round-trip.
                options.JsonSerializerOptions.Converters.Add(new DerivedTypeJsonConverterFactory(DerivedTypes.Instance));
            },
            configureChronicleBuilder: chronicleBuilder => chronicleBuilder.WithCamelCaseNamingPolicy());

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation())
            .WithTracing(tracing =>
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }
}
