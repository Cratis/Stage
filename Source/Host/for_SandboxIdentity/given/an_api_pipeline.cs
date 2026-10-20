// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Identity;
using Cratis.Specifications;
using Microsoft.AspNetCore.Authentication;

namespace Cratis.Stage.Host.for_SandboxIdentity.given;

public class an_api_pipeline : Specification
{
    protected DefaultHttpContext _context = null!;
    protected ClaimsPrincipal? _persona;
    protected bool _enabled = true;
    ServiceProvider _provider = null!;

    protected Task Execute(string path = "/api/projects")
    {
        var services = new ServiceCollection();
        services.AddLogging();
        SandboxIdentityOptions.Configure(services, new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Stage:Sandbox:Identity"] = _enabled.ToString() }).Build());
        services.AddAuthentication(MicrosoftIDentityPlatformAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, MicrosoftIDentityPlatformAuthHandler>(MicrosoftIDentityPlatformAuthHandler.SchemeName, _ => { });
        _provider = services.BuildServiceProvider();
        _context.RequestServices = _provider;
        _context.Request.Path = path;
        var pipeline = new ApplicationBuilder(_provider);
        SandboxIdentity.Use(pipeline, _persona);
        pipeline.UseAuthentication();
        pipeline.Run(_ => Task.CompletedTask);
        return pipeline.Build()(_context);
    }

    void Establish()
    {
        _context = new DefaultHttpContext();
        _persona = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "persona:Shopper"), new Claim(ClaimTypes.Name, "Ada"), new Claim(ClaimTypes.Role, "Shopper")],
            "stage-persona"));
    }

    void Destroy() => _provider.Dispose();
}
