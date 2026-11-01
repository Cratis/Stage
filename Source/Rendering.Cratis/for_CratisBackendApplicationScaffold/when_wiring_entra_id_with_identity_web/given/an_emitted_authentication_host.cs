// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_wiring_entra_id_with_identity_web.given;

public class an_emitted_authentication_host : Specification
{
    protected const string Selector = "CratisAuthenticationSelector";
    protected const string PublicOrigin = "https://application.invalid:8443";
    static readonly Lazy<Type> _authentication = new(CompileAuthentication);
    protected WebApplicationBuilder _builder = null!;
    protected WebApplication _app = null!;

    void Establish()
    {
        _builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        _builder.WebHost.UseTestServer();
        _builder.Services.AddRouting();
    }

    protected void ConfigureEntraId() => _builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Cratis:Authentication:EntraId:Instance"] = "https://identity.invalid/",
        ["Cratis:Authentication:EntraId:TenantId"] = "<tenant-id>",
        ["Cratis:Authentication:EntraId:ClientId"] = "<synthetic-client-id>",
        ["Cratis:Authentication:EntraId:ClientCredentials:0:SourceType"] = "ClientSecret",
        ["Cratis:Authentication:EntraId:ClientCredentials:0:ClientSecret"] = "<synthetic-client-secret>",
        ["Cratis:Authentication:PublicOrigin"] = PublicOrigin
    });

    protected void AddAuthentication() => _authentication.Value.GetMethod("Add")!.Invoke(null, [_builder]);

    protected void UseAuthentication() => _authentication.Value.GetMethod("Use")!.Invoke(null, [_app]);

    async Task Destroy()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    static Type CompileAuthentication()
    {
        var request = CratisBackendApplicationScaffoldRequest.Create("MyApp", "MyApp", "MyApp", CratisBackendApplicationScaffoldProfile.Current);
        var files = new CratisBackendApplicationScaffold().Create(request)
            .Where(input => input.Name.Equals("cratis-scaffold:text:GeneratedAuthentication.cs", StringComparison.Ordinal) || input.Name.Equals("cratis-scaffold:text:GeneratedAuthenticationPerimeter.cs", StringComparison.Ordinal))
            .Select(input => new RenderedFile(input.Name, Encoding.UTF8.GetString(input.Bytes.AsSpan())));
        return RenderedOutput.Load(files).GetType("MyApp.GeneratedAuthentication.Authentication", throwOnError: true)!;
    }
}
