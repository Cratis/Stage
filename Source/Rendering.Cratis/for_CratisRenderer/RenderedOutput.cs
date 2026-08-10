// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// The implicit usings the scaffolded project enables (<c>ImplicitUsings</c> in the Cratis template), mirrored
    /// here so the compilation sees the same ambient namespaces the rendered application really builds with.
    /// </summary>
    const string ImplicitUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Tasks;
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
    public static IReadOnlyList<string> Errors(IEnumerable<RenderedFile> files)
    {
        var trees = files
            .Select(file => CSharpSyntaxTree.ParseText(file.Content, path: file.RelativePath))
            .Prepend(CSharpSyntaxTree.ParseText(ImplicitUsings, path: "GlobalUsings.g.cs"));

        var compilation = CSharpCompilation.Create(
            "RenderedApplication",
            trees,
            _references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        return
        [
            .. compilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => $"{diagnostic.Location.SourceTree?.FilePath}: {diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}")
        ];
    }
}
