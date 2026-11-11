// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Cratis.Stage.Extensions.Protocol;
using RequestContext = Cratis.Stage.Extensions.Protocol.RequestContext;

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Turns what an extension reports into the host's own logs and metrics, tied to the causation of the invocation it belongs to. The system comes from the
/// credential, never from what the extension says.
/// </summary>
/// <param name="loggers">The logger factory.</param>
public sealed class ExtensionTelemetry(ILoggerFactory loggers) : IDisposable
{
    /// <summary>
    /// Gets the name of the meter extension metrics are recorded on.
    /// </summary>
    public const string MeterName = "Cratis.Stage.Extensions";

    /// <summary>
    /// Gets how many distinct metrics one system may record. A system that goes past it is not recorded further, so a bug cannot flood the host.
    /// </summary>
    public const int MaxMetricsPerSystem = 64;

    readonly Meter _meter = new(MeterName);
    readonly ConcurrentDictionary<(string System, string Name, Metric.Types.Kind Kind), object> _instruments = new();
    readonly ConcurrentDictionary<string, int> _counts = new(StringComparer.Ordinal);

    /// <summary>
    /// Writes the log records of an extension.
    /// </summary>
    /// <param name="system">The name of the system, from the credential.</param>
    /// <param name="context">The <see cref="RequestContext"/> the records belong to.</param>
    /// <param name="records">The <see cref="LogRecord"/>s.</param>
    public void Log(string system, RequestContext context, IEnumerable<LogRecord> records)
    {
        var logger = loggers.CreateLogger($"Cratis.Stage.Extensions.{system}");
        foreach (var record in records)
        {
            var state = new Dictionary<string, object?>
            {
                ["system"] = system,
                ["tenant"] = context.Tenant,
                ["invocation"] = context.InvocationId,
                ["attempt"] = context.Attempt,
                ["origin"] = context.OriginAddress,
                ["causation"] = context.Causation
            };
            foreach (var attribute in record.Attributes) state[$"attr.{attribute.Key}"] = attribute.Value;
            using var scope = logger.BeginScope(state);
            var level = Level(record.Level);
            if (logger.IsEnabled(level)) ExtensionLog.Reported(logger, level, record.Message);
        }
    }

    /// <summary>
    /// Records the metrics of an extension.
    /// </summary>
    /// <param name="system">The name of the system, from the credential.</param>
    /// <param name="context">The <see cref="RequestContext"/> the metrics belong to.</param>
    /// <param name="metrics">The <see cref="Metric"/>s.</param>
    /// <returns>How many were recorded.</returns>
    public int Record(string system, RequestContext context, IEnumerable<Metric> metrics)
    {
        var recorded = 0;
        foreach (var metric in metrics)
        {
            if (metric.Kind is not (Metric.Types.Kind.Counter or Metric.Types.Kind.Histogram) || metric.Name.Length == 0) continue;
            var key = (system, metric.Name, metric.Kind);
            if (!_instruments.TryGetValue(key, out var instrument))
            {
                if (_counts.AddOrUpdate(system, 1, (_, count) => count + 1) > MaxMetricsPerSystem) continue;
                instrument = _instruments.GetOrAdd(
                    key,
                    static (created, arguments) => created.Kind == Metric.Types.Kind.Counter
                        ? arguments.Meter.CreateCounter<double>($"stage.extension.{created.Name}", arguments.Unit)
                        : arguments.Meter.CreateHistogram<double>($"stage.extension.{created.Name}", arguments.Unit),
                    (Meter: _meter, Unit: metric.Unit.Length == 0 ? null : metric.Unit));
            }

            var tags = new List<KeyValuePair<string, object?>>
            {
                new("stage.extension.system", system),
                new("stage.tenant", context.Tenant)
            };
            tags.AddRange(metric.Attributes.Select(attribute => new KeyValuePair<string, object?>(attribute.Key, attribute.Value)));
            if (instrument is Counter<double> counter) counter.Add(metric.Value, [.. tags]);
            else if (instrument is Histogram<double> histogram) histogram.Record(metric.Value, [.. tags]);
            recorded++;
        }

        return recorded;
    }

    /// <inheritdoc/>
    public void Dispose() => _meter.Dispose();

    static Microsoft.Extensions.Logging.LogLevel Level(Cratis.Stage.Extensions.Protocol.LogLevel level) => level switch
    {
        Cratis.Stage.Extensions.Protocol.LogLevel.Trace => Microsoft.Extensions.Logging.LogLevel.Trace,
        Cratis.Stage.Extensions.Protocol.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
        Cratis.Stage.Extensions.Protocol.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
        Cratis.Stage.Extensions.Protocol.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
        Cratis.Stage.Extensions.Protocol.LogLevel.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
        _ => Microsoft.Extensions.Logging.LogLevel.Information
    };
}
