// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

/// <summary>
/// Compiles rendered files in memory against the real Cratis assemblies.
/// </summary>
/// <remarks>
/// Asserting on generated strings only proves the renderer emitted what the spec expected it to emit. It cannot
/// see a missing import, a constructor called with the wrong number of arguments, or an attribute placed where it
/// is not valid — all of which the compiler sees immediately. This is the assertion that closes that gap.
/// </remarks>
internal static class RenderedOutput
{
    /// <summary>
    /// The implicit usings the scaffolded project enables (<c>ImplicitUsings</c> in the Cratis template), together
    /// with the global usings the <c>Cratis</c> package itself contributes through its <c>Cratis.props</c>, mirrored
    /// here so the compilation sees the same ambient namespaces the rendered application really builds with.
    /// </summary>
    /// <remarks>
    /// The package's set is load-bearing in both directions: without it the rendered validators look broken because
    /// <c>FluentValidation</c> is missing, and with it a short type name that is unambiguous on its own becomes
    /// ambiguous — <c>IIdentityProvider</c> is declared by both <c>Cratis.Arc.Identity</c> and
    /// <c>Cratis.Chronicle.Identities</c>. A compilation that omits them sees neither.
    /// </remarks>
    const string ImplicitUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Tasks;
        global using Cratis.Arc;
        global using Cratis.Arc.Authentication;
        global using Cratis.Arc.Authorization;
        global using Cratis.Arc.Chronicle.Aggregates;
        global using Cratis.Arc.Commands;
        global using Cratis.Arc.Commands.ModelBound;
        global using Cratis.Arc.Identity;
        global using Cratis.Arc.Queries;
        global using Cratis.Arc.Queries.ModelBound;
        global using Cratis.Arc.Swagger;
        global using Cratis.Arc.Validation;
        global using Cratis.Chronicle;
        global using Cratis.Chronicle.Events;
        global using Cratis.Chronicle.Events.Constraints;
        global using Cratis.Chronicle.EventSequences;
        global using Cratis.Chronicle.Observation;
        global using Cratis.Chronicle.Projections;
        global using Cratis.Chronicle.Projections.ModelBound;
        global using Cratis.Chronicle.Reactors;
        global using Cratis.Chronicle.ReadModels;
        global using Cratis.Chronicle.Reducers;
        global using Cratis.Chronicle.Transactions;
        global using Cratis.Concepts;
        global using FluentValidation;
        """;

    static readonly MetadataReference[] _references =
    [
        .. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(assembly => MetadataReference.CreateFromFile(assembly))
    ];

    /// <summary>
    /// Compiles the rendered files and returns every compilation error, each prefixed with the file it came from.
    /// </summary>
    /// <param name="files">The rendered files to compile.</param>
    /// <returns>The compilation errors, empty when the output compiles.</returns>
    public static IReadOnlyList<string> Errors(IEnumerable<RenderedFile> files) => Diagnostics(CreateCompilation(files), DiagnosticSeverity.Error);

    /// <summary>
    /// Compiles rendered files and returns every compiler warning.
    /// </summary>
    /// <param name="files">The rendered files to compile.</param>
    /// <returns>The compilation warnings, empty when the output is warning-free.</returns>
    public static IReadOnlyList<string> Warnings(IEnumerable<RenderedFile> files) => Diagnostics(CreateCompilation(files), DiagnosticSeverity.Warning);

    /// <summary>
    /// Compiles and loads rendered files so specs can exercise the real Arc evaluators against generated members.
    /// </summary>
    /// <param name="files">The rendered files to compile and load.</param>
    /// <returns>The loaded rendered application assembly.</returns>
    /// <exception cref="RenderedOutputDoesNotCompile">The rendered output does not compile.</exception>
    public static Assembly Load(IEnumerable<RenderedFile> files)
    {
        var compilation = CreateCompilation(files);
        using var assembly = new MemoryStream();
        var result = compilation.Emit(assembly);
        if (!result.Success)
        {
            throw new RenderedOutputDoesNotCompile(Diagnostics(compilation, DiagnosticSeverity.Error));
        }

        assembly.Position = 0;
        return AssemblyLoadContext.Default.LoadFromStream(assembly);
    }

    static CSharpCompilation CreateCompilation(IEnumerable<RenderedFile> files)
    {
        // DEBUG has to be defined or a rendered specification compiles to nothing: the whole file sits inside
        // '#if DEBUG', and a parse without the symbol drops it silently — the assertion would then pass on an
        // empty compilation unit and prove nothing about the spec it was written for.
        var parseOptions = new CSharpParseOptions(preprocessorSymbols: ["DEBUG"]);
        var trees = files
            .Select(file => CSharpSyntaxTree.ParseText(file.Content, parseOptions, path: file.RelativePath))
            .Prepend(CSharpSyntaxTree.ParseText(ImplicitUsings, parseOptions, path: "GlobalUsings.g.cs"));

        return CSharpCompilation.Create(
            $"RenderedApplication_{Guid.NewGuid():N}",
            trees,
            _references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    static IReadOnlyList<string> Diagnostics(CSharpCompilation compilation, DiagnosticSeverity severity) =>
    [
        .. compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == severity)
            .Select(diagnostic => $"{diagnostic.Location.SourceTree?.FilePath}: {diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}")
    ];

    sealed class RenderedOutputDoesNotCompile(IReadOnlyList<string> errors) : Exception(
        $"Rendered output does not compile:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
}
