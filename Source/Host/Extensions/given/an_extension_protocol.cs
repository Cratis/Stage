// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Invocations;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using EffectClass = Cratis.Stage.Host.Invocations.EffectClass;

namespace Cratis.Stage.Host.Extensions.given;

/// <summary>
/// Stage's side of the extension protocol and a scripted backend, both really listening on loopback, so a specification crosses the wire the way an extension does.
/// </summary>
public abstract class an_extension_protocol : Specification
{
    protected const string System = "Slack";

    protected FakeBackend _backend = null!;
    protected RecordingLoggers _logs = null!;
    protected Dictionary<string, string?> _values = null!;
    protected ExtensionSessions _sessions = null!;
    protected ExtensionOptions _options = null!;
    protected ExtensionConnections _connections = null!;
    protected GrpcAbilityBackend _abilities = null!;
    protected IContractTriggers _triggers = null!;
    protected IAbilityInvocations _invocations = null!;
    protected ExtensionHostServer _server = null!;
    protected WebApplication _backendApp = null!;
    protected string _backendAddress = string.Empty;
    protected GrpcChannel? _hostChannel;

    protected virtual IEnumerable<AbilityDescriptor> Declared => [Descriptor("Post", EffectClass.Once, "posted")];

    protected virtual IDictionary<string, ExtensionBackendOptions> Backends => new Dictionary<string, ExtensionBackendOptions> { [System] = new() { Endpoint = _backendAddress } };

    protected static AbilityDescriptor Descriptor(string name, EffectClass effect, params string[] outcomes) =>
        new(new(System, name, 1), effect, [.. outcomes], "sha256:aaaa");

    protected static DescribeResponse Described(params AbilityDeclaration[] abilities)
    {
        var response = new DescribeResponse
        {
            ProtocolVersion = new ProtocolVersion { Major = 1, Minor = 0 },
            Name = "Slack extension",
            Version = "1.2.3"
        };
        response.Abilities.AddRange(abilities);
        return response;
    }

    protected static AbilityDeclaration Ability(string name, Cratis.Stage.Extensions.Protocol.EffectClass effect, params string[] outcomes)
    {
        var declaration = new AbilityDeclaration { Name = name, ContractMajor = 1, Effect = effect };
        declaration.Outcomes.AddRange(outcomes);
        return declaration;
    }

    protected static AbilityCall Call(string ability = "Post", string input = "{}", int attempt = 1) =>
        new(new(System, ability, 1), EffectClass.Once, "invocation-1", attempt, input, "sha256:aaaa", TimeSpan.FromSeconds(10), "Default", new(InvocationOriginKind.ReactionBranch, "Fulfillment/Post/branch 1", "event-42"));

    protected async Task StartBackend()
    {
        _backend = new FakeBackend { Description = Described(Ability("Post", Cratis.Stage.Extensions.Protocol.EffectClass.Once, "posted")) };
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http2));
        builder.Services.AddSingleton(_backend);
        builder.Services.AddRouting();
        builder.Services.AddGrpc();
        _backendApp = builder.Build();
        _backendApp.UseRouting();
        _backendApp.MapGrpcService<FakeBackend>();
        await _backendApp.StartAsync();
        _backendAddress = _backendApp.Urls.First();
    }

    protected async Task StartStage()
    {
        _logs = new RecordingLoggers();
        _values = [];
        _sessions = new ExtensionSessions();
        _triggers = Substitute.For<IContractTriggers>();
        _invocations = Substitute.For<IAbilityInvocations>();
        _options = new ExtensionOptions { HostPort = FreePort(), Backends = Backends };

        var settings = Substitute.For<ISystemSettings>();
        settings.Resolve(Arg.Any<string>(), Arg.Any<string>()).Returns(call => _values.GetValueOrDefault($"{call.ArgAt<string>(0)}:{call.ArgAt<string>(1)}"));
        var optionsAccessor = Options.Create(_options);
        var catalog = new AbilityCatalog(Declared);
        _connections = new ExtensionConnections(new ConfiguredExtensionEndpoints(optionsAccessor), catalog, _sessions, optionsAccessor, _logs.CreateLogger<ExtensionConnections>());
        _abilities = new GrpcAbilityBackend(_connections, _logs.CreateLogger<GrpcAbilityBackend>());

        var provider = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(_logs)
            .AddSingleton(_sessions)
            .AddSingleton(settings)
            .AddSingleton(_triggers)
            .AddSingleton(_invocations)
            .AddSingleton(new ExtensionTelemetry(_logs))
            .BuildServiceProvider();
        _server = new ExtensionHostServer(provider, optionsAccessor, _logs.CreateLogger<ExtensionHostServer>());
        await _server.StartAsync(CancellationToken.None);
    }

    /// <summary>
    /// Does the handshake, which hands the credential over, and returns a client of the host services that presents it.
    /// </summary>
    /// <returns>The client.</returns>
    protected async Task<StageHost.StageHostClient> Connected()
    {
        var connected = await _connections.Connect(System, CancellationToken.None);
        Assert.NotNull(connected.Connection);
        return HostClient(_backend.Described!.SessionToken);
    }

    protected StageHost.StageHostClient HostClient(string? token)
    {
        _hostChannel ??= GrpcChannel.ForAddress($"http://127.0.0.1:{_server.Port}");
        var credentials = token is null ? null : CallCredentials.FromInterceptor((_, metadata) =>
        {
            metadata.Add("authorization", $"Bearer {token}");
            return Task.CompletedTask;
        });
        var channel = credentials is null ? _hostChannel : GrpcChannel.ForAddress($"http://127.0.0.1:{_server.Port}", new GrpcChannelOptions { Credentials = ChannelCredentials.Create(ChannelCredentials.Insecure, credentials), UnsafeUseInsecureChannelCallCredentials = true });
        return new StageHost.StageHostClient(channel);
    }

    protected async Task Destroy()
    {
        _hostChannel?.Dispose();
        _connections?.Dispose();
        if (_server is not null) await _server.StopAsync(CancellationToken.None);
        if (_backendApp is not null)
        {
            await _backendApp.StopAsync();
            await _backendApp.DisposeAsync();
        }
    }

    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
