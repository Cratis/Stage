// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Microsoft.AspNetCore.Authentication;
using Xunit;

using AuthenticationRecording = Cratis.Stage.Host.for_SandboxIdentity.when_stripping_before_authentication.given.AuthenticationRecording;
using RecordingAuthenticationHandler = Cratis.Stage.Host.for_SandboxIdentity.when_stripping_before_authentication.given.RecordingAuthenticationHandler;

namespace Cratis.Stage.Host.for_WarmHost;

public class when_receiving_selected_user_headers_from_a_nonlocal_host : Specification
{
    WebApplication _app = null!;
    IServiceScope _scope = null!;
    DefaultHttpContext _context = null!;
    readonly AuthenticationRecording _recording = new();
    string[] _headersSeenByPipeline = [];
    bool _pipelineReached;

    void Establish()
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { [SandboxUsersOptions.Key] = "true" });
        SandboxIdentityOptions.Configure(builder.Services, builder.Configuration, builder.Environment);
        builder.Services.AddSingleton(_recording);
        builder.Services.AddAuthentication(RecordingAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, RecordingAuthenticationHandler>(RecordingAuthenticationHandler.SchemeName, _ => { });
        _app = WarmHost.Build(builder);
        _app.Run(context =>
        {
            _pipelineReached = true;
            _headersSeenByPipeline = [.. context.Request.Headers.Keys.Where(name => name.StartsWith("x-ms-client-principal", StringComparison.OrdinalIgnoreCase))];
            return Task.CompletedTask;
        });
        _scope = _app.Services.CreateScope();
        _context = new DefaultHttpContext { RequestServices = _scope.ServiceProvider };
        _context.Connection.RemoteIpAddress = IPAddress.Loopback;
        _context.Request.Host = new HostString("example.com");
        _context.Request.Headers["X-MS-CLIENT-PRINCIPAL"] = "selected-principal";
        _context.Request.Headers["X-Ms-Client-Principal-Id"] = "selected-user";
        _context.Request.Headers["x-ms-client-principal-name"] = "Selected user";
        _context.Request.Headers["x-ms-client-principal-idp"] = "aad";
        _context.Request.Headers["x-ms-client-principal-extra"] = "selected-value";
    }

    async Task Because() => await ((IApplicationBuilder)_app).Build()(_context);

    [Fact] void should_reach_the_remaining_pipeline() => _pipelineReached.ShouldBeTrue();
    [Fact] void should_strip_identity_headers_before_authentication() => _recording.Invocations.Single().ShouldBeEmpty();
    [Fact] void should_strip_identity_headers_before_the_remaining_pipeline() => _headersSeenByPipeline.ShouldBeEmpty();

    async Task Destroy()
    {
        _scope.Dispose();
        await _app.DisposeAsync();
    }
}
