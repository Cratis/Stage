// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.given;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using RequestContext = Cratis.Stage.Extensions.Protocol.RequestContext;

namespace Cratis.Stage.Host.Extensions.for_StageHostService;

public class when_reporting_telemetry : an_extension_protocol
{
    readonly List<(string Name, double Value, Dictionary<string, object?> Tags)> _measurements = [];
    MeterListener _listener = null!;
    (string Category, Microsoft.Extensions.Logging.LogLevel Level, string Message, Dictionary<string, object?> Scope) _logged;

    async Task Establish()
    {
        await StartBackend();
        await StartStage();
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == ExtensionTelemetry.MeterName) listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            lock (_measurements) _measurements.Add((instrument.Name, measurement, tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value)));
        });
        _listener.Start();
    }

    async Task Because()
    {
        var host = await Connected();
        var context = new RequestContext { Tenant = "Default", InvocationId = "invocation-1", Attempt = 2, OriginAddress = "Fulfillment/Post/branch 1", Causation = "event-42" };
        var log = new LogRequest { Context = context };
        var record = new LogRecord { Level = Cratis.Stage.Extensions.Protocol.LogLevel.Warning, Message = "rate limit close", Occurred = Timestamp.FromDateTime(DateTime.UtcNow) };
        record.Attributes.Add(new KeyValue { Key = "remaining", Value = "3" });
        log.Records.Add(record);
        await host.LogAsync(log);

        var metrics = new MetricsRequest { Context = context };
        var counter = new Metric { Name = "messages.posted", Kind = Metric.Types.Kind.Counter, Value = 2 };
        counter.Attributes.Add(new KeyValue { Key = "channel", Value = "#orders" });
        metrics.Metrics.Add(counter);
        metrics.Metrics.Add(new Metric { Name = "post.duration", Kind = Metric.Types.Kind.Histogram, Value = 0.25, Unit = "s" });
        metrics.Metrics.Add(new Metric { Name = "ignored", Kind = Metric.Types.Kind.Unspecified, Value = 1 });
        await host.RecordMetricsAsync(metrics);

        _logged = _logs.Records.Single(entry => entry.Category == "Cratis.Stage.Extensions.Slack");
    }

    [Fact] void should_log_the_message_at_its_level() => (_logged.Message == "rate limit close" && _logged.Level == Microsoft.Extensions.Logging.LogLevel.Warning).ShouldBeTrue();
    [Fact] void should_tie_the_log_to_the_invocation() => (_logged.Scope["invocation"]!.ToString() == "invocation-1" && _logged.Scope["attempt"]!.ToString() == "2").ShouldBeTrue();
    [Fact] void should_tie_the_log_to_its_causation() => (_logged.Scope["causation"]!.ToString() == "event-42" && _logged.Scope["origin"]!.ToString() == "Fulfillment/Post/branch 1").ShouldBeTrue();
    [Fact] void should_name_the_system_from_the_credential() => _logged.Scope["system"]!.ToString().ShouldEqual("Slack");
    [Fact] void should_keep_the_attributes() => _logged.Scope["attr.remaining"]!.ToString().ShouldEqual("3");
    [Fact] void should_record_a_counter_with_its_attributes() => _measurements.Single(measurement => measurement.Name == "stage.extension.messages.posted").Tags["channel"]!.ToString().ShouldEqual("#orders");
    [Fact] void should_tag_a_metric_with_the_system_and_tenant() => _measurements.TrueForAll(measurement => measurement.Tags["stage.extension.system"]!.ToString() == "Slack" && measurement.Tags["stage.tenant"]!.ToString() == "Default").ShouldBeTrue();
    [Fact] void should_record_a_histogram() => _measurements.Single(measurement => measurement.Name == "stage.extension.post.duration").Value.ShouldEqual(0.25);
    [Fact] void should_ignore_a_metric_of_no_kind() => _measurements.Exists(measurement => measurement.Name == "stage.extension.ignored").ShouldBeFalse();
}
