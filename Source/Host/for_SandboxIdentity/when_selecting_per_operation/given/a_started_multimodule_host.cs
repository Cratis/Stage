// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Host.for_SandboxIdentity.when_stripping_before_authentication.given;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using NSubstitute;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_selecting_per_operation.given;

public class a_started_multimodule_host : for_StageEndpointMapper.given.a_routed_model
{
    protected HttpClient _client = null!;
    protected int _status;
    protected string? _persona;
    protected string? _displayName;
    protected bool _hasMutation;
    protected ExecutableSemanticModel _model = null!;
    protected string[] _forwardedHeaders = [];
    protected AuthenticationRecording _recording = new();
    string _folder = null!;
    protected virtual bool SandboxEnabled => true;

    async Task Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Commerce"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("commerce"), "commerce", "Commerce.play", multimodule_source.Source);
        var compilation = new SemanticModelCompiler().Compile("Commerce", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        compilation.Success.ShouldBeTrue();
        _model = compilation.Value!.Model;
        var plan = SemanticExecutionPlan.Compile(_model);
        plan.Success.ShouldBeTrue();
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        MapSemanticModel(EventModelLoader.LoadFromSource(multimodule_source.Source), plan.Plan!, appender);

        _folder = Path.Combine(Path.GetTempPath(), $"stage-personas-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "Commerce.play");
        await File.WriteAllTextAsync(path, multimodule_source.Source);
        _app.UseSandboxIdentityHeaders();
        _app.UseRouting();
        _app.UseSandboxIdentity(path, plan.Plan!.Model, _surface);
        _app.Use((context, next) =>
        {
            context.Response.Headers["X-Test-Persona"] = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            context.Response.Headers["X-Test-Name"] = context.User.Identity?.Name ?? "anonymous";
            context.Response.Headers["X-Test-Mutation"] = context.User.HasClaim("mutation", "true").ToString();
            _forwardedHeaders = [.. context.Request.Headers.Keys.Where(name => name.StartsWith("x-ms-client-principal", StringComparison.OrdinalIgnoreCase))];
            (context.User.Identity as ClaimsIdentity)?.AddClaim(new Claim("mutation", "true"));
            return next(context);
        });
        _app.MapGet("/api/unmodeled", () => Results.Ok());
        _app.MapGet("/outside", () => Results.Ok());
        _app.MapGet("/.cratis/me", () => Results.Ok());
        _app.Urls.Add("http://127.0.0.1:0");
        await _app.StartAsync();
        var addresses = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        _client = new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
    }

    protected override void ConfigureHostServices(IServiceCollection services)
    {
        SandboxIdentityOptions.Configure(services, new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Stage:Sandbox:Identity"] = SandboxEnabled.ToString() }).Build());
        services.AddSingleton(_recording);
        services.AddAuthentication(RecordingAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, RecordingAuthenticationHandler>(
                RecordingAuthenticationHandler.SchemeName, _ => { });
    }

    protected async Task Send(string method, string path, string? body = null)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (body is not null)
        {
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        }
        using var response = await _client.SendAsync(request);
        _status = (int)response.StatusCode;
        _persona = response.Headers.GetValues("X-Test-Persona").Single();
        _displayName = response.Headers.GetValues("X-Test-Name").Single();
        _hasMutation = bool.Parse(response.Headers.GetValues("X-Test-Mutation").Single());
    }

    async Task Destroy()
    {
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
        }
        if (_folder is not null)
        {
            Directory.Delete(_folder, true);
        }
    }
}
