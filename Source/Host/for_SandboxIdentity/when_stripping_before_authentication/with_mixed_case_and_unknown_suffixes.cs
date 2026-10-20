// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.AspNetCore.Authentication;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_stripping_before_authentication;

[Collection(nameof(RecordingAuthenticationHandler))]
public class with_mixed_case_and_unknown_suffixes : Specification
{
    WebApplication _app = null!;
    DefaultHttpContext _context = null!;
    bool _reachedNext;

    void Establish()
    {
        RecordingAuthenticationHandler.SeenIdentityHeaders.Clear();
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Stage:Sandbox:Identity"] = "true" });
        SandboxIdentityOptions.Configure(builder.Services, builder.Configuration);
        builder.Services.AddAuthentication(RecordingAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, RecordingAuthenticationHandler>(RecordingAuthenticationHandler.SchemeName, _ => { });
        _app = builder.Build();
        _app.UseSandboxIdentityHeaders();
        _app.Run(context =>
        {
            _reachedNext = true;
            return Task.CompletedTask;
        });

        _context = new DefaultHttpContext { RequestServices = _app.Services };
        _context.Request.Path = "/workbench/anything";
        _context.Request.Headers["X-MS-CLIENT-PRINCIPAL"] = "spoofed";
        _context.Request.Headers["X-Ms-Client-Principal-Id"] = "spoofed-id";
        _context.Request.Headers["x-ms-client-principal-name"] = "spoofed-name";
        _context.Request.Headers["X-MS-CLIENT-PRINCIPAL-IDP"] = "attacker";
        _context.Request.Headers["x-correlation-id"] = "kept";
    }

    Task Because() => ((IApplicationBuilder)_app).Build()(_context);

    [Fact] void should_reach_the_rest_of_the_pipeline() => _reachedNext.ShouldBeTrue();
    [Fact] void should_not_let_authentication_see_any_identity_header() => RecordingAuthenticationHandler.SeenIdentityHeaders.ShouldBeEmpty();
    [Fact] void should_remove_every_identity_header() => _context.Request.Headers.Keys.Any(name => name.StartsWith("x-ms-client-principal", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_keep_other_headers() => _context.Request.Headers["x-correlation-id"].ToString().ShouldEqual("kept");

    void Destroy() => _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
}
