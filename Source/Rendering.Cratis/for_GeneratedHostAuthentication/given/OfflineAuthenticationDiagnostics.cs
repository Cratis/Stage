// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

enum OfflineAuthenticationEndpoint
{
    Metadata,
    Keys,
    InstanceDiscovery,
    Token,
    Unexpected,
    Unsupported
}

enum OfflineAuthenticationPhase
{
    None,
    ChallengeReturned,
    CallbackDispatched,
    OidcMessageReceived,
    AuthorizationCodeHandlerEntered,
    AuthorizationCodeHandlerReturned
}

// Factory entry during construction does not prove that a confidential client was returned.
enum OfflineAuthenticationCoarseObservation
{
    MsalFactory,
    MsalCertificateFactory,
    MsalSfFactory,
    SendAsyncEntry,
    RouteEntry
}

sealed class OfflineAuthenticationDiagnostics
{
    readonly long[] _endpoints = new long[6];
    readonly long[] _coarseObservations = new long[5];
    readonly long[] _credentialEvents = new long[3];
    readonly Dictionary<string, long> _exceptions = [];
    readonly Lock _sync = new();
    long _failures;
    int _published;
    int _lastPhase;
    int _callbackDispatched;

    internal bool CallbackDispatched => Volatile.Read(ref _callbackDispatched) != 0;

    internal void Observe(OfflineAuthenticationPhase phase)
    {
        if (phase == OfflineAuthenticationPhase.CallbackDispatched)
        {
            Interlocked.Exchange(ref _callbackDispatched, 1);
        }

        Volatile.Write(ref _lastPhase, (int)phase);
    }

    internal void RecordCoarseObservation(OfflineAuthenticationCoarseObservation observation, bool eligible)
    {
        if (eligible)
        {
            Interlocked.Increment(ref _coarseObservations[(int)observation]);
        }
    }

    // Absence is ambiguous (including cache bypass or an earlier wait). 405 -> 403 marks
    // only credential interval passage, not a returned confidential client.
    internal void RecordCredentialEvent(int eventId)
    {
        if (!CallbackDispatched)
        {
            return;
        }

        var index = eventId switch { 405 => 0, 403 => 1, 406 => 2, _ => -1 };
        if (index >= 0)
        {
            Interlocked.Increment(ref _credentialEvents[index]);
        }
    }

    internal void Record(OfflineAuthenticationEndpoint endpoint) => Interlocked.Increment(ref _endpoints[(int)endpoint]);

    internal void Failure(Exception exception)
    {
        Interlocked.Increment(ref _failures);
        lock (_sync)
        {
            var type = exception.GetType().Name;
            if (_exceptions.TryGetValue(type, out var count))
            {
                _exceptions[type] = count + 1;
            }
            else if (_exceptions.Count < 16)
            {
                _exceptions.Add(type, 1);
            }
        }
    }

    // Individual atomic reads form a nontransactional, coarse snapshot.
    internal OfflineAuthenticationSnapshot Snapshot()
    {
        lock (_sync)
        {
            return new OfflineAuthenticationSnapshot
            {
                LastPhase = (OfflineAuthenticationPhase)Volatile.Read(ref _lastPhase),
                Metadata = Interlocked.Read(ref _endpoints[0]),
                Keys = Interlocked.Read(ref _endpoints[1]),
                InstanceDiscovery = Interlocked.Read(ref _endpoints[2]),
                Token = Interlocked.Read(ref _endpoints[3]),
                Unexpected = Interlocked.Read(ref _endpoints[4]),
                Unsupported = Interlocked.Read(ref _endpoints[5]),
                Failures = Interlocked.Read(ref _failures),
                MsalFactory = Interlocked.Read(ref _coarseObservations[0]),
                MsalCertificateFactory = Interlocked.Read(ref _coarseObservations[1]),
                MsalSfFactory = Interlocked.Read(ref _coarseObservations[2]),
                SendAsyncEntry = Interlocked.Read(ref _coarseObservations[3]),
                RouteEntry = Interlocked.Read(ref _coarseObservations[4]),
                CredentialEvent405 = Interlocked.Read(ref _credentialEvents[0]),
                CredentialEvent403 = Interlocked.Read(ref _credentialEvents[1]),
                CredentialEvent406 = Interlocked.Read(ref _credentialEvents[2]),
                ExceptionTypes = string.Join(' ', _exceptions.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}"))
            };
        }
    }

    internal OfflineAuthenticationFailure Reject(Exception exception)
    {
        Failure(exception);
        var snapshot = Snapshot();
        if (Interlocked.Exchange(ref _published, 1) == 0)
        {
            Console.Error.WriteLine(snapshot);
        }

        // No original exception or inner exception may reach the runner's failure formatter.
        return new OfflineAuthenticationFailure(snapshot);
    }
}

sealed record OfflineAuthenticationSnapshot
{
    internal OfflineAuthenticationPhase LastPhase { get; init; }
    internal long Metadata { get; init; }
    internal long Keys { get; init; }
    internal long InstanceDiscovery { get; init; }
    internal long Token { get; init; }
    internal long Unexpected { get; init; }
    internal long Unsupported { get; init; }
    internal long Failures { get; init; }
    internal long MsalFactory { get; init; }
    internal long MsalCertificateFactory { get; init; }
    internal long MsalSfFactory { get; init; }
    internal long SendAsyncEntry { get; init; }
    internal long RouteEntry { get; init; }
    internal long CredentialEvent405 { get; init; }
    internal long CredentialEvent403 { get; init; }
    internal long CredentialEvent406 { get; init; }
    internal string ExceptionTypes { get; init; } = string.Empty;

    public override string ToString() => $"last-phase={LastPhase} metadata={Metadata} keys={Keys} instance-discovery={InstanceDiscovery} token={Token} unexpected={Unexpected} unsupported-flow={Unsupported} failures={Failures} coarse-msal-factory-since-callback-dispatch={MsalFactory} coarse-msal-cert-factory-since-callback-dispatch={MsalCertificateFactory} coarse-msal-sf-factory-since-callback-dispatch={MsalSfFactory} coarse-send-async-entry-since-callback-dispatch={SendAsyncEntry} coarse-route-entry-since-callback-dispatch={RouteEntry} credential-event-405-since-callback-dispatch={CredentialEvent405} credential-event-403-since-callback-dispatch={CredentialEvent403} credential-event-406-since-callback-dispatch={CredentialEvent406} {ExceptionTypes}".TrimEnd();
}

/// <summary>
/// The exception that is thrown when the offline fixture rejects an unsafe operation.
/// </summary>
/// <param name="snapshot">The safe diagnostic counts for the rejected operation.</param>
sealed class OfflineAuthenticationFailure(OfflineAuthenticationSnapshot snapshot) : Exception(snapshot.ToString());
