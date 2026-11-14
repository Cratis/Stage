// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

/// <summary>
/// Owns the offline authority for exactly one serialized collection in this process.
/// </summary>
public sealed class OfflineAuthenticationFixture : IDisposable
{
    // AadIssuerValidator retains the first client by authority process-wide. Never release this claim:
    // a later collection must fail instead of inheriting a disposed client from the library cache.
    static int _authorityClaimed;
    readonly Lock _sync = new();
    OfflineAuthenticationScenarioState? _scenario;
    TaskCompletionSource? _requestsDrained;
    OfflineAuthenticationDiagnostics _lastDiagnostics = new();
    int _hosts;
    int _requests;
    bool _disposed;

    /// <summary>
    /// Initializes the exclusive collection owner without opening any network transport.
    /// </summary>
    public OfflineAuthenticationFixture()
    {
        if (Interlocked.CompareExchange(ref _authorityClaimed, 1, 0) != 0)
        {
            throw _lastDiagnostics.Reject(new InvalidOperationException());
        }

        Issuer = new OfflineIssuer();
        Transport = new OfflineAuthenticationTransport(this, Issuer);
    }

    internal OfflineIssuer Issuer { get; }
    internal OfflineAuthenticationTransport Transport { get; }
    bool HasBorrowers => _hosts != 0 || _requests != 0;
    bool IsIdle => _scenario is null && !HasBorrowers;

    internal OfflineAuthenticationDiagnostics Diagnostics
    {
        get
        {
            lock (_sync)
            {
                return _scenario?.Diagnostics ?? _lastDiagnostics;
            }
        }
    }

    internal OfflineAuthenticationScenario BeginScenario()
    {
        lock (_sync)
        {
            if (_disposed || !IsIdle)
            {
                throw Diagnostics.Reject(new InvalidOperationException());
            }

            Issuer.ResetExchange();
            _lastDiagnostics = new OfflineAuthenticationDiagnostics();
            _scenario = new OfflineAuthenticationScenarioState(_lastDiagnostics);
            return new OfflineAuthenticationScenario(this, _scenario);
        }
    }

    internal void RequireActiveScenario()
    {
        lock (_sync)
        {
            RequireScenario();
        }
    }

    internal void BorrowHost()
    {
        lock (_sync)
        {
            RequireScenario();
            _hosts++;
        }
    }

    internal void ReturnHost()
    {
        lock (_sync)
        {
            _hosts--;
        }
    }

    internal void BeginRequest()
    {
        lock (_sync)
        {
            RequireScenario();
            if (_requests == 0)
            {
                _requestsDrained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            _requests++;
        }
    }

    internal void EndRequest()
    {
        lock (_sync)
        {
            _requests--;
            if (_requests == 0)
            {
                _requestsDrained?.TrySetResult();
                _requestsDrained = null;
            }
        }
    }

    internal Task DrainRequests()
    {
        lock (_sync)
        {
            return _requestsDrained?.Task ?? Task.CompletedTask;
        }
    }

    internal void ReleaseRequestResources(Action release)
    {
        lock (_sync)
        {
            // Recheck after the asynchronous drain: never dispose resources of a newly active request.
            if (_requests != 0)
            {
                throw Diagnostics.Reject(new InvalidOperationException());
            }

            release();
        }
    }

    internal void EndScenario(OfflineAuthenticationScenarioState scenario)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_scenario, scenario) || HasBorrowers)
            {
                throw Diagnostics.Reject(new InvalidOperationException());
            }

            Issuer.ResetExchange();
            _scenario = null;
        }
    }

    internal void AssertBindings(IServiceProvider services) => new OfflineAuthenticationBindings(this).Assert(services);

    internal Task AssertRetainedClients() => new OfflineAuthenticationBindings(this).AssertRetainedClients();

    /// <summary>
    /// Disposes all clients and signing material only after every scenario and host has finished.
    /// </summary>
    /// <exception cref="OfflineAuthenticationFailure">A scenario, host, or request is still active.</exception>
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            if (!IsIdle)
            {
                throw Diagnostics.Reject(new InvalidOperationException());
            }

            _disposed = true;
            Transport.Dispose();
            Issuer.Dispose();
        }
    }

    void RequireScenario()
    {
        if (_disposed || _scenario is null)
        {
            Diagnostics.Record(OfflineAuthenticationEndpoint.Unexpected);
            throw Diagnostics.Reject(new InvalidOperationException());
        }
    }
}
