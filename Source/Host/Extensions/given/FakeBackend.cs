// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Extensions.Protocol;
using Grpc.Core;

namespace Cratis.Stage.Host.Extensions.given;

/// <summary>
/// An extension backend whose answers a specification scripts.
/// </summary>
public sealed class FakeBackend : ExtensionBackend.ExtensionBackendBase
{
    public DescribeRequest? Described { get; private set; }

    public int Handshakes { get; private set; }

    public List<InvokeRequest> Invoked { get; } = [];

    public List<InboundHttpRequest> HttpRequests { get; } = [];

    public DescribeResponse Description { get; set; } = new();

    public Func<InvokeRequest, InvokeResponse> OnInvoke { get; set; } = _ => new InvokeResponse { Returned = new Returned() };

    public Func<InboundHttpRequest, InboundHttpResponse> OnHttp { get; set; } = _ => new InboundHttpResponse { Status = 200 };

    /// <summary>
    /// Gets or sets whether the next invocation is cut off on the wire instead of answered, the way a backend that goes away mid-call is.
    /// </summary>
    public bool CutOffNextInvoke { get; set; }

    public override Task<DescribeResponse> Describe(DescribeRequest request, ServerCallContext context)
    {
        Described = request;
        Handshakes++;
        return Task.FromResult(Description);
    }

    public override Task<InvokeResponse> Invoke(InvokeRequest request, ServerCallContext context)
    {
        Invoked.Add(request);
        if (CutOffNextInvoke)
        {
            CutOffNextInvoke = false;
            context.GetHttpContext().Abort();
            return Task.FromResult(new InvokeResponse());
        }

        return Task.FromResult(OnInvoke(request));
    }

    public override Task<InboundHttpResponse> HandleHttp(InboundHttpRequest request, ServerCallContext context)
    {
        HttpRequests.Add(request);
        return Task.FromResult(OnHttp(request));
    }

    public override Task<HealthResponse> Health(HealthRequest request, ServerCallContext context) =>
        Task.FromResult(new HealthResponse { Status = HealthResponse.Types.Status.Serving });
}
