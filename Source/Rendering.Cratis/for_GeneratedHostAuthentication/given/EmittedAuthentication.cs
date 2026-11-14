// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Microsoft.AspNetCore.Builder;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

static class EmittedAuthentication
{
    static readonly Lazy<Type> Helper = new(Compile);

    internal static void Services(WebApplicationBuilder builder) => Invoke("ConfigureServices", builder);

    internal static void Application(WebApplication app) => Invoke("ConfigureApplication", app);

    static void Invoke(string method, object argument)
    {
        try
        {
            Helper.Value.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [argument]);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();
        }
    }

    static Type Compile()
    {
        const string prefix = "cratis-scaffold:text:";
        var request = CratisBackendApplicationScaffoldRequest.Create("MyApp", "MyApp", "MyApp", CratisBackendApplicationScaffoldProfile.Current);
        var files = new CratisBackendApplicationScaffold().Create(request)
            .Where(input => input.Name[prefix.Length..].StartsWith("GeneratedHostAuthentication", StringComparison.Ordinal))
            .Select(input => new RenderedFile(input.Name[prefix.Length..], Encoding.UTF8.GetString(input.Bytes.AsSpan())));
        var sdkUsings = new RenderedFile("WebSdkUsings.g.cs", """
            global using System;
            global using System.Linq;
            global using System.Threading.Tasks;
            global using Microsoft.AspNetCore.Builder;
            global using Microsoft.AspNetCore.Http;
            global using Microsoft.Extensions.DependencyInjection;
            """);
        using var bytes = new MemoryStream();
        var compilation = RenderedOutput.CreateCompilation([.. files, sdkUsings]).WithAssemblyName("EmittedHostAuthentication");
        var result = compilation.Emit(bytes);
        if (!result.Success)
        {
            // Compiler diagnostics contain source locations, never runtime credentials or protocol messages.
            throw new InvalidEmittedAuthentication(string.Join(Environment.NewLine, result.Diagnostics));
        }

        bytes.Position = 0;
        return AssemblyLoadContext.Default.LoadFromStream(bytes).GetType("MyApp.GeneratedHostAuthentication", throwOnError: true)!;
    }

    /// <summary>
    /// The exception that is thrown when the actual rendered authentication helpers do not compile.
    /// </summary>
    /// <param name="diagnostics">The compiler diagnostics.</param>
    sealed class InvalidEmittedAuthentication(string diagnostics) : Exception(diagnostics);
}
