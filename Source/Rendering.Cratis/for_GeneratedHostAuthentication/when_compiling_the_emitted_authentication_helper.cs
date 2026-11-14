// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication;

public class when_compiling_the_emitted_authentication_helper : a_current_scaffold
{
    Diagnostic[] _diagnostics = [];
    RenderedFile[] _helpers = [];

    void Because()
    {
        _helpers = [.. _first.Where(input => PathOf(input).StartsWith("GeneratedHostAuthentication", StringComparison.Ordinal))
            .Select(input => new RenderedFile(PathOf(input), Text(input)))];
        var sdkUsings = new RenderedFile("WebSdkUsings.g.cs", """
            global using System;
            global using System.Linq;
            global using System.Threading.Tasks;
            global using Microsoft.AspNetCore.Builder;
            global using Microsoft.AspNetCore.Http;
            global using Microsoft.Extensions.DependencyInjection;
            """);
        using var assembly = new MemoryStream();
        _diagnostics = [.. RenderedOutput.CreateCompilation([.. _helpers, sdkUsings]).Emit(assembly).Diagnostics];
    }

    [Fact] void should_compile_all_three_emitted_partials() => _helpers.Length.ShouldEqual(3);
    [Fact] void should_bind_to_the_pinned_identity_apis() => _diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_compile_without_warnings() => _diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ShouldBeEmpty();
}
