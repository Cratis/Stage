// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

sealed class OfflineAuthenticationTransport : IHttpClientFactory, IMsalHttpClientFactory, IMsalMtlsHttpClientFactory, IMsalSFHttpClientFactory, IDisposable
{
    readonly OfflineAuthenticationFixture _owner;
    readonly HttpMessageHandler _handler;
    readonly HttpClient _issuerClient;
    readonly HttpClient _msalClient;
    readonly List<HttpClient> _clients = [];
    readonly Lock _sync = new();
    bool _disposed;

    internal OfflineAuthenticationTransport(OfflineAuthenticationFixture owner, OfflineIssuer issuer)
    {
        _owner = owner;
        _handler = new TerminalHandler(owner, issuer);
        _issuerClient = CreateClient();
        _msalClient = CreateClient();
    }

    HttpClient IHttpClientFactory.CreateClient(string name)
    {
        _owner.RequireActiveScenario();
        return name == OfflineIssuer.IssuerValidationClientName ? _issuerClient : BorrowClient();
    }

    HttpClient IMsalHttpClientFactory.GetHttpClient()
    {
        var diagnostics = _owner.Diagnostics;
        diagnostics.RecordCoarseObservation(OfflineAuthenticationCoarseObservation.MsalFactory, diagnostics.CallbackDispatched);
        _owner.RequireActiveScenario();
        return _msalClient;
    }

    HttpClient IMsalMtlsHttpClientFactory.GetHttpClient(X509Certificate2 x509Certificate2)
    {
        var diagnostics = _owner.Diagnostics;
        diagnostics.RecordCoarseObservation(OfflineAuthenticationCoarseObservation.MsalCertificateFactory, diagnostics.CallbackDispatched);

        // This is transport containment, not a TLS test. Both certificate branches stay in memory.
        _owner.RequireActiveScenario();
        return _msalClient;
    }

    HttpClient IMsalSFHttpClientFactory.GetHttpClient(Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> validateServerCert)
    {
        var diagnostics = _owner.Diagnostics;
        diagnostics.RecordCoarseObservation(OfflineAuthenticationCoarseObservation.MsalSfFactory, diagnostics.CallbackDispatched);
        _owner.RequireActiveScenario();
        _owner.Diagnostics.Record(OfflineAuthenticationEndpoint.Unsupported);
        throw _owner.Diagnostics.Reject(new NotSupportedException());
    }

    internal bool Owns(HttpClient client)
    {
        lock (_sync)
        {
            return !_disposed && (ReferenceEquals(client, _issuerClient) || ReferenceEquals(client, _msalClient) || _clients.Contains(client));
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var client in _clients)
            {
                client.Dispose();
            }

            _issuerClient.Dispose();
            _msalClient.Dispose();
            _handler.Dispose();
        }
    }

    HttpClient BorrowClient()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                throw _owner.Diagnostics.Reject(new ObjectDisposedException(nameof(OfflineAuthenticationTransport)));
            }

            var client = CreateClient();
            _clients.Add(client);
            return client;
        }
    }

    HttpClient CreateClient() => new(_handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(10) };

    static OfflineAuthenticationEndpoint Classify(HttpRequestMessage request)
    {
        if (!IsExpectedAuthority(request.RequestUri))
        {
            return OfflineAuthenticationEndpoint.Unexpected;
        }

        return (request.Method.Method, request.RequestUri!.AbsolutePath) switch
        {
            ("GET", $"/{OfflineIssuer.Tenant}/v2.0/.well-known/openid-configuration" or "/common/v2.0/.well-known/openid-configuration") => OfflineAuthenticationEndpoint.Metadata,
            ("GET", $"/{OfflineIssuer.Tenant}/v2.0/keys") => OfflineAuthenticationEndpoint.Keys,
            ("GET", "/common/discovery/instance") => OfflineAuthenticationEndpoint.InstanceDiscovery,
            ("POST", $"/{OfflineIssuer.Tenant}/v2.0/token") => OfflineAuthenticationEndpoint.Token,
            _ => OfflineAuthenticationEndpoint.Unexpected
        };
    }

    static bool IsExpectedAuthority(Uri? uri) =>
        uri is { IsAbsoluteUri: true, Port: 443, UserInfo: "", Fragment: "" } &&
        string.Equals(uri.Scheme, "https", StringComparison.Ordinal) &&
        string.Equals(uri.Host, "login.microsoftonline.com", StringComparison.OrdinalIgnoreCase);

    sealed class TerminalHandler(OfflineAuthenticationFixture owner, OfflineIssuer issuer) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var diagnostics = owner.Diagnostics;
            var eligible = diagnostics.CallbackDispatched;
            diagnostics.RecordCoarseObservation(OfflineAuthenticationCoarseObservation.SendAsyncEntry, eligible);
            owner.BeginRequest();
            try
            {
                var endpoint = Classify(request);
                diagnostics.RecordCoarseObservation(OfflineAuthenticationCoarseObservation.RouteEntry, eligible);
                diagnostics.Record(endpoint);
                cancellationToken.ThrowIfCancellationRequested();
                if (endpoint == OfflineAuthenticationEndpoint.Unexpected)
                {
                    throw new InvalidOperationException();
                }

                var response = await issuer.Respond(endpoint, request, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    response.Dispose();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return response;
            }
            catch (Exception exception)
            {
                throw diagnostics.Reject(exception);
            }
            finally
            {
                owner.EndRequest();
            }
        }
    }
}
