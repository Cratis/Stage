// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Extensions.Protocol;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using RequestContext = Cratis.Stage.Extensions.Protocol.RequestContext;

namespace Cratis.Stage.Extensions;

/// <summary>
/// What Stage offers an extension: settings and secrets, triggers, answering later, and logs and metrics tied to the invocation they are used from.
/// </summary>
/// <param name="client">The client of Stage's host services, or null before the handshake.</param>
/// <param name="credential">The credential metadata every call presents.</param>
/// <param name="context">The context of the request the calls are tied to.</param>
public sealed class HostServices(StageHost.StageHostClient? client, Metadata credential, RequestContext context)
{
    StageHost.StageHostClient Client => client ?? throw new HostServiceException("Stage has not completed the handshake with this extension, so the host services are not reachable yet.");

    /// <summary>
    /// Reads a public setting the extension declared.
    /// </summary>
    /// <param name="name">The name of the setting.</param>
    /// <param name="cancellationToken">A token that cancels the call.</param>
    /// <returns>The value, or null when it is not set.</returns>
    /// <exception cref="HostServiceException">The name is not a public setting the extension declared.</exception>
    public async Task<string?> Setting(string name, CancellationToken cancellationToken = default)
    {
        var response = await Client.GetSettingAsync(new GetSettingRequest { Context = context, Name = name }, credential, cancellationToken: cancellationToken);
        return response.Status switch
        {
            GetSettingResponse.Types.Status.Found => response.Value,
            GetSettingResponse.Types.Status.NotSet => null,
            GetSettingResponse.Types.Status.IsSecret => throw new HostServiceException($"{name} is declared as a secret: read it with Secret()."),
            _ => throw new HostServiceException($"{name} is not a setting the extension declared.")
        };
    }

    /// <summary>
    /// Reads a secret the extension declared. Do not log it or return it from an ability.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="cancellationToken">A token that cancels the call.</param>
    /// <returns>The value, or null when it is not set.</returns>
    /// <exception cref="HostServiceException">The name is not a secret the extension declared.</exception>
    public async Task<string?> Secret(string name, CancellationToken cancellationToken = default)
    {
        var response = await Client.GetSecretAsync(new GetSecretRequest { Context = context, Name = name }, credential, cancellationToken: cancellationToken);
        return response.Status switch
        {
            GetSecretResponse.Types.Status.Found => response.Value,
            GetSecretResponse.Types.Status.NotSet => null,
            GetSecretResponse.Types.Status.NotSecret => throw new HostServiceException($"{name} is declared as a public setting: read it with Setting()."),
            _ => throw new HostServiceException($"{name} is not a secret the extension declared.")
        };
    }

    /// <summary>
    /// Raises a trigger the extension declared. The model reacts to it.
    /// </summary>
    /// <param name="trigger">The name of the trigger.</param>
    /// <param name="payloadJson">What it carries, as JSON.</param>
    /// <param name="deduplicationKey">The key that makes raising it again find the first raise. Within an invocation it defaults to the idempotency key; outside one it is required.</param>
    /// <param name="correlationKey">What the trigger is about, such as the payment a webhook reports on. Workflow runs that wait for the trigger about the same thing resume.</param>
    /// <param name="cancellationToken">A token that cancels the call.</param>
    /// <returns>What came of it.</returns>
    /// <exception cref="ArgumentException">There is no deduplication key and the call is not within an invocation.</exception>
    public async Task<TriggerResult> Trigger(string trigger, string payloadJson = "{}", string? deduplicationKey = null, string correlationKey = "", CancellationToken cancellationToken = default)
    {
        var key = deduplicationKey ?? (context.IdempotencyKey.Length > 0 ? context.IdempotencyKey : throw new ArgumentException("A trigger raised outside an invocation needs a deduplication key.", nameof(deduplicationKey)));
        var response = await Client.RaiseTriggerAsync(
            new RaiseTriggerRequest { Context = context, Trigger = trigger, DeduplicationKey = key, PayloadJson = payloadJson, CorrelationKey = correlationKey },
            credential,
            cancellationToken: cancellationToken);
        return new TriggerResult(response.Status.ToString(), response.Detail);
    }

    /// <summary>
    /// Completes an invocation the extension accepted to answer later.
    /// </summary>
    /// <param name="outlet">The outlet the answer takes: <c lang="text">returned</c> or a declared outcome.</param>
    /// <param name="payloadJson">What the answer carries, as JSON.</param>
    /// <param name="invocationId">The invocation to answer. It defaults to the invocation this was obtained for; keep the identity to answer from elsewhere.</param>
    /// <param name="cancellationToken">A token that cancels the call.</param>
    /// <returns>The status of the answer, <c lang="text">Answered</c> when it was taken.</returns>
    public async Task<string> Answer(string outlet, string payloadJson = "{}", string? invocationId = null, CancellationToken cancellationToken = default)
    {
        var target = context.Clone();
        if (invocationId is not null) target.InvocationId = invocationId;
        var response = await Client.AnswerAsync(new AnswerRequest { Context = target, Outlet = outlet, PayloadJson = payloadJson }, credential, cancellationToken: cancellationToken);
        return response.Status.ToString();
    }

    /// <summary>
    /// Writes a log record tied to the invocation.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <param name="message">The message. Never put a secret in it.</param>
    /// <param name="attributes">Attributes that go with it.</param>
    /// <param name="cancellationToken">A token that cancels the call.</param>
    /// <returns>A task that completes when Stage has the record.</returns>
    public async Task Log(Microsoft.Extensions.Logging.LogLevel level, string message, IReadOnlyDictionary<string, string>? attributes = null, CancellationToken cancellationToken = default)
    {
        var record = new LogRecord { Level = Map(level), Message = message, Occurred = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow) };
        record.Attributes.AddRange(Attributes(attributes));
        var request = new LogRequest { Context = context };
        request.Records.Add(record);
        await Client.LogAsync(request, credential, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Adds to a counter tied to the invocation.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">What to add.</param>
    /// <param name="attributes">Attributes that go with it.</param>
    /// <param name="cancellationToken">A token that cancels the call.</param>
    /// <returns>A task that completes when Stage has the metric.</returns>
    public Task Counter(string name, double value = 1, IReadOnlyDictionary<string, string>? attributes = null, CancellationToken cancellationToken = default) =>
        Record(new Metric { Name = name, Kind = Metric.Types.Kind.Counter, Value = value }, attributes, cancellationToken);

    /// <summary>
    /// Records one measurement of a distribution.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The measurement.</param>
    /// <param name="unit">The unit, such as <c lang="text">s</c>.</param>
    /// <param name="attributes">Attributes that go with it.</param>
    /// <param name="cancellationToken">A token that cancels the call.</param>
    /// <returns>A task that completes when Stage has the metric.</returns>
    public Task Histogram(string name, double value, string unit = "", IReadOnlyDictionary<string, string>? attributes = null, CancellationToken cancellationToken = default) =>
        Record(new Metric { Name = name, Kind = Metric.Types.Kind.Histogram, Value = value, Unit = unit }, attributes, cancellationToken);

    static IEnumerable<KeyValue> Attributes(IReadOnlyDictionary<string, string>? attributes) =>
        (attributes ?? new Dictionary<string, string>()).Select(pair => new KeyValue { Key = pair.Key, Value = pair.Value });

    static Cratis.Stage.Extensions.Protocol.LogLevel Map(Microsoft.Extensions.Logging.LogLevel level) => level switch
    {
        Microsoft.Extensions.Logging.LogLevel.Trace => Cratis.Stage.Extensions.Protocol.LogLevel.Trace,
        Microsoft.Extensions.Logging.LogLevel.Debug => Cratis.Stage.Extensions.Protocol.LogLevel.Debug,
        Microsoft.Extensions.Logging.LogLevel.Warning => Cratis.Stage.Extensions.Protocol.LogLevel.Warning,
        Microsoft.Extensions.Logging.LogLevel.Error => Cratis.Stage.Extensions.Protocol.LogLevel.Error,
        Microsoft.Extensions.Logging.LogLevel.Critical => Cratis.Stage.Extensions.Protocol.LogLevel.Critical,
        _ => Cratis.Stage.Extensions.Protocol.LogLevel.Information
    };

    async Task Record(Metric metric, IReadOnlyDictionary<string, string>? attributes, CancellationToken cancellationToken)
    {
        metric.Attributes.AddRange(Attributes(attributes));
        var request = new MetricsRequest { Context = context };
        request.Metrics.Add(metric);
        await Client.RecordMetricsAsync(request, credential, cancellationToken: cancellationToken);
    }
}
