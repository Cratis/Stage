// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter.given;

public static class the_emitted_perimeter
{
    public const string Cookie = "Cookies";
    public const string Bearer = "Bearer";
    public const string OpenIdConnect = "OpenIdConnect";
    public const string Selector = "CratisAuthenticationSelector";
    public const string PublicOrigin = "https://app.example.invalid";
    public const string EntraIdKey = "Cratis:Authentication:EntraId";
    public const string PublicOriginKey = "Cratis:Authentication:PublicOrigin";
    static readonly Lazy<Assembly> _assembly = new(Compile);

    public static string SelectScheme(HttpContext context) => (string)Perimeter.GetMethod("SelectScheme")!.Invoke(null, [context])!;

    public static void Use(IApplicationBuilder app) => Perimeter.GetMethod("Use").Invoke(null, [app, new Uri(PublicOrigin)]);

    public static object? Read(IConfiguration configuration) => _assembly.Value.GetType("MyApp.GeneratedAuthentication.AuthenticationSettings").GetMethod("Read").Invoke(null, [configuration]);

    static Type Perimeter => _assembly.Value.GetType("MyApp.GeneratedAuthentication.AuthenticationPerimeter")!;

    static Assembly Compile()
    {
        var request = CratisBackendApplicationScaffoldRequest.Create("MyApp", "MyApp", "MyApp", CratisBackendApplicationScaffoldProfile.Current);
        var input = new CratisBackendApplicationScaffold().Create(request).Single(input => input.Name.EndsWith(":GeneratedAuthenticationPerimeter.cs", StringComparison.Ordinal));
        var source = new UTF8Encoding(false, true).GetString(input.Bytes.AsSpan());
        return RenderedOutput.Load([new RenderedFile("GeneratedAuthenticationPerimeter.cs", source)]);
    }
}
