// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Choreography.Hosting;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.Activation;
using Cratis.Stage.Host.Extensions.Components;
using Cratis.Stage.Host.Extensions.Packages;
using Cratis.Stage.Host.Extensions.Packages.given;
using Cratis.Stage.Host.Invocations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Orleans.Configuration;
using Orleans.Hosting;
using Xunit;

namespace Cratis.Stage.Host.Extensions.given;

/// <summary>
/// The Stage host of <see cref="an_extension_application"/>, composed the way <see cref="SemanticHost"/> composes it: invocations and extension
/// hosting first, then the catalogs the model and the loaded packages describe, over a real Orleans silo. Only the backend process is replaced,
/// by a backend in the spec's own process that speaks the same gRPC protocol.
/// </summary>
public abstract class an_extension_host : an_extension_application
{
    protected WebApplication _app = null!;
    protected FakeBackend _backend = null!;
    protected InProcessBackendRuntime _runtime = null!;

    protected IServiceProvider Services => _app.Services;

    /// <summary>
    /// Describes the ability the way the sample's backend declares it: <c language="csharp">Notify</c>, once, with the <c language="csharp">Muted</c> outcome.
    /// </summary>
    /// <returns>The <see cref="DescribeResponse"/>.</returns>
    protected static DescribeResponse Describe()
    {
        var ability = new AbilityDeclaration { Name = "Notify", ContractMajor = 1, Effect = Cratis.Stage.Extensions.Protocol.EffectClass.Once };
        ability.Outcomes.Add("Muted");
        var response = new DescribeResponse { ProtocolVersion = new ProtocolVersion { Major = 1, Minor = 0 }, Name = "notifier", Version = "1.0.0" };
        response.Abilities.Add(ability);
        return response;
    }

    protected static ClaimsPrincipal Person(string name) => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "spec"));

    protected static async Task Eventually(Func<Task<bool>> condition, string expectation)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (!await condition())
        {
            Assert.True(DateTimeOffset.UtcNow < deadline, $"Timed out waiting for {expectation}.");
            await Task.Delay(50);
        }
    }

    protected async Task StartHost()
    {
        await LoadApplication();
        _backend = new()
        {
            Description = Describe(),
            OnInvoke = request => new InvokeResponse { Returned = new Returned { OutputJson = $$"""{"noticeId":"{{JsonElement.Parse(request.InputJson).GetProperty("message").GetString()}}"}""" } }
        };
        _runtime = new(_backend);

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration["Stage:Invocations:AllowVolatileOnce"] = "true";
        builder.Configuration["Stage:Extensions:HostPort"] = FreePort().ToString();
        builder.Configuration["systems:Notifier:channel"] = "ops";
        builder.Configuration["systems:Notifier:token"] = "s3cret";
        builder.Services.AddAuthorization();
        builder.Services.AddStageInvocations(builder.Configuration).ComposeWithAbilities();
        var siloPort = FreePort();
        builder.AddCratisChoreography(new CratisChoreographyOptions
        {
            Clustering = ClusteringMode.Localhost,
            Storage = GrainStorageMode.InMemory,
            ClusterId = "extension-spec",
            ServiceId = "extension-spec",
            Workflows = true
        });
        builder.Services.Configure<EndpointOptions>(options =>
        {
            options.SiloPort = siloPort;
            options.GatewayPort = FreePort();
        });
        builder.Services.Configure<DevelopmentClusterMembershipOptions>(options => options.PrimarySiloEndpoint = new IPEndPoint(IPAddress.Loopback, siloPort));
        builder.Services.AddStageExtensions(builder.Configuration);

        // What SemanticHost does once the packages are loaded and the model is compiled.
        ExtensionModelHosting.Add(builder.Services, _loaded.Model.Application, _uiContract, _modelPackages);
        PinnedAbilityCatalog.AddExtensionPackages(builder.Services, _modelPackages);
        var overrides = new ModelActivationOverrides(SystemActivationOverrides.Read(_modelFolder));
        builder.Services.Replace(ServiceDescriptor.Singleton(provider => new ActivatedSystems(_modelPackages, overrides, provider.GetRequiredService<IOptions<ExtensionOptions>>())));
        builder.Services.PostConfigure<ExtensionOptions>(options =>
        {
            options.Execution.Sharing.Publishers = [PackageFixtures.Publisher];
            options.Execution.Hosts["dotnet"] = new() { Command = "unused" };
        });

        // Raising a signal needs the workflow engine, which this host leaves out: a signal waits for Scene's trigger.
        builder.Services.Replace(ServiceDescriptor.Singleton(Substitute.For<IContractTriggers>()));
        builder.Services.RemoveAll<IBackendRuntime>();
        builder.Services.AddSingleton<IBackendRuntime>(_runtime);

        _app = builder.Build();
        await _app.StartAsync();
    }

    protected new async Task Destroy()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
