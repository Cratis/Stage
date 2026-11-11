// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Stage.Extensions.Protocol;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using HttpHeader = Cratis.Stage.Extensions.Protocol.Header;
using RequestContext = Cratis.Stage.Extensions.Protocol.RequestContext;

namespace Cratis.Stage.Extensions;

/// <summary>
/// Serves the extension protocol for an <see cref="ExtensionDefinition"/>: the handshake, abilities, inbound HTTP and health.
/// </summary>
/// <param name="definition">The <see cref="ExtensionDefinition"/>.</param>
public sealed class ExtensionBackendService(ExtensionDefinition definition) : ExtensionBackend.ExtensionBackendBase, IDisposable
{
    const int MajorVersion = 1;
    const int MinorVersion = 0;

    GrpcChannel? _hostChannel;
    StageHost.StageHostClient? _host;
    Metadata _credential = [];

    /// <inheritdoc/>
    public override Task<DescribeResponse> Describe(DescribeRequest request, ServerCallContext context)
    {
        if (request.ProtocolVersion?.Major != MajorVersion)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"This SDK speaks protocol major {MajorVersion}; Stage speaks {request.ProtocolVersion?.Major}."));
        }

        // The credential arrives here and is kept in memory: nowhere else, never in the environment or a file.
        _hostChannel?.Dispose();
        _hostChannel = request.HostEndpoint.Length > 0 ? GrpcChannel.ForAddress(request.HostEndpoint) : null;
        _host = _hostChannel is null ? null : new StageHost.StageHostClient(_hostChannel);
        _credential = new Metadata { { "authorization", $"Bearer {request.SessionToken}" } };

        var response = new DescribeResponse
        {
            ProtocolVersion = new ProtocolVersion { Major = MajorVersion, Minor = MinorVersion },
            Name = definition.Name,
            Version = definition.Version
        };
        foreach (var ability in definition.Abilities)
        {
            var declaration = new AbilityDeclaration { Name = ability.Name, ContractMajor = ability.ContractMajor, Effect = Effect(ability.Effect) };
            declaration.Outcomes.AddRange(ability.Outcomes);
            response.Abilities.Add(declaration);
        }

        response.Triggers.AddRange(definition.Triggers.Select(trigger => new TriggerDeclaration { Name = trigger }));
        response.Settings.AddRange(definition.Settings.Select(setting => new SettingDeclaration { Name = setting.Name, Secret = setting.Secret, Required = setting.Required }));
        response.HttpRoutes.AddRange(definition.Routes.Select(route => route.Prefix));
        return Task.FromResult(response);
    }

    /// <inheritdoc/>
    public override async Task<InvokeResponse> Invoke(InvokeRequest request, ServerCallContext context)
    {
        var ability = definition.Abilities.FirstOrDefault(candidate => candidate.Name == request.Ability);
        if (ability is null || ability.ContractMajor != request.ContractMajor)
        {
            return new InvokeResponse { NotSent = new NotSent { Reason = $"The extension does not realize {request.Ability}@{request.ContractMajor}." } };
        }

        var call = new AbilityContext(request.Context, request.InputJson, new HostServices(_host, _credential, request.Context), context.CancellationToken);
        try
        {
            return Map(await ability.Handler(call));
        }
        catch (Exception exception)
        {
            // An exception says nothing about whether the effect happened, so it is not ruled out.
            return new InvokeResponse { Failed = new Failed { Reason = exception.Message } };
        }
    }

    /// <inheritdoc/>
    public override async Task<InboundHttpResponse> HandleHttp(InboundHttpRequest request, ServerCallContext context)
    {
        var route = definition.Routes.Where(candidate => request.Path == candidate.Prefix || request.Path.StartsWith(candidate.Prefix.TrimEnd('/') + "/", StringComparison.Ordinal))
            .OrderByDescending(candidate => candidate.Prefix.Length)
            .FirstOrDefault();
        if (route.Handler is null) return new InboundHttpResponse { Status = 404 };

        var headers = request.Headers.ToDictionary(header => header.Name, header => header.Values.ToArray(), StringComparer.OrdinalIgnoreCase);
        var reply = await route.Handler(new HttpCall(request.Method, request.Path, request.Query, headers, request.Body.Memory.ToArray()), context.CancellationToken);
        var response = new InboundHttpResponse { Status = reply.Status, Body = ByteString.CopyFrom(reply.Body.Span) };
        foreach (var header in reply.Headers ?? new Dictionary<string, string[]>())
        {
            var item = new HttpHeader { Name = header.Key };
            item.Values.AddRange(header.Value);
            response.Headers.Add(item);
        }

        return response;
    }

    /// <inheritdoc/>
    public override Task<HealthResponse> Health(HealthRequest request, ServerCallContext context) =>
        Task.FromResult(new HealthResponse { Status = HealthResponse.Types.Status.Serving });

    /// <inheritdoc/>
    public void Dispose() => _hostChannel?.Dispose();

    static InvokeResponse Map(AbilityOutcome outcome) => outcome switch
    {
        AbilityOutcome.ReturnedOutcome returned => new InvokeResponse { Returned = new Returned { Outcome = returned.Outcome, OutputJson = returned.OutputJson } },
        AbilityOutcome.AcceptedOutcome => new InvokeResponse { Accepted = new Accepted() },
        AbilityOutcome.NotSentOutcome notSent => new InvokeResponse { NotSent = new NotSent { Reason = notSent.Reason } },
        AbilityOutcome.FailedOutcome failed => new InvokeResponse { Failed = new Failed { Reason = failed.Reason, EffectRuledOut = failed.EffectRuledOut } },
        _ => new InvokeResponse { Failed = new Failed { Reason = "The ability answered with nothing." } }
    };

    static Cratis.Stage.Extensions.Protocol.EffectClass Effect(EffectClass effect) => effect switch
    {
        EffectClass.Safe => Cratis.Stage.Extensions.Protocol.EffectClass.Safe,
        EffectClass.Idempotent => Cratis.Stage.Extensions.Protocol.EffectClass.Idempotent,
        _ => Cratis.Stage.Extensions.Protocol.EffectClass.Once
    };
}
