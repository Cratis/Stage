// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Extensions.Protocol;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;
using RequestContext = Cratis.Stage.Extensions.Protocol.RequestContext;

namespace Cratis.Stage.Host.Extensions.Inbound;

/// <summary>
/// Delivers stored inbound requests to the extensions that own them. The sender was answered when the request was stored, so nothing here is on its clock: an
/// extension that is not running is activated, a request that cannot be delivered is retried with a growing wait, and one that cannot be delivered at all
/// stays in the Workbench as failed.
/// </summary>
/// <param name="deliveries">The <see cref="IInboundDeliveries"/>.</param>
/// <param name="connections">The <see cref="ExtensionConnections"/>.</param>
/// <param name="options">The <see cref="ExtensionOptions"/>.</param>
/// <param name="time">The clock.</param>
/// <param name="logger">The logger.</param>
public sealed class InboundDeliveryDispatcher(
    IInboundDeliveries deliveries,
    ExtensionConnections connections,
    IOptions<ExtensionOptions> options,
    TimeProvider time,
    ILogger<InboundDeliveryDispatcher> logger) : BackgroundService
{
    const int Parallelism = 8;

    static readonly string[] _sensitive = ["Cookie", "Authorization"];

    readonly SemaphoreSlim _wake = new(0, int.MaxValue);

    /// <summary>
    /// Asks the dispatcher to look for due requests now, rather than at its next poll.
    /// </summary>
    public void Wake() => _wake.Release();

    /// <summary>
    /// Delivers every request that is due once.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the work should stop.</param>
    /// <returns>How many attempts were made.</returns>
    public async Task<int> RunDue(CancellationToken cancellationToken)
    {
        var due = await deliveries.Due(time.GetUtcNow());
        await Parallel.ForEachAsync(due, new ParallelOptions { MaxDegreeOfParallelism = Parallelism, CancellationToken = cancellationToken }, async (delivery, token) => await Attempt(delivery, token));
        return due.Count;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await RunDue(stoppingToken) > 0) continue;
                await _wake.WaitAsync(options.Value.Inbound.PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                ExtensionLog.InboundRoundFailed(logger, exception);
                await Task.Delay(options.Value.Inbound.PollInterval, stoppingToken);
            }
        }
    }

    async Task Attempt(InboundDelivery delivery, CancellationToken cancellationToken)
    {
        var (outcome, status, reason) = await Deliver(delivery, cancellationToken);
        var settled = await deliveries.Settle(delivery.Id, outcome, status, reason);
        ExtensionLog.InboundSettled(logger, settled.Id, settled.System, settled.Status, settled.Attempts, reason);
    }

    async Task<(InboundOutcome Outcome, int Status, string Reason)> Deliver(InboundDelivery delivery, CancellationToken cancellationToken)
    {
        var timeout = options.Value.Inbound.AttemptTimeout;
        try
        {
            var result = await connections.Connect(delivery.System, cancellationToken);
            if (result.Connection is not { } connection) return (InboundOutcome.Retry, 0, result.Refusal);
            if (!connection.Description.HttpRoutes.Any(route => ExtensionHttpEndpoints.Matches(route, delivery.Path)))
            {
                return (InboundOutcome.Rejected, 0, $"{delivery.System} declares no route for {delivery.Path}.");
            }

            try
            {
                var deadline = time.GetUtcNow().UtcDateTime + timeout;
                var response = await connection.Client.HandleHttpAsync(Request(delivery, deadline), deadline: deadline, cancellationToken: cancellationToken);
                return (InboundDeliveryRules.Classify(response.Status), response.Status, $"The extension answered {response.Status}.");
            }
            catch (RpcException exception)
            {
                if (exception.StatusCode == StatusCode.Unavailable) connections.Forget(delivery.System);
                return (InboundOutcome.Retry, 0, $"The extension could not be reached: {exception.StatusCode}.");
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (InboundOutcome.Retry, 0, "The extension did not answer in time.");
        }
    }

    InboundHttpRequest Request(InboundDelivery delivery, DateTime deadline)
    {
        var request = new InboundHttpRequest
        {
            Context = new RequestContext
            {
                System = delivery.System,
                Tenant = Invocations.AbilityInvocations.Scope.Value,
                InvocationId = delivery.Id,
                IdempotencyKey = delivery.Id,
                Attempt = delivery.Attempts + 1,
                Deadline = Timestamp.FromDateTime(deadline)
            },
            Method = delivery.Method,
            Path = delivery.Path,
            Query = delivery.Query,
            Body = ByteString.CopyFrom(delivery.Body)
        };
        foreach (var header in delivery.Headers.Where(header => !_sensitive.Contains(header.Name, StringComparer.OrdinalIgnoreCase)))
        {
            var forwarded = new Header { Name = header.Name };
            forwarded.Values.AddRange(header.Values);
            request.Headers.Add(forwarded);
        }

        return request;
    }
}
