// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Invocations;
using NSubstitute;

namespace Cratis.Stage.Host.Extensions.Http.given;

/// <summary>
/// A system really listening on loopback that records what it receives, and the built-in http extension wired the way a host wires it.
/// </summary>
public abstract class an_http_system : Specification
{
    protected const string System = "Carrier";
    protected const string Secret = "s3cr3t-value-123";

    protected const string BookPickup = """
        http
          post "/pickups"
          header "Idempotency-Key" = $call.idempotencyKey
          body
            reference = parcelId
            street = address.street
            express = true
          response 201
            trackingNumber = body.tracking
          response 409 -> NoCapacity
        """;

    readonly Lock _gate = new();
    protected readonly List<RecordedRequest> _received = [];
    protected Func<RecordedRequest, (int Status, string Body)> _answer = _ => (201, """{"tracking":"TRK-1"}""");
    protected Dictionary<string, string?> _values = [];
    protected RecordingLoggers _logs = null!;
    protected WebApplication _server = null!;
    protected ServiceProvider _provider = null!;
    protected IAbilityBackend _inner = null!;
    protected IAbilityBackend _backend = null!;
    protected string _address = string.Empty;

    protected static AbilityCall Call(string ability = "BookPickup", string input = """{"parcelId":"p-1","address":{"street":"Main 1"}}""", TimeSpan? timeout = null) =>
        new(new(System, ability, 1), EffectClass.Idempotent, "invocation-1", 1, input, "sha256:aaaa", timeout ?? TimeSpan.FromSeconds(10));

    protected static HttpAbilityDefinition Parsed(string block, string ability = "BookPickup")
    {
        var parsed = HttpBlockParser.Parse(System, ability, block);
        return parsed.Definition ?? throw new InvalidOperationException(string.Join(' ', parsed.Errors));
    }

    protected async Task StartSystem()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0));
        _server = builder.Build();
        _server.Run(async context =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var request = new RecordedRequest(
                context.Request.Method,
                context.Request.Path + context.Request.QueryString.Value,
                context.Request.Headers.ToDictionary(header => header.Key, header => header.Value.ToString(), StringComparer.OrdinalIgnoreCase),
                await reader.ReadToEndAsync());
            lock (_gate) _received.Add(request);
            var (status, body) = _answer(request);
            context.Response.StatusCode = status;
            await context.Response.WriteAsync(body);
        });
        await _server.StartAsync();
        _address = _server.Urls.First();
        _values = new() { [$"{System}:Http:BaseAddress"] = _address };
    }

    protected void StartStage(params HttpAbilityDefinition[] definitions)
    {
        _logs = new RecordingLoggers();
        var settings = Substitute.For<ISystemSettings>();
        settings.Resolve(Arg.Any<string>(), Arg.Any<string>()).Returns(call => _values.GetValueOrDefault($"{call.ArgAt<string>(0)}:{call.ArgAt<string>(1)}"));
        _inner = Substitute.For<IAbilityBackend>();
        _inner.Invoke(Arg.Any<AbilityCall>(), Arg.Any<CancellationToken>()).Returns(new AbilityNotSent("The inner backend was asked."));
        _provider = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(_logs)
            .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
            .AddSingleton(settings)
            .AddSingleton(_inner)
            .AddStageHttpAbilities(definitions)
            .BuildServiceProvider();
        _backend = _provider.GetRequiredService<IAbilityBackend>();
    }

    protected async Task Destroy()
    {
        if (_provider is not null) await _provider.DisposeAsync();
        if (_server is not null) await _server.DisposeAsync();
    }
}
