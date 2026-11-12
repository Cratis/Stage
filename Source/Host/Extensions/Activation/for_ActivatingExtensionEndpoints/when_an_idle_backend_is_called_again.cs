// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.Activation.given;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Extensions.Packages;
using Cratis.Stage.Host.Extensions.Packages.given;
using Cratis.Stage.Host.Invocations;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using EffectClass = Cratis.Stage.Host.Invocations.EffectClass;

namespace Cratis.Stage.Host.Extensions.Activation.for_ActivatingExtensionEndpoints;

/// <summary>
/// Calls an ability through the whole path - activation, handshake and the call - against a backend that is really a gRPC server, so what is checked is that an
/// idle backend that stops is started again by the next call and that call is not lost.
/// </summary>
public class when_an_idle_backend_is_called_again : a_backend_package
{
    ServerRuntime _runtime = null!;
    ExtensionConnections _connections = null!;
    AbilityResult _first = null!;
    AbilityResult _afterIdle = null!;
    BackendState _stateWhileIdle;
    int _startsAfterIdle;

    async Task Because()
    {
        await LoadBackend("dotnet");
        var time = new ManualTime();
        var options = new ExtensionOptions();
        options.Execution.Sharing.Publishers = [PackageFixtures.Publisher];
        options.Execution.Hosts["dotnet"] = new() { Command = "unused" };
        var accessor = Options.Create(options);
        _runtime = new();

        var packages = new ExtensionPackages(_result, new Dictionary<string, string> { ["Mail"] = PackageFixtures.Reference });
        var activator = new ExtensionActivator(new(packages, null, accessor), [_runtime], accessor, time, NullLogger<ExtensionActivator>.Instance);
        var catalog = new AbilityCatalog([new AbilityDescriptor(new("Mail", "Send", 1), EffectClass.Once, ["sent"], "sha256:aaaa")]);
        _connections = new ExtensionConnections(new ActivatingExtensionEndpoints(new ConfiguredExtensionEndpoints(accessor), activator), catalog, new ExtensionSessions(), accessor, NullLogger<ExtensionConnections>.Instance);
        activator.Stopped += _connections.Forget;
        var abilities = new GrpcAbilityBackend(_connections, NullLogger<GrpcAbilityBackend>.Instance, activator);

        _first = await abilities.Invoke(Call(), CancellationToken.None);

        time.Advance(TimeSpan.FromMinutes(11));
        await activator.Sweep(CancellationToken.None);
        _stateWhileIdle = activator.Systems.Find("Mail")!.State;

        _afterIdle = await abilities.Invoke(Call(), CancellationToken.None);
        _startsAfterIdle = _runtime.Started;
    }

    void Destroy()
    {
        _connections.Dispose();
        _runtime.Dispose();
    }

    static AbilityCall Call() => new(
        new("Mail", "Send", 1),
        EffectClass.Once,
        "invocation-1",
        1,
        "{}",
        "sha256:aaaa",
        TimeSpan.FromSeconds(10),
        "Default",
        new(InvocationOriginKind.ReactionBranch, "Fulfillment/Send/branch 1", "event-42"));

    [Fact] void should_perform_the_first_call_by_starting_the_backend() => _first.ShouldBeOfExactType<AbilityReturned>();
    [Fact] void should_stop_the_backend_when_it_is_idle() => _stateWhileIdle.ShouldEqual(BackendState.Inactive);
    [Fact] void should_not_lose_the_call_that_finds_it_stopped() => _afterIdle.ShouldBeOfExactType<AbilityReturned>();
    [Fact] void should_have_started_the_backend_twice() => _startsAfterIdle.ShouldEqual(2);
    [Fact] void should_have_called_each_backend_once() => _runtime.Servers.Select(server => server.Backend.Invoked.Count).ShouldContainOnly(1, 1);

    sealed class ServerRuntime : IBackendRuntime, IDisposable
    {
        public List<Server> Servers { get; } = [];

        public int Started => Servers.Count;

        public ExecutionTier Tier => ExecutionTier.SharedHost;

        public async Task<StartedBackend> Start(BackendLaunch launch, CancellationToken cancellationToken)
        {
            var backend = new Cratis.Stage.Host.Extensions.given.FakeBackend { Description = Described() };
            var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
            builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, launch.Address.Port, listen => listen.Protocols = HttpProtocols.Http2));
            builder.Services.AddSingleton(backend);
            builder.Services.AddRouting();
            builder.Services.AddGrpc();
            var app = builder.Build();
            app.UseRouting();
            app.MapGrpcService<Cratis.Stage.Host.Extensions.given.FakeBackend>();
            await app.StartAsync(cancellationToken);
            var server = new Server(app, backend);
            Servers.Add(server);
            return server;
        }

        public void Dispose()
        {
            foreach (var server in Servers) server.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        static DescribeResponse Described()
        {
            var response = new DescribeResponse { ProtocolVersion = new ProtocolVersion { Major = 1, Minor = 0 }, Name = "Mail extension", Version = "1.0.0" };
            var declaration = new AbilityDeclaration { Name = "Send", ContractMajor = 1, Effect = Cratis.Stage.Extensions.Protocol.EffectClass.Once };
            declaration.Outcomes.Add("sent");
            response.Abilities.Add(declaration);
            return response;
        }
    }

    sealed class Server(WebApplication app, Cratis.Stage.Host.Extensions.given.FakeBackend backend) : StartedBackend
    {
        readonly TaskCompletionSource<string> _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Cratis.Stage.Host.Extensions.given.FakeBackend Backend => backend;

        public override Task<string> Exited => _exited.Task;

        public override async ValueTask DisposeAsync()
        {
            await app.StopAsync();
            await app.DisposeAsync();
            _exited.TrySetResult("stopped");
        }
    }
}
